using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcPerformanceComparison;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcPerformanceComparison;

[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net90)]
public class GrpcBenchmarks
{
    private HelloRequest _request = null!;
    private byte[] _serializedRequest = null!;
    private Server? _grpcCoreServer;
    private Microsoft.AspNetCore.Hosting.IWebHost? _grpcDotnetServer;
    private Channel? _grpcCoreChannel;
    private GrpcChannel? _grpcDotnetChannel;
    private Greeter.GreeterClient? _grpcCoreClient;
    private Greeter.GreeterClient? _grpcDotnetClient;
    private const int GrpcCorePort = 50051;
    private const int GrpcDotnetPort = 50052;

    private bool _grpcCoreAvailable = false;

    [GlobalSetup]
    public void Setup()
    {
        // 创建测试请求
        _request = new HelloRequest
        {
            Name = "Performance Test",
            Id = 12345,
        };
        _request.Tags.AddRange(new[] { "tag1", "tag2", "tag3", "tag4", "tag5" });
        _request.Metadata.Add("key1", "value1");
        _request.Metadata.Add("key2", "value2");
        _request.Metadata.Add("key3", "value3");

        // 序列化请求用于测试序列化性能
        _serializedRequest = _request.ToByteArray();

        // 尝试启动 Grpc.Core 服务器（可能在某些平台上不可用）
        try
        {
            _grpcCoreServer = new Server
            {
                Services = { Greeter.BindService(new GreeterService()) },
                Ports = { new ServerPort("localhost", GrpcCorePort, ServerCredentials.Insecure) }
            };
            _grpcCoreServer.Start();
            _grpcCoreChannel = new Channel("localhost", GrpcCorePort, ChannelCredentials.Insecure);
            _grpcCoreClient = new Greeter.GreeterClient(_grpcCoreChannel);
            _grpcCoreClient.SayHello(_request); // 预热连接
            _grpcCoreAvailable = true;
            Console.WriteLine("✓ Grpc.Core 已成功初始化");
        }
        catch (Exception ex)
        {
            // 检查是否是原生库加载失败
            var errorMessage = ex.Message;
            if (errorMessage.Contains("native library") || errorMessage.Contains("libgrpc_csharp_ext"))
            {
                Console.WriteLine("⚠ 警告: Grpc.Core 原生库不可用（macOS ARM64 平台限制）");
                Console.WriteLine("   原因: Grpc.Core 需要原生 C++ 库，在某些平台上可能不可用");
                Console.WriteLine("   处理: 将跳过 Grpc.Core 测试，仅运行 grpc-dotnet 测试");
            }
            else
            {
                Console.WriteLine($"⚠ 警告: Grpc.Core 初始化失败: {ex.GetType().Name}");
                Console.WriteLine($"   原因: {ex.Message}");
                Console.WriteLine("   处理: 将跳过 Grpc.Core 测试");
            }
            _grpcCoreAvailable = false;
        }

        // 启动 grpc-dotnet 服务器
        _grpcDotnetServer = new WebHostBuilder()
            .UseKestrel(options =>
            {
                options.ListenLocalhost(GrpcDotnetPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            })
            .UseStartup<GrpcDotnetStartup>()
            .Build();
        _grpcDotnetServer.Start();

        // 创建 grpc-dotnet 客户端通道
        _grpcDotnetChannel = GrpcChannel.ForAddress($"http://localhost:{GrpcDotnetPort}");
        _grpcDotnetClient = new Greeter.GreeterClient(_grpcDotnetChannel);

        // 预热连接
        _grpcDotnetClient.SayHello(_request);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_grpcCoreAvailable)
        {
            _grpcCoreChannel?.ShutdownAsync().Wait();
            _grpcCoreServer?.ShutdownAsync().Wait();
        }
        _grpcDotnetChannel?.Dispose();
        _grpcDotnetServer?.StopAsync().Wait();
        _grpcDotnetServer?.Dispose();
    }

    // 序列化性能测试（两者都使用 Google.Protobuf，性能应该相同）
    [Benchmark(Baseline = true)]
    public byte[] Protobuf_Serialize()
    {
        return _request.ToByteArray();
    }

    [Benchmark]
    public HelloRequest Protobuf_Deserialize()
    {
        return HelloRequest.Parser.ParseFrom(_serializedRequest);
    }

    // RPC 调用性能测试
    [Benchmark]
    public HelloReply GrpcCore_RpcCall()
    {
        if (!_grpcCoreAvailable)
        {
            // 如果 Grpc.Core 不可用，抛出异常（BenchmarkDotNet 会跳过此测试）
            throw new InvalidOperationException("Grpc.Core is not available on this platform (native library missing)");
        }
        return _grpcCoreClient!.SayHello(_request);
    }

    [Benchmark]
    public HelloReply GrpcDotnet_RpcCall()
    {
        return _grpcDotnetClient!.SayHello(_request);
    }
}

// grpc-dotnet 服务器启动配置
public class GrpcDotnetStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<GreeterService>();
        });
    }
}
