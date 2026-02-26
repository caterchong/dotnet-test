using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

var baseUrl = "http://localhost:7777";

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║      HTTP/2 Echo Server - 性能压测工具             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

Console.WriteLine("正在检查服务器连接...");
var handler = new SocketsHttpHandler();
using (var client = new HttpClient(handler) 
{ 
    Timeout = TimeSpan.FromSeconds(5),
    DefaultRequestVersion = HttpVersion.Version20
})
{
    try
    {
        var response = await client.GetAsync($"{baseUrl}/health");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("❌ 无法连接到服务器。请确保 Echo 服务在运行。");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 无法连接到服务器: {ex.Message}");
        Console.WriteLine($"异常类型: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"内部异常: {ex.InnerException.Message}");
        }
        return;
    }
}
Console.WriteLine("✓ 服务器连接成功\n");

var benchmark = new Http2EchoBenchmark(baseUrl);
var results = new List<BenchmarkResult>();

results.Add(await benchmark.BenchmarkBasicEcho(iterations: 100));
results.Add(await benchmark.BenchmarkConcurrency(concurrentRequests: 50, requestsPerConnection: 10));
results.Add(await benchmark.BenchmarkLargePayload(payloadSizeKB: 512, iterations: 20));
results.Add(await benchmark.BenchmarkStressTest(durationSeconds: 10, concurrentRequests: 100));

PrintSummary(results);

void PrintSummary(List<BenchmarkResult> results)
{
    Console.WriteLine("\n");
    Console.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("║              性能测试总结                          ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

    var table = new[]
    {
        new[] { "测试名称", "总请求数", "平均响应时间(ms)", "吞吐量(req/s)", "错误数" },
        new[] { "─────────", "────────", "──────────────", "──────────", "────" }
    };

    foreach (var table_row in table)
    {
        Console.WriteLine(string.Join(" │ ", table_row));
    }

    foreach (var result in results)
    {
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            continue;

        var testName = result.TestName switch
        {
            "BasicEcho" => "基本Echo",
            "Concurrency" => "并发能力",
            "LargePayload" => "大数据",
            "StressTest" => "压力测试",
            _ => result.TestName
        };

        var row = new[]
        {
            testName.PadRight(9),
            result.Iterations.ToString().PadRight(8),
            result.AverageMilliseconds.ToString("F3").PadRight(14),
            result.RequestsPerSecond.ToString("F2").PadRight(10),
            result.ErrorCount.ToString().PadRight(4)
        };

        Console.WriteLine(string.Join(" │ ", row));
    }

    var maxRps = results.Where(r => r.ErrorMessage == null).Max(r => r.RequestsPerSecond);
    Console.WriteLine($"\n最高吞吐量: {maxRps:F2} req/s");
}
