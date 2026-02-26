using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

public class Http2EchoBenchmark
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    
    public Http2EchoBenchmark(string baseUrl = "http://localhost:5000")
    {
        _baseUrl = baseUrl;
        
        // 配置 HttpClient 专门使用 HTTP/2
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        
        _httpClient = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version20
        };
    }

    /// <summary>
    /// 测试基本的 Echo 功能响应时间
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkBasicEcho(int iterations = 100)
    {
        Console.WriteLine($"\n=== 基本 Echo 功能测试 (重复数: {iterations}) ===");
        
        var stopwatch = Stopwatch.StartNew();
        var testData = "Hello, HTTP/2 Echo Server! " + new string('x', 1024);
        var content = new StringContent(testData);
        
        try
        {
            for (int i = 0; i < iterations; i++)
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/echo", content);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return new BenchmarkResult { ErrorMessage = ex.Message };
        }
        
        stopwatch.Stop();
        
        var result = new BenchmarkResult
        {
            TestName = "BasicEcho",
            Iterations = iterations,
            TotalMilliseconds = stopwatch.ElapsedMilliseconds,
            AverageMilliseconds = (double)stopwatch.ElapsedMilliseconds / iterations,
            RequestsPerSecond = (iterations * 1000.0) / stopwatch.ElapsedMilliseconds
        };
        
        Console.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"平均响应时间: {result.AverageMilliseconds:F3} ms");
        Console.WriteLine($"吞吐量: {result.RequestsPerSecond:F2} req/s");
        
        return result;
    }

    /// <summary>
    /// 测试并发处理能力
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkConcurrency(int concurrentRequests = 50, int requestsPerConnection = 10)
    {
        Console.WriteLine($"\n=== 并发处理能力测试 (并发数: {concurrentRequests}, 每连接请求数: {requestsPerConnection}) ===");
        
        var stopwatch = Stopwatch.StartNew();
        var testData = "Concurrent test data - " + new string('x', 512);
        var totalRequests = concurrentRequests * requestsPerConnection;
        var tasks = new List<Task>();
        
        try
        {
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var content = new StringContent(testData);
                    for (int j = 0; j < requestsPerConnection; j++)
                    {
                        var response = await _httpClient.PostAsync($"{_baseUrl}/echo", content);
                        response.EnsureSuccessStatusCode();
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return new BenchmarkResult { ErrorMessage = ex.Message };
        }
        
        stopwatch.Stop();
        
        var result = new BenchmarkResult
        {
            TestName = "Concurrency",
            ConcurrentRequests = concurrentRequests,
            Iterations = totalRequests,
            TotalMilliseconds = stopwatch.ElapsedMilliseconds,
            AverageMilliseconds = (double)stopwatch.ElapsedMilliseconds / totalRequests,
            RequestsPerSecond = (totalRequests * 1000.0) / stopwatch.ElapsedMilliseconds
        };
        
        Console.WriteLine($"总请求数: {totalRequests}");
        Console.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"平均响应时间: {result.AverageMilliseconds:F3} ms");
        Console.WriteLine($"吞吐量: {result.RequestsPerSecond:F2} req/s");
        
        return result;
    }

    /// <summary>
    /// 测试大数据处理
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkLargePayload(int payloadSizeKB = 1024, int iterations = 20)
    {
        Console.WriteLine($"\n=== 大数据处理测试 (数据大小: {payloadSizeKB} KB, 重复数: {iterations}) ===");
        
        var stopwatch = Stopwatch.StartNew();
        var largeData = new string('x', payloadSizeKB * 1024);
        
        try
        {
            for (int i = 0; i < iterations; i++)
            {
                var content = new StringContent(largeData);
                var response = await _httpClient.PostAsync($"{_baseUrl}/echo", content);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return new BenchmarkResult { ErrorMessage = ex.Message };
        }
        
        stopwatch.Stop();
        
        var result = new BenchmarkResult
        {
            TestName = "LargePayload",
            Iterations = iterations,
            PayloadSizeKB = payloadSizeKB,
            TotalMilliseconds = stopwatch.ElapsedMilliseconds,
            AverageMilliseconds = (double)stopwatch.ElapsedMilliseconds / iterations,
            RequestsPerSecond = (iterations * 1000.0) / stopwatch.ElapsedMilliseconds
        };
        
        Console.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"平均响应时间: {result.AverageMilliseconds:F3} ms");
        Console.WriteLine($"吞吐量: {result.RequestsPerSecond:F2} req/s");
        
        return result;
    }

    /// <summary>
    /// 压力测试 - 持续发送请求
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkStressTest(int durationSeconds = 10, int concurrentRequests = 100)
    {
        Console.WriteLine($"\n=== 压力测试 (持续时间: {durationSeconds}s, 并发数: {concurrentRequests}) ===");
        
        var stopwatch = Stopwatch.StartNew();
        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
        var testData = "Stress test data";
        var requestCount = 0;
        var errorCount = 0;
        var tasks = new List<Task>();
        
        try
        {
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var content = new StringContent(testData);
                            var response = await _httpClient.PostAsync($"{_baseUrl}/echo", content, cts.Token);
                            response.EnsureSuccessStatusCode();
                            Interlocked.Increment(ref requestCount);
                        }
                        catch when (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        catch
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                }, cts.Token));
            }
            
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when duration is reached
        }
        
        stopwatch.Stop();
        
        var result = new BenchmarkResult
        {
            TestName = "StressTest",
            Iterations = requestCount,
            ErrorCount = errorCount,
            ConcurrentRequests = concurrentRequests,
            TotalMilliseconds = stopwatch.ElapsedMilliseconds,
            AverageMilliseconds = requestCount > 0 ? (double)stopwatch.ElapsedMilliseconds / requestCount : 0,
            RequestsPerSecond = (requestCount * 1000.0) / stopwatch.ElapsedMilliseconds
        };
        
        Console.WriteLine($"成功请求: {requestCount}");
        Console.WriteLine($"失败请求: {errorCount}");
        Console.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"吞吐量: {result.RequestsPerSecond:F2} req/s");
        
        return result;
    }
}

public class BenchmarkResult
{
    public string TestName { get; set; } = "";
    public int Iterations { get; set; }
    public int ConcurrentRequests { get; set; }
    public int PayloadSizeKB { get; set; }
    public long TotalMilliseconds { get; set; }
    public double AverageMilliseconds { get; set; }
    public double RequestsPerSecond { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorMessage { get; set; }

    public override string ToString()
    {
        return $"[{TestName}] Iterations: {Iterations}, Avg: {AverageMilliseconds:F3}ms, RPS: {RequestsPerSecond:F2}";
    }
}
