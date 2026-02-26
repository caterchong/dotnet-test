using System.Net;
using System.Text;

// 正常服务器模式
var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel 服务器支持 HTTP/2
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // 0.0.0.0:7777 - h2c (HTTP/2 cleartext, prior-knowledge)
    serverOptions.ListenAnyIP(7777, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });

    // 0.0.0.0:7778 - HTTP/1.1
    serverOptions.ListenAnyIP(7778, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

var app = builder.Build();

// Echo 端点 - POST 方式
app.MapPost("/echo", async (HttpContext context) =>
{
    using (var reader = new StreamReader(context.Request.Body))
    {
        var requestBody = await reader.ReadToEndAsync();
        await context.Response.WriteAsync(requestBody);
    }
});

// Echo 端点 - 支持所有 HTTP 方法
app.MapMethods("/echo/{*path}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, async (HttpContext context) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;
    var body = "";

    if (context.Request.ContentLength > 0)
    {
        using (var reader = new StreamReader(context.Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }
    }

    var responseData = new
    {
        method,
        path = path.Value,
        timestamp = DateTime.UtcNow,
        body = body,
        headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
    };

    context.Response.ContentType = "application/json";
    var json = System.Text.Json.JsonSerializer.Serialize(responseData);
    await context.Response.WriteAsync(json);
});

// 简单的健康检查端点
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

const int H2C_PORT = 7777;
const int HTTP1_PORT = 7778;
Console.WriteLine($"HTTP/2 (h2c) Echo Server 启动在: http://0.0.0.0:{H2C_PORT}");
Console.WriteLine($"HTTP/1.1 Echo Server 启动在: http://0.0.0.0:{HTTP1_PORT}");
Console.WriteLine("支持的端点:");
Console.WriteLine($"  POST http://0.0.0.0:{H2C_PORT}/echo - 原样返回请求体 (h2c)");
Console.WriteLine($"  POST http://0.0.0.0:{HTTP1_PORT}/echo - 原样返回请求体 (http1.1)");
Console.WriteLine($"  GET/POST/PUT/DELETE http://0.0.0.0:{H2C_PORT}/echo/path - 返回请求详情 (JSON)");
Console.WriteLine($"  GET/POST/PUT/DELETE http://0.0.0.0:{HTTP1_PORT}/echo/path - 返回请求详情 (JSON)");
Console.WriteLine($"  GET http://0.0.0.0:{H2C_PORT}/health - 健康检查");
Console.WriteLine($"  GET http://0.0.0.0:{HTTP1_PORT}/health - 健康检查");

await app.RunAsync();
