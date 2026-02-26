# HTTP/2 Echo Server - HTTP2c 协议压测工具

## 项目概述

基于 **Kestrel 框架**开发的 **HTTP/2** Web 服务程序，实现 Echo 功能（接收数据并原样返回），并提供完整的性能压测方案。

## 功能特性

### 1. Echo 服务
- **POST /echo** - 原样返回请求体内容
- **GET/POST/PUT/DELETE /echo/{path}** - 返回完整的请求信息（JSON格式）
- **GET /health** - 服务健康检查

### 2. 协议支持
- ✅ HTTP/1.1
- ✅ HTTP/2 (h2c - HTTP/2 Cleartext)
- ✅ 完整的 Kestrel 支持

### 3. 性能压测工具

内置 4 种压测场景：

| 测试场景 | 说明 | 指标 |
|---------|------|------|
| **基本 Echo** | 顺序发送 100 个请求 | 响应时间、吞吐量 |
| **并发能力** | 50 个并发连接，每个 10 个请求 | 并发处理能力、平均延迟 |
| **大数据处理** | 发送 512KB 数据 20 次 | 大负载处理能力 |
| **压力测试** | 持续 10 秒，100 个并发连接 | 稳定性、最大吞吐量 |

## 项目结构

```
http2c/
├── Program.cs                  # 主程序 - Echo 服务实现
├── Http2EchoBenchmark.cs      # 基准测试类 - 测试逻辑
├── BenchmarkRunner.cs         # 压测运行器 - 执行所有测试
├── Http2EchoServer.csproj    # 项目文件
├── appsettings.json           # 配置文件
└── README.md                  # 本文件
```

## 快速开始

### 前置要求
- .NET 8.0 或更高版本
- Windows / Linux / macOS

### 1. 启动 Echo 服务

```bash
# 方式1：直接运行
dotnet run --project Http2EchoServer.csproj

# 方式2：发布后运行
dotnet publish -c Release
./bin/Release/net8.0/Http2EchoServer
```

服务将启动在 `http://localhost:5000`

### 2. 测试基本功能

```bash
# 测试 POST echo
curl -X POST http://localhost:5000/echo -d "Hello Server"

# 测试 GET echo
curl http://localhost:5000/echo/test

# 健康检查
curl http://localhost:5000/health
```

### 3. 运行性能压测

```bash
# 编译压测程序
dotnet build Http2EchoServer.csproj

# 运行压测（需要服务器在另一个终端运行）
dotnet run --project Http2EchoServer.csproj -- benchmark

# 或直接运行编译后的压测程序
dotnet run BenchmarkRunner.cs
```

## 压测结果说明

### 输出指标

- **总请求数** - 完成的请求总数
- **平均响应时间** - 平均每个请求的响应时间（毫秒）
- **吞吐量** - 每秒处理的请求数（req/s）
- **错误数** - 失败的请求数

### 性能评估

| 指标 | 优秀 | 良好 | 一般 |
|-----|------|------|------|
| **吞吐量** | > 10,000 req/s | 5,000-10,000 | < 5,000 |
| **平均延迟** | < 1 ms | 1-5 ms | > 5 ms |
| **并发稳定性** | 错误率 < 0.1% | 0.1-1% | > 1% |

## 使用 PowerShell 脚本启动

### start-server.ps1

```powershell
# 启动 Echo 服务
dotnet run
```

### run-benchmark.ps1

```powershell
# 运行所有压测
dotnet run BenchmarkRunner.cs
```

## 自定义压测

修改 `BenchmarkRunner.cs` 中的参数：

```csharp
// 基本测试 - 增加迭代次数
results.Add(await benchmark.BenchmarkBasicEcho(iterations: 1000));

// 并发测试 - 增加并发数
results.Add(await benchmark.BenchmarkConcurrency(
    concurrentRequests: 200,  // 200 个并发
    requestsPerConnection: 20  // 每个 20 个请求
));

// 大数据测试 - 增加数据大小
results.Add(await benchmark.BenchmarkLargePayload(
    payloadSizeKB: 1024,  // 1 MB
    iterations: 50
));

// 压力测试 - 扩展持续时间
results.Add(await benchmark.BenchmarkStressTest(
    durationSeconds: 30,  // 30 秒
    concurrentRequests: 200
));
```

## HTTP/2 特性

### h2c 协议说明

HTTP/2 Cleartext (`h2c`) 是不加密的 HTTP/2 协议实现：

- ✅ 相比 HTTP/1.1 的优势
  - 多路复用：单一 TCP 连接处理多个请求
  - 二进制分帧：更高效的数据传输
  - 服务器推送：主动发送资源
  - 头部压缩：减少传输开销

- 使用场景
  - 内网通信
  - 本地开发测试
  - 微服务架构

## 调试与故障排除

### 问题：无法启动服务

```bash
# 检查端口是否被占用
netstat -ano | findstr :5000

# 更改设置中的端口
# 修改 appsettings.json 或命令行参数
```

### 问题：压测无法连接

```
❌ 无法连接到服务器。请确保 Echo 服务在运行。
```

**解决方案：**
1. 确保 Echo 服务已启动
2. 检查服务器地址和端口正确
3. 尝试 `curl http://localhost:5000/health`

### 问题：高错误率

- 增加 HTTP 客户端超时时间
- 减少并发数量
- 检查服务器资源（CPU、内存）

## 高级配置

### Kestrel 性能调优

编辑 `Program.cs` 中的 Kestrel 配置：

```csharp
serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 最大请求体 100MB
serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(75);
```

### 增加并发限制

```csharp
serverOptions.Limits.MaxConcurrentConnections = 500;
serverOptions.Limits.MaxConcurrentUpgradedConnections = 500;
```

## 性能优化建议

1. **启用 Release 编译**
   ```bash
   dotnet run -c Release
   ```

2. **调整线程池大小**
   ```csharp
   ThreadPool.GetMinThreads(out int workerThreads, out int ioThreads);
   ThreadPool.SetMinThreads(Math.Max(workerThreads, Environment.ProcessorCount * 2), ioThreads);
   ```

3. **监控系统资源**
   - CPU 使用率
   - 内存占用
   - 网络 I/O

## 参考资源

- [ASP.NET Core Kestrel 文档](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [HTTP/2 规范 RFC 7540](https://tools.ietf.org/html/rfc7540)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)

## 许可

MIT License

## 作者

HTTP/2 Echo Server 开发项目

---

**最后更新**: 2026年2月
