# Kubernetes 部署指南

本目录包含在 Kubernetes（包括 Kind 集群）上部署 gRPC 负载均衡测试的所有必要文件。

## 文件说明

- `all.yaml` - 完整的部署配置（推荐使用）
- `server-deployment.yaml` - 仅服务器部署配置
- `client-deployment.yaml` - 仅客户端部署配置
- `build-and-deploy.sh` - 构建镜像和部署脚本

## 前置要求

1. **Docker** - 用于构建镜像
2. **Kind** - 本地 Kubernetes 集群（或任何 Kubernetes 集群）
3. **kubectl** - Kubernetes 命令行工具

## 快速开始

### 1. 创建 Kind 集群（如果还没有）

```bash
kind create cluster --name grpc-test
```

### 2. 构建镜像并部署

```bash
# 方式1: 使用脚本（推荐）
chmod +x build-and-deploy.sh
./build-and-deploy.sh
kubectl apply -f all.yaml

# 方式2: 手动构建
docker build -t grpc-server:latest -f ../GrpcServer/Dockerfile ..
docker build -t grpc-client:latest -f ../GrpcClient/Dockerfile ..
kind load docker-image grpc-server:latest
kind load docker-image grpc-client:latest
kubectl apply -f all.yaml
```

### 3. 检查部署状态

```bash
# 查看 Pod 状态
kubectl get pods -l app=grpc-server
kubectl get pods -l app=grpc-client

# 查看 Service
kubectl get svc grpc-server

# 查看客户端日志
kubectl logs -f deployment/grpc-client
```

## 架构说明

### Headless Service

服务器使用 **Headless Service**（`clusterIP: None`），这样：
- 每个 Pod 都有独立的 DNS 记录
- DNS resolver 可以解析到所有 Pod IP
- 客户端可以使用 DNS resolver 进行负载均衡

DNS 格式：
```
<pod-name>.<service-name>.<namespace>.svc.cluster.local
```

例如：
- `grpc-server-xxx-1.grpc-server.default.svc.cluster.local`
- `grpc-server-xxx-2.grpc-server.default.svc.cluster.local`
- `grpc-server-xxx-3.grpc-server.default.svc.cluster.local`

### DNS Resolver 配置

客户端使用 DNS resolver：
```
dns:///grpc-server.default.svc.cluster.local:50051
```

这会：
1. 查询 DNS 获取所有 Pod IP
2. 使用 Round-Robin 负载均衡分配请求
3. 每30秒自动刷新 DNS 记录

## 测试场景

### 场景1: 基本负载均衡

```bash
# 部署3个服务器实例
kubectl apply -f all.yaml

# 查看客户端日志，观察请求分布
kubectl logs -f deployment/grpc-client
```

### 场景2: 扩容测试

```bash
# 扩容到4个实例
kubectl scale deployment grpc-server --replicas=4

# 等待新 Pod 启动
kubectl rollout status deployment/grpc-server

# 观察客户端是否发现新实例（DNS 刷新可能需要30秒）
kubectl logs -f deployment/grpc-client
```

### 场景3: 缩容测试

```bash
# 缩容到2个实例
kubectl scale deployment grpc-server --replicas=2

# 观察客户端如何处理
kubectl logs -f deployment/grpc-client
```

### 场景4: Pod 故障测试

```bash
# 删除一个 Pod（模拟故障）
kubectl delete pod -l app=grpc-server --field-selector=status.phase=Running

# 观察客户端如何处理故障
kubectl logs -f deployment/grpc-client
```

### 场景5: 查看 DNS 解析

```bash
# 在客户端 Pod 中测试 DNS 解析
kubectl exec -it deployment/grpc-client -- nslookup grpc-server.default.svc.cluster.local

# 应该看到所有 Pod IP
```

## 调试技巧

### 查看 Pod 详细信息

```bash
kubectl describe pod <pod-name>
```

### 进入 Pod 调试

```bash
# 进入服务器 Pod
kubectl exec -it deployment/grpc-server -- /bin/sh

# 进入客户端 Pod
kubectl exec -it deployment/grpc-client -- /bin/sh
```

### 查看 Service 端点

```bash
# 查看 Service 的所有端点（Pod IP）
kubectl get endpoints grpc-server
```

### 查看 DNS 记录

```bash
# 在客户端 Pod 中查询 DNS
kubectl exec -it deployment/grpc-client -- nslookup grpc-server.default.svc.cluster.local
```

## 清理

```bash
# 删除所有资源
kubectl delete -f all.yaml

# 或者删除特定资源
kubectl delete deployment grpc-server
kubectl delete deployment grpc-client
kubectl delete service grpc-server
```

## 常见问题

### 1. 镜像拉取失败

如果使用 Kind，确保：
- 使用 `imagePullPolicy: Never`
- 使用 `kind load docker-image` 加载镜像

### 2. DNS 解析失败

检查：
- Service 是否为 Headless（`clusterIP: None`）
- Pod 是否正常运行
- DNS 服务是否正常

### 3. 客户端无法连接

检查：
- 服务器 Pod 是否正常运行
- Service 选择器是否正确
- 端口配置是否正确

### 4. 负载不均衡

- DNS resolver 需要时间刷新（默认30秒）
- 检查客户端日志中的服务器分布
- 确认所有服务器 Pod 都健康

## 性能调优

### 调整 DNS 刷新间隔

修改客户端代码中的 `refreshInterval`：
```csharp
new DnsResolverFactory(refreshInterval: TimeSpan.FromSeconds(10))
```

### 调整负载均衡策略

在客户端代码中修改 `ServiceConfig`：
```csharp
LoadBalancingConfigs = { new LoadBalancingConfig("round_robin") }
// 或
LoadBalancingConfigs = { new LoadBalancingConfig("pick_first") }
```

## 参考

- [gRPC Client-side Load Balancing](https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing)
- [Kubernetes Headless Services](https://kubernetes.io/docs/concepts/services-networking/service/#headless-services)
- [Kind Documentation](https://kind.sigs.k8s.io/)
