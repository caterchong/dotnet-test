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
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<GreeterService>(sp => new GreeterService(port, hostname));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

Console.WriteLine($"gRPC Server starting on port {port}, hostname: {hostname}");
Console.WriteLine($"Listening on http://0.0.0.0:{port}");

app.Run();
