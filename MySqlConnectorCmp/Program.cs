using System.Diagnostics.Metrics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using MySqlConnectorCmp.Benchmarks;
using MySqlConnectorCmp.Services;

// ── 命令行模式切换 ─────────────────────────────────────────────────────────────
// --benchmark : 运行 BenchmarkDotNet 基准测试
// --smoke     : 快速冒烟测试（验证两种模式能否正常工作）
// (默认)      : 启动 HTTP 服务
// ────────────────────────────────────────────────────────────────────────────────

var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONN")
    ?? "Server=127.0.0.1;Port=3306;Database=bench_test;User=root;Password=;";

if (args.Contains("--benchmark"))
{
    Console.WriteLine("=== MySqlConnector: 单连接复用 vs 每次查询新建连接 基准测试 ===\n");
    var config = DefaultConfig.Instance;
    BenchmarkRunner.Run<ConnectionPatternBenchmark>(config);
    return;
}

if (args.Contains("--smoke"))
{
    await RunSmokeTestAsync(connectionString);
    return;
}

// ── HTTP 服务模式 ──────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// 注册连接池指标监听
builder.Services.AddSingleton<PoolMetricsListener>();

var app = builder.Build();

var singleService = new SingleConnectionService(connectionString);
var perQueryService = new PerQueryConnectionService(connectionString);
var poolListener = app.Services.GetRequiredService<PoolMetricsListener>();

// GET /single — 模式1: 单连接复用
app.MapGet("/single", async (CancellationToken ct) =>
{
    var result = await singleService.ExecuteAsync(ct);
    return Results.Ok(new { mode = "single", result });
});

// GET /per-query — 模式2: 每次查询新建连接
app.MapGet("/per-query", async (CancellationToken ct) =>
{
    var result = await perQueryService.ExecuteAsync(ct);
    return Results.Ok(new { mode = "per-query", result });
});

// GET /pool-info — 连接池状态
app.MapGet("/pool-info", () =>
{
    var snapshot = poolListener.GetSnapshot();
    return Results.Ok(snapshot);
});

app.Run();

// ── 冒烟测试 ───────────────────────────────────────────────────────────────────
static async Task RunSmokeTestAsync(string connectionString)
{
    Console.WriteLine("=== 冒烟测试：单连接复用 vs 每次查询新建连接 ===\n");

    var single = new SingleConnectionService(connectionString);
    var perQuery = new PerQueryConnectionService(connectionString);

    Console.WriteLine("[模式1] 单连接复用 — 一个请求内复用同一 MySqlConnection");
    var sw1 = System.Diagnostics.Stopwatch.StartNew();
    var result1 = await single.ExecuteAsync();
    sw1.Stop();
    Console.WriteLine($"  结果: {result1}");
    Console.WriteLine($"  耗时: {sw1.ElapsedMilliseconds}ms\n");

    Console.WriteLine("[模式2] 每次查询新建连接 — 每个 DB 操作独立 open/close");
    var sw2 = System.Diagnostics.Stopwatch.StartNew();
    var result2 = await perQuery.ExecuteAsync();
    sw2.Stop();
    Console.WriteLine($"  结果: {result2}");
    Console.WriteLine($"  耗时: {sw2.ElapsedMilliseconds}ms\n");

    Console.WriteLine("关键观察:");
    Console.WriteLine("  1. 模式1 只 open/close 一次连接，模式2 open/close 三次");
    Console.WriteLine("  2. 连接池开启时(Pooling=true)，模式2 的 open/close 会从池中借还连接");
    Console.WriteLine("  3. 高并发下，模式2 会向连接池借还更多次，可能增加等待时间");
    Console.WriteLine("  4. 模式1 在请求期间独占连接，其他请求可能等待");
    Console.WriteLine("\n启动 HTTP 服务: dotnet run");
    Console.WriteLine("  GET /single     — 模式1");
    Console.WriteLine("  GET /per-query  — 模式2");
    Console.WriteLine("  GET /pool-info  — 连接池状态");
    Console.WriteLine("运行基准测试: dotnet run -- --benchmark");
}

// ── 连接池指标监听器 ───────────────────────────────────────────────────────────
public class PoolMetricsListener : IDisposable
{
    private readonly MeterListener _listener;
    private long _idleConnections;
    private long _usedConnections;
    private long _pendingRequests;
    private long _timeouts;

    public PoolMetricsListener()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "MySqlConnector")
                    listener.EnableMeasurementEvents(instrument);
            }
        };

        // MySqlConnector 的 usage/pending_requests 使用 int 类型的 UpDownCounter
        _listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            switch (instrument.Name)
            {
                case "db.client.connections.usage":
                    // UpDownCounter: measurement 是增量 (+1/-1)，累加
                    var stateValue = GetTagValue(tags, "state");
                    if (stateValue == "idle") Interlocked.Add(ref _idleConnections, measurement);
                    else if (stateValue == "used") Interlocked.Add(ref _usedConnections, measurement);
                    break;
                case "db.client.connections.pending_requests":
                    Interlocked.Add(ref _pendingRequests, measurement);
                    break;
                case "db.client.connections.timeouts":
                    Interlocked.Add(ref _timeouts, measurement);
                    break;
            }
        });

        _listener.Start();
    }

    public PoolSnapshot GetSnapshot() => new()
    {
        IdleConnections = Interlocked.Read(ref _idleConnections),
        UsedConnections = Interlocked.Read(ref _usedConnections),
        PendingRequests = Interlocked.Read(ref _pendingRequests),
        Timeouts = Interlocked.Read(ref _timeouts),
    };

    public void Dispose() => _listener.Dispose();

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].Key == key) return tags[i].Value?.ToString();
        }
        return null;
    }
}

public record PoolSnapshot
{
    public long IdleConnections { get; init; }
    public long UsedConnections { get; init; }
    public long PendingRequests { get; init; }
    public long Timeouts { get; init; }
}
