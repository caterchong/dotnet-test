using System.Diagnostics;
using System.Text;

string baseUrl = "http://localhost:8080";
int concurrency = 10;
int totalRequests = 1000;
int durationSeconds = 0;
string? bodySizeArg = null;

for (int i = 0; i < args.Length; i++)
{
    string a = args[i];
    if (a.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || a.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        baseUrl = a.TrimEnd('/');
        continue;
    }
    if (a == "-c" && i + 1 < args.Length) { concurrency = int.Parse(args[++i]); continue; }
    if (a.StartsWith("-c", StringComparison.OrdinalIgnoreCase) && a.Length > 2) { concurrency = int.Parse(a.AsSpan(2)); continue; }
    if (a == "-n" && i + 1 < args.Length) { totalRequests = int.Parse(args[++i]); continue; }
    if (a.StartsWith("-n", StringComparison.OrdinalIgnoreCase) && a.Length > 2) { totalRequests = int.Parse(a.AsSpan(2)); continue; }
    if (a == "-d" && i + 1 < args.Length) { durationSeconds = int.Parse(args[++i]); continue; }
    if (a.StartsWith("-d", StringComparison.OrdinalIgnoreCase) && a.Length > 2) { durationSeconds = int.Parse(a.AsSpan(2)); continue; }
    if (a == "-b" && i + 1 < args.Length) { bodySizeArg = args[++i]; continue; }
    if (a.StartsWith("-b", StringComparison.OrdinalIgnoreCase) && a.Length > 2) { bodySizeArg = a.Substring(2); continue; }
}

int bodySize = 0;
if (!string.IsNullOrEmpty(bodySizeArg))
{
    bodySize = bodySizeArg.EndsWith("k", StringComparison.OrdinalIgnoreCase)
        ? int.Parse(bodySizeArg.AsSpan(0, bodySizeArg.Length - 1)) * 1024
        : int.Parse(bodySizeArg);
}

var payload = bodySize > 0 ? new string('x', bodySize) : "hello";
var content = new StringContent(payload, Encoding.UTF8, "text/plain");

using var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = concurrency * 2,
    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
};
using var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine($"压测: {baseUrl}/echo");
Console.WriteLine($"并发: {concurrency}, 请求体: {payload.Length} bytes");
if (durationSeconds > 0)
    Console.WriteLine($"时长: {durationSeconds}s");
else
    Console.WriteLine($"总请求数: {totalRequests}");
Console.WriteLine();

var sw = Stopwatch.StartNew();
var completed = 0;
var errors = 0;
// 时长模式(-d)下不限制请求数，按请求数模式(-n)下限制
var limitByCount = durationSeconds <= 0 && totalRequests > 0;
var remaining = limitByCount ? totalRequests : int.MaxValue;
var latencies = new List<long>(limitByCount ? Math.Min(totalRequests, 500_000) : 500_000);
var endTime = durationSeconds > 0 ? DateTime.UtcNow.AddSeconds(durationSeconds) : (DateTime?)null;

async Task RunOne()
{
    while (true)
    {
        if (endTime.HasValue && DateTime.UtcNow >= endTime.Value) break;
        if (limitByCount)
        {
            var left = Interlocked.Decrement(ref remaining);
            if (left < 0) break;
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            var res = await client.PostAsync("/echo", content);
            res.EnsureSuccessStatusCode();
            Interlocked.Increment(ref completed);
            lock (latencies) latencies.Add((Stopwatch.GetTimestamp() - start) * 1_000_000 / Stopwatch.Frequency);
        }
        catch
        {
            Interlocked.Increment(ref errors);
        }
    }
}

var tasks = Enumerable.Range(0, concurrency).Select(_ => RunOne()).ToArray();
await Task.WhenAll(tasks);
sw.Stop();

var total = completed + errors;
var rps = total / sw.Elapsed.TotalSeconds;
latencies.Sort();
long p50 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.5)] : 0;
long p95 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.95)] : 0;
long p99 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.99)] : 0;

Console.WriteLine($"总请求: {total}, 成功: {completed}, 失败: {errors}");
Console.WriteLine($"QPS: {rps:F0}");
Console.WriteLine($"延迟(ms): P50={p50 / 1000.0:F2}, P95={p95 / 1000.0:F2}, P99={p99 / 1000.0:F2}");
Console.WriteLine($"耗时: {sw.ElapsedMilliseconds}ms");
