#!/bin/bash

# HTTP/2 Echo Server - 启动脚本

echo "================================"
echo "HTTP/2 Echo Server"
echo "================================"
echo ""
echo "启动 Echo 服务..."
echo "服务地址: http://localhost:5000"
echo ""

dotnet run -- --urls "http://0.0.0.0:5000"
