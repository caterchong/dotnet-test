# 📑 HTTP/2 Echo Server - 项目文件索引

## 📂 项目目录结构

```
http2c/
├── 📄 核心文件
│   ├── Program.cs                      ⭐ 主程序（Echo服务 + 压测入口）
│   ├── Http2EchoBenchmark.cs           ⭐ 性能测试基准类
│   ├── Http2EchoServer.csproj          ⭐ 项目配置文件
│   └── appsettings.json                📋 应用设置
│
├── 📚 文档文件 (必读!)
│   ├── 🟢 QUICK_START.md               👈 从这里开始! (5分钟)
│   ├── 🟡 README.md                    📖 完整使用指南
│   ├── 🔵 PERFORMANCE_TESTING_PLAN.md  📊 详细压测方案
│   ├── 🟣 CLIENT_TESTING_GUIDE.md      🧪 客户端测试命令
│   ├── 🔴 PROJECT_STATUS.md            ✅ 项目完成状态
│   └── 📋 INDEX.md                     📑 本文件
│
├── 🚀 启动脚本
│   ├── start-server.ps1                (Windows PowerShell)
│   ├── start-server.sh                 (Linux/macOS Bash)
│   └── run-benchmark.ps1               (Windows 压测脚本)
│
├── 🔧 编译输出
│   ├── bin/Release/net6.0/             (编译后的可执行文件)
│   └── obj/                            (编译临时文件)
│
└── 📋 其他
    └── requirements.txt                (原始需求说明)
```

---

## 📖 文档阅读顺序

### 🟢 第一步：快速开始 (5分钟)
**文件**: [QUICK_START.md](QUICK_START.md)

快速了解如何:
- 编译项目
- 启动服务
- 测试基本功能
- 运行压测

**适合人群**: 想快速上手的用户

---

### 🟡 第二步：完整指南 (15分钟)
**文件**: [README.md](README.md)

深入了解项目:
- 功能特性详解
- HTTP/2 协议说明
- 自定义压测配置
- 常见问题解答
- 性能评估标准

**适合人群**: 想全面了解项目的用户

---

### 🔵 第三步：压测方案 (30分钟)
**文件**: [PERFORMANCE_TESTING_PLAN.md](PERFORMANCE_TESTING_PLAN.md)

专业的压测指导:
- 4 个详细的测试场景
- 性能指标定义
- 执行步骤说明
- 结果分析方法
- 性能优化建议
- 常见性能问题解决

**适合人群**: 进行性能评估和优化的用户

---

### 🟣 第四步：客户端测试 (可选)
**文件**: [CLIENT_TESTING_GUIDE.md](CLIENT_TESTING_GUIDE.md)

实用的测试命令:
- curl 命令示例
- PowerShell 脚本
- 负载测试工具
- 监控命令

**适合人群**: 想进行手动测试或集成其他工具的用户

---

### 🔴 参考：项目状态
**文件**: [PROJECT_STATUS.md](PROJECT_STATUS.md)

完整的项目总结:
- 功能完成清单
- 技术栈说明
- 输出示例
- 快速问题解决

**适合人群**: 想快速了解项目整体情况

---

## 💻 源代码文件说明

### Program.cs - 主程序 (230+ 行)

**职责**:
- 配置 Kestrel HTTP/2 支持
- 实现 Echo 端点
- 健康检查端点
- 压测模式入口
- 性能报告输出

**关键特性**:
```csharp
// Echo 端点
app.MapPost("/echo", async (HttpContext context) => { ... });

// 信息端点
app.MapMethods("/echo/{*path}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, async (...) => { ... });

// 健康检查
app.MapGet("/health", () => { ... });
```

**入口方式**:
```bash
dotnet run                    # 启动服务
dotnet run benchmark          # 运行压测
dotnet run -- --port=9000    # 指定端口
```

---

### Http2EchoBenchmark.cs - 压测工具 (150+ 行)

**职责**:
- 输出性能测试逻辑
- 提供 4 个测试场景
- 收集和计算性能指标
- 生成测试报告

**提供的方法**:
```csharp
// 基本反应时间测试
BenchmarkBasicEcho(iterations)

// 并发处理能力测试  
BenchmarkConcurrency(concurrentRequests, requestsPerConnection)

// 大数据处理测试
BenchmarkLargePayload(payloadSizeKB, iterations)

// 压力测试
BenchmarkStressTest(durationSeconds, concurrentRequests)
```

**性能指标**:
- 总耗时 (毫秒)
- 平均响应时间 (毫秒)
- 吞吐量 (requests/second)
- 错误数统计

---

### Http2EchoServer.csproj - 项目配置

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**说明**:
- 基于 ASP.NET Core Web SDK
- 目标框架: .NET 6.0
- 启用可为空（Nullable）引用类型
- 启用隐式 using 声明

---

### appsettings.json - 应用配置

```json
{
  "Logging": { ... },
  "AllowedHosts": "*"
}
```

**说明**:
- 日志级别配置
- 允许所有主机访问

---

## 🚀 启动脚本说明

### Windows PowerShell 脚本

#### start-server.ps1
```powershell
# 启动 Echo 服务
Write-Host "启动 Echo 服务..." -ForegroundColor Yellow
Write-Host "服务地址: http://localhost:3000" -ForegroundColor Cyan

dotnet run -- --urls "http://0.0.0.0:3000"
```

**用法**:
```bash
.\start-server.ps1
```

