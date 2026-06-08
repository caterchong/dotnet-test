using System.Diagnostics.Metrics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using MySqlConnector;

namespace MySqlConnectorCmp.Benchmarks;

/// <summary>
/// 测试 MySqlConnection 不同使用方式的性能开销
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[RankColumn]
public class ConnectionPatternBenchmark
{
    private string _connectionString = null!;
    private PoolMonitor? _poolMonitor;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connectionString = Environment.GetEnvironmentVariable("MYSQL_CONN")
            ?? "Server=127.0.0.1;Port=3306;Database=bench_test;User=root;Password=;Pooling=true;MinimumPoolSize=8;MaximumPoolSize=32;ConnectionReset=false;ConnectionIdleTimeout=180;";
// "Server=127.0.0.1;Port=3306;Database=bench_test;User=root;Password=;Pooling=true;MinimumPoolSize=8;MaximumPoolSize=32;ConnectionReset=false;ConnectionIdleTimeout=180;";
        _poolMonitor = new PoolMonitor();
        _poolMonitor.Start();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _poolMonitor?.Stop();
    }

    /// <summary>
    /// 仅构造对象，不 Open
    /// </summary>
    [Benchmark(Description = "new MySqlConnection()")]
    public MySqlConnection NewConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    /// <summary>
    /// 串行：构造 + OpenAsync + DisposeAsync
    /// </summary>
    [Benchmark(Description = "OpenAsync (serial)")]
    public async Task OpenAndDisposeAsync()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
    }

    private async Task OpenOnceAsync()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
    }

    private class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddColumn(StatisticColumn.P95);
        }
    }
}

/// <summary>
/// 后台定时采集 MySqlConnector 连接池指标并输出
/// </summary>
internal class PoolMonitor
{
    private readonly MeterListener _listener;
    private long _idleConnections;
    private long _usedConnections;
    private long _pendingRequests;
    private PeriodicTimer? _timer;
    private Task? _monitorTask;

    public PoolMonitor()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "MySqlConnector")
                    listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            switch (instrument.Name)
            {
                case "db.client.connections.usage":
                    var connState = GetTagValue(tags, "state");
                    if (connState == "idle") Interlocked.Add(ref _idleConnections, measurement);
                    else if (connState == "used") Interlocked.Add(ref _usedConnections, measurement);
                    break;
                case "db.client.connections.pending_requests":
                    Interlocked.Add(ref _pendingRequests, measurement);
                    break;
            }
        });

        _listener.Start();
    }

    public void Start()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        _monitorTask = RunAsync();
    }

    public void Stop()
    {
        _timer?.Dispose();
        _monitorTask?.Wait();
        _listener.Dispose();
    }

    private async Task RunAsync()
    {
        while (await _timer!.WaitForNextTickAsync())
        {
            var idle = Interlocked.Read(ref _idleConnections);
            var used = Interlocked.Read(ref _usedConnections);
            var pending = Interlocked.Read(ref _pendingRequests);
            var total = idle + used;
            Console.WriteLine($"[PoolMonitor] idle={idle}, used={used}, total={total}, pending={pending}");
        }
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].Key == key) return tags[i].Value?.ToString();
        }
        return null;
    }
}
