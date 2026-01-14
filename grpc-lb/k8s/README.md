# Kubernetes 部署说明

## 前置要求

- Kind 集群或 Kubernetes 集群
- kubectl 已配置

## 部署步骤

### 1. 构建 Docker 镜像

```bash
# 构建服务端镜像
docker build -t grpc-server:latest -f Dockerfile.server .

# 如果需要推送到 registry
docker tag grpc-server:latest your-registry/grpc-server:latest
docker push your-registry/grpc-server:latest
```

### 2. 部署到 Kubernetes

```bash
# 部署服务
kubectl apply -f deployment.yaml

# 检查部署状态
kubectl get pods -l app=grpc-server
kubectl get svc grpc-server
```

### 3. 测试扩缩容

```bash
# 扩容到4个实例
kubectl scale deployment grpc-server --replicas=4

# 等待新实例启动
kubectl rollout status deployment/grpc-server

# 缩容到2个实例
kubectl scale deployment grpc-server --replicas=2

# 查看 Pod 状态
kubectl get pods -l app=grpc-server -w
```

### 4. 运行客户端测试

```bash
# 在集群内运行客户端
kubectl run grpc-client --image=grpc-client:latest --rm -it --restart=Never -- \
  "dns://grpc-server.default.svc.cluster.local:50051" 60 1000

# 或者从本地连接到集群服务
# 需要先 port-forward
kubectl port-forward svc/grpc-server 50051:50051
```

## DNS 解析

在 Kubernetes 中，服务可以通过 DNS 名称访问：
- 同一命名空间: `grpc-server:50051`
- 跨命名空间: `grpc-server.default.svc.cluster.local:50051`

客户端使用 DNS resolver 时，会自动发现所有 Pod IP。

## 监控和日志

```bash
# 查看 Pod 日志
kubectl logs -l app=grpc-server --tail=50 -f

# 查看特定 Pod 日志
kubectl logs <pod-name>

# 查看服务端点
kubectl get endpoints grpc-server
```

## 故障模拟

```bash
# 删除一个 Pod（模拟故障）
kubectl delete pod <pod-name>

# 查看 Pod 自动恢复
kubectl get pods -l app=grpc-server -w
```