#### run-benchmark.ps1
```powershell
# 编译项目
dotnet build -c Release

# 检查服务连接
Invoke-WebRequest -Uri "http://localhost:3000/health" -TimeoutSec 5

# 运行压测
dotnet run --no-build -c Release -- benchmark
```

**用法**:
```bash
.\run-benchmark.ps1
```

---

### Linux/macOS Bash 脚本

#### start-server.sh
```bash
#!/bin/bash

echo "启动 Echo 服务..."
echo "服务地址: http://localhost:3000"
echo ""

dotnet run -- --urls "http://0.0.0.0:3000"
```

**用法**:
```bash
chmod +x start-server.sh
./start-server.sh
```

---

## 📦 编译输出

### bin/Release/net6.0/
- `Http2EchoServer.dll` - 主程序集
- `Http2EchoServer.exe` - Windows 可执行文件
- 依赖程序集
- 配置文件

### 运行编译后的程序
```bash
# Windows
.\bin\Release\net6.0\Http2EchoServer.exe

# Linux/macOS
./bin/Release/net6.0/Http2EchoServer
```

---

## 🔍 文件选择指南

### "我想快速尝试"
→ 打开 [QUICK_START.md](QUICK_START.md) (5 分钟)

### "我想了解完整功能"
→ 打开 [README.md](README.md) (15 分钟)

### "我想进行性能测试"
→ 打开 [PERFORMANCE_TESTING_PLAN.md](PERFORMANCE_TESTING_PLAN.md) (30 分钟)

### "我想手动测试服务"
→ 打开 [CLIENT_TESTING_GUIDE.md](CLIENT_TESTING_GUIDE.md)

### "我想查看项目完成情况"
→ 打开 [PROJECT_STATUS.md](PROJECT_STATUS.md)

### "我想了解源代码"
→ 查看 [Program.cs](Program.cs) 和 [Http2EchoBenchmark.cs](Http2EchoBenchmark.cs)

---

## 📊 关键数据

| 指标 | 值 |
|------|-----|
| **项目大小** | ~500 行代码 |
| **文档数量** | 6 份详细文档 |
| **压测场景** | 4 个 |
| **编译时间** | < 2 秒 |
| **启动时间** | < 1 秒 |
| **支持协议** | HTTP/1.1, HTTP/2 |

---

## 🎯 常见任务快速导航

| 任务 | 文件 | 命令 |
|------|------|------|
| 启动服务 | Program.cs | `dotnet run` |
| 运行压测 | Http2EchoBenchmark.cs | `dotnet run benchmark` |
| 修改压测配置 | Program.cs | 编辑第 25-50 行 |
| 自定义端口 | Program.cs | `dotnet run -- --port=9000` |
| 查看API文档 | README.md | 翻到"功能特性"部分 |
| 学习优化 | PERFORMANCE_TESTING_PLAN.md | 翻到"优化建议"部分 |
| 手动测试 | CLIENT_TESTING_GUIDE.md | 复制 curl/PowerShell 命令 |

---

## 🔗 文件依赖关系

```
Program.cs
  ├─ 使用 → Http2EchoBenchmark.cs
  ├─ 读取 → appsettings.json
  └─ 配置 → Kestrel (ASP.NET Core)

Http2EchoServer.csproj
  └─ 编译 → Program.cs + Http2EchoBenchmark.cs

*.ps1 (脚本)
  └─ 执行 → dotnet run
```

---

## 📋 文件大小参考

```
Program.cs                          ~8 KB
Http2EchoBenchmark.cs              ~6 KB
Http2EchoServer.csproj             ~0.3 KB
appsettings.json                   ~0.2 KB
README.md                          ~15 KB
PERFORMANCE_TESTING_PLAN.md        ~30 KB
CLIENT_TESTING_GUIDE.md            ~8 KB
QUICK_START.md                     ~3 KB
PROJECT_STATUS.md                  ~10 KB
---
总计:                              ~80 KB (源代码+文档)
```

---

## 🎓 学习路径建议

### 🟢 初级 (1 小时)
1. 阅读 QUICK_START.md (5分钟)
2. 尝试启动服务和基本测试 (10分钟)
3. 阅读 README.md 前半部分 (15分钟)
4. 自由探索 (30分钟)

### 🟡 中级 (2 小时)
1. 深入阅读 README.md (20分钟)
2. 研究 Program.cs 源代码 (20分钟)
3. 阅读 PERFORMANCE_TESTING_PLAN.md 前半部分 (20分钟)
4. 修改压测参数进行自定义测试 (30分钟)
5. 自由探索和实验 (30分钟)

### 🔵 高级 (4+ 小时)
1. 完整阅读所有文档 (1.5小时)
2. 深入研究源代码 (1小时)
3. 实施高级压测场景 (1小时)
4. 执行性能优化 (30分钟+)

---

## ✨ 最终提示

**如果您只有 5 分钟:**
→ 打开 [QUICK_START.md](QUICK_START.md)

**如果您有 30 分钟:**
→ 依次打开: QUICK_START.md → README.md → 尝试实操

**如果您有 2 小时:**
→ 阅读所有文档 + 尝试所有功能 + 修改代码

**如果您遇到问题:**
→ 查看 README.md 的"常见问题"部分

---

**祝您使用愉快！如有任何问题，请查阅相应的文档。** 🚀

*最后更新: 2026年2月10日*
