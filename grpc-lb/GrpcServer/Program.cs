using GrpcLoadBalancing.Server;
using GrpcLoadBalancing.Shared;

var builder = WebApplication.CreateBuilder(args);

// 从命令行参数或环境变量获取端口
var port = args.Length > 0 && int.TryParse(args[0], out var p) 
    ? p 
    : int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "50051");

var hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? "localhost";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port, listenOptions =>
    {
        // 同时支持 HTTP/1.1（用于健康检查）和 HTTP/2（用于 gRPC）
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<GreeterService>(sp => new GreeterService(port, hostname));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

// 添加 HTTP 健康检查端点（用于 Kubernetes 健康检查）
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine($"gRPC Server starting on port {port}, hostname: {hostname}");
Console.WriteLine($"Listening on http://0.0.0.0:{port}");

app.Run();
