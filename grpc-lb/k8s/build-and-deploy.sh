#!/bin/bash

set -e

echo "=== Building Docker Images ==="

# 获取脚本所在目录
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 检测架构
ARCH=$(uname -m)
PLATFORM="linux/amd64"

# 如果是 ARM64 (Apple Silicon)，使用 linux/amd64 平台构建以避免 protoc 工具问题
if [ "$ARCH" = "arm64" ]; then
    echo "Detected ARM64 architecture, using linux/amd64 platform for compatibility"
    PLATFORM="linux/amd64"
fi

# 构建 Server 镜像
# 使用 DOCKER_BUILDKIT=1 以确保正确的平台处理
echo "Building grpc-server image (platform: $PLATFORM)..."
cd "$PROJECT_ROOT"
DOCKER_BUILDKIT=1 docker build --platform "$PLATFORM" -t grpc-server:latest -f GrpcServer/Dockerfile .

# 构建 Client 镜像
# 使用 DOCKER_BUILDKIT=1 以确保正确的平台处理
echo "Building grpc-client image (platform: $PLATFORM)..."
DOCKER_BUILDKIT=1 docker build --platform "$PLATFORM" -t grpc-client:latest -f GrpcClient/Dockerfile .

# 加载镜像到 kind 集群
if command -v kind &> /dev/null; then
    echo "Loading images into kind cluster..."
    kind load docker-image grpc-server:latest
    kind load docker-image grpc-client:latest
    echo "Images loaded successfully!"
else
    echo "Warning: kind command not found. Skipping image loading."
    echo "If using kind, run manually:"
    echo "  kind load docker-image grpc-server:latest"
    echo "  kind load docker-image grpc-client:latest"
fi

echo ""
echo "=== Deployment ==="
echo "To deploy to Kubernetes, run:"
echo "  kubectl apply -f $SCRIPT_DIR/all.yaml"
echo ""
echo "To check status:"
echo "  kubectl get pods -l app=grpc-server"
echo "  kubectl get pods -l app=grpc-client"
echo "  kubectl get svc grpc-server"
echo ""
echo "To view client logs:"
echo "  kubectl logs -f deployment/grpc-client"
