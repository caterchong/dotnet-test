using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 关闭请求访问日志
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);

// 仅 HTTP/1.1，Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

var app = builder.Build();

// Echo：原样返回客户端上行数据（请求体），支持 GET/POST/PUT/PATCH/DELETE
app.MapMethods("/echo", new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }, async (HttpContext context) =>
{
    context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
    if (context.Request.ContentLength.GetValueOrDefault() > 0)
        await context.Request.Body.CopyToAsync(context.Response.Body);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

const int Port = 8080;
Console.WriteLine($"HTTP/1.1 Echo Server: http://0.0.0.0:{Port}");
Console.WriteLine("  POST /echo  - 回显请求体");
Console.WriteLine("  GET  /health - 健康检查");

await app.RunAsync();
