# HTTP/2 Echo Server - 压测方案详解

## 目录
1. [压测目标](#压测目标)
2. [测试环境](#测试环境)
3. [测试场景](#测试场景)
4. [性能指标](#性能指标)
5. [执行步骤](#执行步骤)
6. [结果分析](#结果分析)
7. [优化建议](#优化建议)

---

## 压测目标

### 主要目标
- **测试 HTTP/2 协议下的并发处理能力**
- **评估服务响应时间稳定性**
- **确定系统最大吞吐量**
- **验证大数据传输的性能表现**

### 关键性能指标 (KPI)
- 吞吐量 (Requests Per Second - RPS)
- 响应时间 (Response Time)
- 99 百分位延迟 (P99 Latency)
- 错误率 (Error Rate)

---

## 测试环境

### 硬件配置
根据实际环境修改：

```
CPU: Intel Core i7 (或等效)
内存: 8GB+
网络: 本地 (Localhost)
操作系统: Windows 10/11, Linux, macOS
```

### 软件环境
- .NET SDK 8.0+
- HTTP/2 支持的客户端
- 性能监控工具（可选）

### 网络配置
- 本地回环 (127.0.0.1)
- 无网络延迟
- 无带宽限制

---

## 测试场景

### 场景 1: 基本响应时间测试

**目的**: 测试单个 Echo 请求的响应时间

```
发送方式: 顺序发送
请求数: 100
数据大小: 1 KB
超时时间: 30 秒
```

**期望结果**:
- 平均响应时间: < 1ms
- 成功率: 100%
- 吞吐量: > 10,000 req/s

---

### 场景 2: 并发处理能力测试

**目的**: 验证多个并发连接的处理能力

```
并发连接数: 50
每个连接请求数: 10
总请求数: 500
数据大小: 1 KB
```

**期望结果**:
- 平均响应时间: 1-5ms
- 成功率: > 99%
- 吞吐量: > 5,000 req/s

**并发场景扩展**:
```
轻负载:  10 并发  × 5 请求 = 50 请求
中负载: 50 并发  × 10 请求 = 500 请求
重负载: 200 并发 × 20 请求 = 4,000 请求
```

---

### 场景 3: 大数据处理测试

**目的**: 评估系统处理大消息的能力

```
请求数: 20
单个请求大小: 512 KB
总数据量: 10 MB
```

**期望结果**:
- 平均响应时间: < 50ms
- 吞吐量: > 1,000 req/s
- CPU 占用率: < 80%

**数据大小梯度**:
```
小消息:   1 KB  (基准)
中消息: 256 KB
大消息: 512 KB
超大消息: 2 MB
```

---

### 场景 4: 持续压力测试

**目的**: 验证系统在持续高负载下的稳定性

```
持续时间: 10 秒
并发连接数: 100
消息大小: 1 KB
```

**期望结果**:
- 成功率: > 99.9%
- 错误数: < 5
- 吞吐量稳定性: 标准差 < 10%

**压力等级**:
```
I 级（低压）:  10 秒, 50 并发
II 级（中压）: 30 秒, 100 并发
III 级（高压）: 60 秒, 200 并发
```

---

## 性能指标

### 指标定义

| 指标 | 定义 | 单位 | 目标值 |
|-----|------|------|-------|
| **吞吐量** | 每秒处理的请求数 | req/s | > 10,000 |
| **平均延迟** | 所有请求的平均响应时间 | ms | < 5 |
| **P50 延迟** | 50% 请求的响应时间 | ms | < 1 |
| **P99 延迟** | 99% 请求的响应时间 | ms | < 50 |
| **最大延迟** | 最长的单个响应时间 | ms | < 1000 |
| **错误率** | 失败请求占比 | % | < 0.1 |
| **成功率** | 成功请求占比 | % | > 99.9 |

### 性能等级评定

```
┌─────────────┬─────────────┬─────────────┬─────────────┐
│   等级      │   吞吐量    │  平均延迟   │  错误率     │
├─────────────┼─────────────┼─────────────┼─────────────┤
│ 优秀 ⭐⭐⭐  │ > 20,000    │  < 0.5 ms   │  < 0.01%    │
│ 良好 ⭐⭐    │ 10,000-20k  │ 0.5-5 ms    │ 0.01-0.1%   │
│ 良好 ⭐     │ 5,000-10k   │ 5-10 ms     │  0.1-1%     │
│ 需改进 ○    │ < 5,000     │  > 10 ms    │   > 1%      │
└─────────────┴─────────────┴─────────────┴─────────────┘
```

---

## 执行步骤

### 第一步: 环境准备

```bash
# 1. 克隆或切换到项目目录
cd http2c

# 2. 恢复 NuGet 包
dotnet restore

# 3. 编译项目
dotnet build -c Release
```

### 第二步: 启动服务

```bash
# 方式1: 在新终端运行
dotnet run

# 或使用 PowerShell 脚本
.\start-server.ps1

# 验证服务启动
curl http://localhost:5000/health
```

### 第三步: 运行压测

```bash
# 方式1: 使用内置压测
dotnet run BenchmarkRunner.cs

# 方式2: 使用 PowerShell 脚本
.\run-benchmark.ps1

# 方式3: 自定义压测
# 修改 BenchmarkRunner.cs 中的参数后运行
```

### 第四步: 收集结果

```bash
# 输出将显示：
# - 每个测试的详细结果
# - 总体性能汇总表
# - 最高吞吐量
```

---

## 结果分析

### 输出示例

```
╔══════════════════════════════════════════════════════╗
║              性能测试总结                          ║
╚══════════════════════════════════════════════════════╝

测试名称 │ 总请求数 │ 平均响应时间(ms) │ 吞吐量(req/s) │ 错误数
─────────┼──────────┼──────────────────┼──────────────┼────────
基本Echo │   100    │     0.523       │  1910.24     │   0
并发能力 │   500    │     2.145       │  4653.18     │   0
大数据   │    20    │    25.340       │   788.92     │   0
压力测试 │  15340   │     6.523       │  15340.00    │   2

最高吞吐量: 15340.00 req/s
```

### 分析方法

1. **比较基准线**
   ```
   基准性能（100% = 第一次测试的结果）
   后续测试结果 → 相对性能变化
   ```

2. **识别性能瓶颈**
   ```
   • 如果并发测试吞吐量 < 基本测试的 80%
     → 并发处理可能有瓶颈
   
   • 如果大数据测试延迟 > 100ms
     → 缓冲区设置可能需要优化
   
   • 如果压力测试错误率 > 1%
     → 系统资源可能不足
   ```

3. **生成报告**
   ```
   记录关键指标:
   • 总请求数
   • 平均/最大/最小响应时间
   • 成功/失败请求数
   • 每个场景的吞吐量
   • 系统资源使用情况
   ```

---

## 优化建议

### 一. 服务端优化

#### 1. Kestrel 配置优化

```csharp
serverOptions.Limits.MaxRequestBodySize = 512 * 1024 * 1024; // 增大请求体大小
serverOptions.Limits.MaxConcurrentConnections = 1000;
serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
```

#### 2. 线程池优化

```csharp
ThreadPool.GetMinThreads(out int workerThreads, out int ioThreads);
int newWorkerThreads = Math.Max(workerThreads, Environment.ProcessorCount * 4);
ThreadPool.SetMinThreads(newWorkerThreads, ioThreads);
```

#### 3. GC 优化

```xml
<!-- 在 .csproj 中添加 -->
<ItemGroup>
  <RuntimeHostConfigurationOption Include="System.GC.Server" Value="true" />
  <RuntimeHostConfigurationOption Include="System.GC.Concurrent" Value="true" />
</ItemGroup>
```

### 二. 客户端优化

#### 1. HttpClient 连接复用

```csharp
// 使用单一 HttpClient 实例而非每次创建
private static readonly HttpClient _httpClient = new HttpClient();

// 禁用 HTTP/1.1
var handler = new HttpClientHandler();
handler.AutomaticDecompression = DecompressionMethods.None;
```

#### 2. 连接池配置

```csharp
var socketsHandler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = 100,
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
};
```

### 三. 网络优化

#### 1. 操作系统调优

```bash
# Windows (PowerShell - 管理员)
Set-NetTCPSetting -SettingName InternetCustom -MaxSynRetransmissions 2

# Linux
sudo sysctl -w net.core.somaxconn=65535
```

#### 2. TCP 参数优化

```csharp
serverOptions.ListenLocalhost(5000, listenOptions =>
{
    var socketOptions = listenOptions.SocketOptions;
    socketOptions.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
});
```

### 四. 应用逻辑优化

#### 1. 异步处理

```csharp
// ✓ 推荐
app.MapPost("/echo", async (HttpContext context) => { ... });

// ✗ 避免
app.MapPost("/echo", (HttpContext context) => { ... }); // 阻塞
```

#### 2. 缓冲优化

```csharp
var buffer = new byte[16384]; // 增加缓冲区大小
var bytesRead = await context.Request.Body.ReadAsync(buffer);
```

---

## 监控和调试

### 性能监控工具

1. **Windows 性能监视器**
   ```
   • 监控 CPU 使用率
   • 监控内存使用
   • 监控网络 I/O
   ```

2. **dotTrace (JetBrains)**
   ```
   • 性能剖析
   • 热点分析
   • 内存泄漏检测
   ```

3. **Application Insights**
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

### 日志记录

```csharp
ILogger<Program> logger;

app.MapPost("/echo", async (HttpContext context) =>
{
    var stopwatch = Stopwatch.StartNew();
    // ... 处理请求
    stopwatch.Stop();
    
    logger.LogInformation("Request processed in {ElapsedMilliseconds}ms", 
        stopwatch.ElapsedMilliseconds);
});
```

---

## 常见问题

### Q1: 吞吐量远低于预期？

**原因分析**:
- HttpClient 实例过多 (连接开销)
- 线程池配置不足
- 垃圾回收压力大

**解决方案**:
```csharp
// 使用持久化 HttpClient
private static readonly HttpClient _client = new();

// 增加线程池大小
ThreadPool.SetMinThreads(200, 200);
```

### Q2: 请求超时或失败？

**原因分析**:
- 连接数限制
- 内存不足
- 请求体过大

**解决方案**:
```csharp
// 增加超时时间
_httpClient.Timeout = TimeSpan.FromSeconds(60);

// 增加最大并发连接
serverOptions.Limits.MaxConcurrentConnections = 5000;
```

### Q3: 内存持续增长？

**原因分析**:
- 内存泄漏
- 缓冲区未及时释放
- GC 压力大

**解决方案**:
```csharp
// 强制垃圾回收
GC.Collect();
GC.WaitForPendingFinalizers();

// 启用服务器 GC
// 在 .csproj 中: <ServerGarbageCollection>true</ServerGarbageCollection>
```

---

## 最佳实践总结

| 项目 | 建议 | 优先级 |
|-----|------|-------|
| 使用 Release 编译 | 生产环境必须 | ⭐⭐⭐ |
| 启用服务器 GC | 高并发场景 | ⭐⭐ |
| HttpClient 连接池 | 避免创建多个实例 | ⭐⭐⭐ |
| 异步处理所有 I/O | 提高吞吐量 | ⭐⭐⭐ |
| 监控系统资源 | 及时发现问题 | ⭐⭐ |
| 设置合理超时 | 避免无限等待 | ⭐⭐ |

---

**文档版本**: 1.0  
**最后更新**: 2026年2月  
**作者**: HTTP/2 Echo Server 项目
