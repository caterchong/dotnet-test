# 🚀 5 分钟快速开始

## 1️⃣ 编译项目 (30 秒)

```bash
cd http2c
dotnet build -c Release
```

## 2️⃣ 启动服务 (1 分钟)

**终端 1:**
```bash
dotnet run -c Release
# 或指定端口:
dotnet run -c Release -- --port=8888
```

您应该看到:
```
HTTP/2 Echo Server 启动在: http://localhost:3000
支持的端点:
  POST http://localhost:3000/echo - 原样返回请求体
  GET/POST/PUT/DELETE http://localhost:3000/echo/path - 返回请求详情 (JSON)
  GET http://localhost:3000/health - 健康检查

提示: 运行压测使用: dotnet run benchmark
```

## 3️⃣ 测试服务 (2 分钟)

**终端 2 - 快速测试:**

```bash
# 健康检查
curl http://localhost:3000/health

# 发送数据
echo "Hello Echo Server!" | curl -X POST -d @- http://localhost:3000/echo

# 发送 JSON
curl -X POST http://localhost:3000/echo \
  -H "Content-Type: application/json" \
  -d '{"test":"HTTP/2 Echo","time":"2026-02-10"}'
```

## 4️⃣ 运行压测 (2 分钟)

**终端 2 - 压测:**

```bash
# 运行所有压测场景
dotnet run -c Release -- benchmark

# 或使用 PowerShell 脚本
.\run-benchmark.ps1
```

---

## 📊 预期输出

```
╔══════════════════════════════════════════════════════╗
║      HTTP/2 Echo Server - 性能压测工具             ║
╚══════════════════════════════════════════════════════╝

✓ 服务器连接成功 (端口: 3000)

=== 基本 Echo 功能测试 (重复数: 100) ===
总耗时: XXX ms
平均响应时间: X.XX ms
吞吐量: X,XXX.XX req/s

[更多测试结果...]

╔══════════════════════════════════════════════════════╗
║              性能测试总结                          ║
╚══════════════════════════════════════════════════════╝

最高吞吐量: XX,XXX.XX req/s
```

---

## 🎯 关键端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/health` | GET | 健康检查 |
| `/echo` | POST | Echo 原始数据 |
| `/echo/{path}` | GET/POST/PUT/DELETE | 返回请求信息 |

---

## 💡 常用命令

### 指定端口启动
```bash
dotnet run -c Release -- --port=9000
```

### 环境变量方式
```bash
# Windows
$env:PORT="8080"; dotnet run -c Release

# Linux/macOS  
PORT=8080 dotnet run -c Release
```

### 后台运行 (Linux)
```bash
nohup dotnet run -c Release > echo-server.log 2>&1 &
```

### 后台运行 (Windows PowerShell)
```bash
Start-Process dotnet -ArgumentList @("run", "-c", "Release") -WindowStyle Hidden
```

---

## 📋 端口号建议

| 用途 | 端口 | 使用方式 |
|------|------|----------|
| 开发测试 | 3000 | `dotnet run` (默认) |
| 预发环境 | 8080 | `dotnet run -- --port=8080` |
| 压力测试 | 9000 | `dotnet run -- --port=9000` |
| Docker | 5000 | 容器内部端口 |

---

## ✅ 快速验证列表

- [ ] 项目编译成功 (无错误)
- [ ] 服务正常启动 (显示端口信息)
- [ ] 健康检查通过 (`curl /health` 返回 200)
- [ ] Echo 功能正常 (POST 数据被原样返回)
- [ ] 压测运行成功 (显示性能统计)

---

## 🔗 下一步

- 📖 阅读 [README.md](README.md) 了解详细用法
- 📊 查看 [PERFORMANCE_TESTING_PLAN.md](PERFORMANCE_TESTING_PLAN.md) 了解压测方案
- 🧪 参考 [CLIENT_TESTING_GUIDE.md](CLIENT_TESTING_GUIDE.md) 进行高级测试
- 📈 查看 [PROJECT_STATUS.md](PROJECT_STATUS.md) 了解项目状态

---

**就这么简单! 享受您的 HTTP/2 Echo 服务! 🎉**
