using System.Collections.Concurrent;
using GrpcLoadBalancing.Shared;

namespace GrpcLoadBalancing.Client;

class Program
{
    private static readonly ConcurrentDictionary<int, int> ServerCallCounts = new();
    private static LoadBalancer? _loadBalancer;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== gRPC Client Load Balancing Test ===\n");

        // 解析参数
        // 格式: dns://hostname:port 或 http://host1:port1,http://host2:port2,http://host3:port3
        var target = args.Length > 0 ? args[0] : "http://localhost:50051,http://localhost:50052,http://localhost:50053";
        var testDuration = args.Length > 1 && int.TryParse(args[1], out var duration) ? duration : 60;
        var requestInterval = args.Length > 2 && int.TryParse(args[2], out var interval) ? interval : 1000;

        Console.WriteLine($"Target: {target}");
        Console.WriteLine($"Test Duration: {testDuration} seconds");
        Console.WriteLine($"Request Interval: {requestInterval} ms\n");

        // 创建负载均衡器
        _loadBalancer = new LoadBalancer(target);
        
        // 等待初始 DNS 解析
        await Task.Delay(2000);

        // 显示发现的端点
        var endpoints = _loadBalancer.GetEndpoints();
        Console.WriteLine($"Discovered {endpoints.Count} endpoint(s):");
        foreach (var endpoint in endpoints)
        {
            Console.WriteLine($"  - {endpoint.Address} (Healthy: {endpoint.Healthy})");
        }
        Console.WriteLine();

        // 运行测试
        await RunLoadBalancingTest(testDuration, requestInterval);

        // 清理
        _loadBalancer?.Dispose();
    }

    static async Task RunLoadBalancingTest(int durationSeconds, int intervalMs)
    {
        var endTime = DateTime.Now.AddSeconds(durationSeconds);
        var requestId = 0;
        var lastStatusTime = DateTime.Now;

        Console.WriteLine("Starting load balancing test...");
        Console.WriteLine("Press Ctrl+C to stop early\n");

        try
        {
            while (DateTime.Now < endTime)
            {
                requestId++;
                
                // 每10秒显示一次端点状态
                if (DateTime.Now - lastStatusTime > TimeSpan.FromSeconds(10))
                {
                    var endpoints = _loadBalancer!.GetEndpoints();
                    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] Endpoint Status:");
                    foreach (var endpoint in endpoints)
                    {
                        var status = endpoint.Healthy ? "✓ Healthy" : "✗ Unhealthy";
                        Console.WriteLine($"  {endpoint.Address}: {status} (Last check: {endpoint.LastHealthCheck:HH:mm:ss})");
                    }
                    Console.WriteLine();
                    lastStatusTime = DateTime.Now;
                }

                try
                {
                    var client = _loadBalancer!.GetClient();
                    var request = new HelloRequest
                    {
                        Name = $"Client-{requestId}",
                        Id = requestId
                    };

                    var reply = await client.SayHelloAsync(request);
                    
                    // 统计每个服务器的调用次数
                    ServerCallCounts.AddOrUpdate(reply.ServerPort, 1, (key, value) => value + 1);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Request {requestId}: " +
                                    $"Server {reply.ServerHost}:{reply.ServerPort} - " +
                                    $"{reply.Message}");

                    await Task.Delay(intervalMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Request {requestId} failed: {ex.Message}");
                    await Task.Delay(intervalMs);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test interrupted: {ex.Message}");
        }

        // 打印统计信息
        Console.WriteLine("\n=== Test Results ===");
        Console.WriteLine($"Total requests: {requestId}");
        Console.WriteLine("\nServer call distribution:");
        foreach (var kvp in ServerCallCounts.OrderBy(x => x.Key))
        {
            var percentage = requestId > 0 ? (double)kvp.Value / requestId * 100 : 0;
            Console.WriteLine($"  Port {kvp.Key}: {kvp.Value} calls ({percentage:F2}%)");
        }

        // 显示最终端点状态
        var finalEndpoints = _loadBalancer!.GetEndpoints();
        Console.WriteLine("\nFinal endpoint status:");
        foreach (var endpoint in finalEndpoints)
        {
            var status = endpoint.Healthy ? "✓ Healthy" : "✗ Unhealthy";
            Console.WriteLine($"  {endpoint.Address}: {status}");
        }
    }
}
