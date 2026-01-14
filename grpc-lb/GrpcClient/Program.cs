using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using GrpcLoadBalancing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcLoadBalancing.Client;

/// <summary>
/// 使用 gRPC .NET 内置功能的完整实现
/// 支持 DNS Resolver 和 Round-Robin Load Balancer
/// 参考: https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing
/// </summary>
// 使用 gRPC .NET 内置负载均衡的示例实现
// 要使用此实现，请将此类重命名为 Program 或使用 /main 编译选项
class Program
{
    private static readonly ConcurrentDictionary<int, int> ServerCallCounts = new();
    private static GrpcChannel? _channel;
    private static Greeter.GreeterClient? _client;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== gRPC Client Load Balancing (Built-in DNS + Round-Robin) ===\n");

        // 解析参数
        // 选项1: dns:///hostname:port (使用 DNS resolver)
        // 选项2: static:/// (使用静态地址列表)
        var target = args.Length > 0 ? args[0] : "static:///";
        var testDuration = args.Length > 1 && int.TryParse(args[1], out var duration) ? duration : 60;
        var requestInterval = args.Length > 2 && int.TryParse(args[2], out var interval) ? interval : 1000;

        Console.WriteLine($"Target: {target}");
        Console.WriteLine($"Test Duration: {testDuration} seconds");
        Console.WriteLine($"Request Interval: {requestInterval} ms\n");

        // 创建使用内置负载均衡的通道
        CreateChannelWithBuiltInLoadBalancing(target);

        if (_client == null)
        {
            Console.WriteLine("Failed to create gRPC client");
            return;
        }

        // 等待初始连接建立
        await Task.Delay(2000);

        // 运行测试
        await RunLoadBalancingTest(testDuration, requestInterval);

        // 清理
        _channel?.Dispose();
    }

    static void CreateChannelWithBuiltInLoadBalancing(string target)
    {
        try
        {
            // 配置依赖注入
            var services = new ServiceCollection();

            if (target.StartsWith("dns://"))
            {
                // 使用 DNS Resolver
                // DNS resolver 会自动查询 DNS 获取地址列表
                // 格式: dns:///hostname:port 或 dns:///hostname (默认端口80)
                services.AddSingleton<ResolverFactory>(
                    sp => new DnsResolverFactory(refreshInterval: TimeSpan.FromSeconds(30)));

                Console.WriteLine("Using DNS Resolver (refreshes every 30 seconds)");
            }
            else if (target.StartsWith("static://"))
            {
                // 使用 Static Resolver - 指定静态地址列表
                var staticAddresses = new[]
                {
                    new BalancerAddress("localhost", 50051),
                    new BalancerAddress("localhost", 50052),
                    new BalancerAddress("localhost", 50053)
                };

                services.AddSingleton<ResolverFactory>(
                    sp => new StaticResolverFactory(addr => staticAddresses));

                Console.WriteLine($"Using Static Resolver with {staticAddresses.Length} addresses:");
                foreach (var addr in staticAddresses)
                {
                    Console.WriteLine($"  - {addr.EndPoint}");
                }
            }
            else
            {
                throw new ArgumentException("Target must start with 'dns://' or 'static://'");
            }

            var serviceProvider = services.BuildServiceProvider();

            // 配置 ServiceConfig 使用 round_robin 负载均衡
            // 可选: "pick_first" (默认) 或 "round_robin"
            var serviceConfig = new ServiceConfig
            {
                LoadBalancingConfigs = { new LoadBalancingConfig("round_robin") }
            };

            Console.WriteLine("Load Balancer: round_robin\n");

            // 创建通道
            _channel = GrpcChannel.ForAddress(
                target,
                new GrpcChannelOptions
                {
                    Credentials = ChannelCredentials.Insecure,
                    ServiceConfig = serviceConfig,
                    ServiceProvider = serviceProvider
                });

            _client = new Greeter.GreeterClient(_channel);
            Console.WriteLine("gRPC channel created successfully\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating channel: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    static async Task RunLoadBalancingTest(int durationSeconds, int intervalMs)
    {
        var endTime = DateTime.Now.AddSeconds(durationSeconds);
        var requestId = 0;

        Console.WriteLine("Starting load balancing test...");
        Console.WriteLine("Press Ctrl+C to stop early\n");

        try
        {
            while (DateTime.Now < endTime)
            {
                requestId++;
                try
                {
                    var request = new HelloRequest
                    {
                        Name = $"Client-{requestId}",
                        Id = requestId
                    };

                    var reply = await _client!.SayHelloAsync(request);
                    
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
    }
}
