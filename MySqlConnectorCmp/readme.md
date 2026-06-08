# MySqlConnectorCmp — MySqlConnection 使用方式对比测试

对比两种 MySqlConnection 使用方式下的连接池状态和性能差异。

## 项目结构

```
MySqlConnectorCmp/
├── MySqlConnectorCmp.csproj          # Web SDK + MySqlConnector + BenchmarkDotNet
├── Program.cs                         # Minimal API 入口 + 连接池监听器
├── Services/
│   ├── SingleConnectionService.cs     # 模式1: 单连接复用
│   └── PerQueryConnectionService.cs   # 模式2: 每次查询新建连接
├── Benchmarks/
│   └── ConnectionPatternBenchmark.cs  # BenchmarkDotNet 基准测试
└── setup.sql                          # 建表脚本
```

## 两种模式说明

每个请求流程：访问 3 次 DB（简单 SELECT），每次 DB 访问后做 idle 操作（计算 Fibonacci(30)），最后返回结果。

- **模式1 (SingleConnection)**: 请求开始 open 一次，3 次 SELECT 复用同一连接，请求结束 close — 连接池只借还 1 次
- **模式2 (PerQueryConnection)**: 每次 SELECT 独立 open/close — 连接池借还 3 次，高并发时可能增加等待时间

## 运行方式

| 命令 | 说明 |
|------|------|
| `dotnet run` | 启动 HTTP 服务 |
| `dotnet run -- --smoke` | 快速冒烟测试，验证两种模式 |
| `dotnet run -c Release -- --benchmark` | BenchmarkDotNet 基准测试 |

## HTTP Endpoints

| 路径 | 说明 |
|------|------|
| `GET /single` | 模式1: 一个请求内复用同一 MySqlConnection（3 次 SELECT + Fibonacci） |
| `GET /per-query` | 模式2: 每次查询新建 MySqlConnection（3 次 open/close + Fibonacci） |
| `GET /pool-info` | 返回连接池状态（空闲/使用中/等待中/超时数） |

## 使用 wrk 压测

BenchmarkDotNet 默认串行执行，无法模拟并发场景下连接池争抢的差异。使用 `wrk` 对 HTTP 端点压测更贴近真实场景：

```bash
# 先启动服务
dotnet run

# 另一个终端用 wrk 压测

# 模式1: 单连接复用
wrk -t4 -c100 -d30s http://localhost:5000/single

# 模式2: 每次查询新建连接
wrk -t4 -c100 -d30s http://localhost:5000/per-query

# 压测过程中随时查看连接池状态
curl http://localhost:5000/pool-info
```

`wrk` 的 `-c` 参数控制真实并发连接数，比 BenchmarkDotNet 的串行模拟更准确地反映连接池争抢的差异。

## 环境变量

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `MYSQL_CONN` | MySQL 连接字符串 | `Server=127.0.0.1;Port=3306;Database=bench_test;User=root;Password=;Pooling=true;MinimumPoolSize=8;MaximumPoolSize=32;ConnectionReset=false;ConnectionIdleTimeout=180;` |

关键参数是ConnectionReset=false, 如果不设置，性能会很差， 一次操作蜕化到67677 ns
官方文档(https://mysqlconnector.net/connection-options/)里面提出了这个参数设置成false的若干风险

```
If true, all connections retrieved from the pool will have been reset. The default value of true ensures that the connection is in the same state whether it’s newly created or retrieved from the pool. A value of false avoids making an additional server round trip to reset the connection, but the connection state is not reset, meaning that session variables and other session state changes from any previous use of the connection are carried over. Additionally (if Connection Reset is false), when MySqlConnection.Open returns a connection from the pool (instead of opening a new one), the connection may be invalid (and throw an exception on first use) if the server has closed the connection.
```

如果确定要选择性能， 必须要保证两点
1. mysql语句中用户自定义变量不要互相影响
2. transaction一定要用using， 确保异常情况下，也会Rollback


# 性能测试结论

| Method                  | Mean      | Error    | StdDev   | P95       | Rank | Gen0   | Allocated |
|------------------------ |----------:|---------:|---------:|----------:|-----:|-------:|----------:|
| 'new MySqlConnection()' |  36.71 ns | 0.482 ns | 0.427 ns |  37.35 ns |    1 | 0.0019 |     200 B |
| 'OpenAsync (serial)'    | 552.65 ns | 6.059 ns | 5.371 ns | 561.82 ns |    2 | 0.0057 |     648 B |


// * Hints *
Outliers
  ConnectionPatternBenchmark.'new MySqlConnection() (construct only)': Default -> 3 outliers were removed (40.50 ns..41.89 ns)
  ConnectionPatternBenchmark.'using + OpenAsync (full lifecycle)': Default     -> 2 outliers were removed, 3 outliers were detected (63.95 us, 68.84 us, 69.60 us)

// * Legends *
  Mean      : Arithmetic mean of all measurements
  Error     : Half of 99.9% confidence interval
  StdDev    : Standard deviation of all measurements
  P95       : Percentile 95
  Gen0      : GC Generation 0 collects per 1000 operations
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 ns      : 1 Nanosecond (0.000000001 sec)

