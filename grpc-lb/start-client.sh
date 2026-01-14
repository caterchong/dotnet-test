#!/bin/bash

# 启动 gRPC 客户端的脚本

TARGET="${1:-http://localhost:50051,http://localhost:50052,http://localhost:50053}"
DURATION="${2:-60}"
INTERVAL="${3:-1000}"

echo "Starting gRPC client..."
echo "Target: $TARGET"
echo "Duration: $DURATION seconds"
echo "Interval: $INTERVAL ms"
echo ""

dotnet run --project GrpcClient/GrpcClient.csproj "$TARGET" "$DURATION" "$INTERVAL"
