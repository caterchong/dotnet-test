# HTTP/1.1 Echo 服务与压测

基于 .NET 10、Microsoft.Extensions.Hosting、Kestrel 的 HTTP/1.1 回显服务，并附带压测工具。

## 环境

- .NET 10 SDK（如未安装可把两个项目的 `TargetFramework` 改为 `net9.0` 或 `net8.0`）

## 启动 Echo 服务

```bash
cd http1.1/EchoServer
dotnet run
```

服务监听 `http://0.0.0.0:8080`：

- **POST /echo**：原样返回请求体
- **GET /health**：健康检查

## 压测工具

```bash
cd http1.1/StressTest
dotnet run
```

默认：`http://localhost:8080`，并发 10，共 1000 次请求。

### 参数

| 参数 | 说明 |
|------|------|
| `URL` | 服务地址（默认 `http://localhost:8080`） |
| `-n N` | 总请求数（默认 1000） |
| `-c N` | 并发数（默认 10） |
| `-d N` | 压测时长（秒），与 `-n` 二选一 |
| `-b N` | 请求体大小（字节），如 `-b 1k` 表示 1024 字节 |

### 示例

```bash
# 默认 1000 次、并发 10
dotnet run

# 指定地址与 5000 次、并发 50
dotnet run -- http://localhost:8080 -n 5000 -c 50

# 压测 30 秒、并发 20
dotnet run -- http://localhost:8080 -d 30 -c 20

# 1KB 请求体
dotnet run -- -b 1k -n 2000 -c 20
```

输出包含：总请求数、成功/失败、QPS、P50/P95/P99 延迟(ms)。
