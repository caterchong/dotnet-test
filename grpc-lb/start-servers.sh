#!/bin/bash

# 启动3个 gRPC 服务器实例的脚本

echo "Starting gRPC servers..."

# 启动服务器1 (端口 50051)
dotnet run --project GrpcServer/GrpcServer.csproj 50051 &
SERVER1_PID=$!

# 启动服务器2 (端口 50052)
dotnet run --project GrpcServer/GrpcServer.csproj 50052 &
SERVER2_PID=$!

# 启动服务器3 (端口 50053)
dotnet run --project GrpcServer/GrpcServer.csproj 50053 &
SERVER3_PID=$!

echo "Servers started:"
echo "  Server 1 (port 50051): PID $SERVER1_PID"
echo "  Server 2 (port 50052): PID $SERVER2_PID"
echo "  Server 3 (port 50053): PID $SERVER3_PID"
echo ""
echo "Press Ctrl+C to stop all servers"

# 等待用户中断
trap "kill $SERVER1_PID $SERVER2_PID $SERVER3_PID; exit" INT TERM

wait
