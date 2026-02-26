using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const int Port = 8080;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        // 关闭访问日志
        logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
        });

        webBuilder.ConfigureServices(services =>
        {
            services.AddRouting();
        });

        webBuilder.Configure(app =>
        {
            var routeBuilder = new RouteBuilder(app);

            // Echo：原样返回客户端上行数据（请求体），支持 GET/POST/PUT/PATCH/DELETE
            routeBuilder.MapVerb("GET", "/echo", async context =>
            {
                context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
                if (context.Request.ContentLength.GetValueOrDefault() > 0)
                    await context.Request.Body.CopyToAsync(context.Response.Body);
            });
            routeBuilder.MapVerb("POST", "/echo", async context =>
            {
                context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
                if (context.Request.ContentLength.GetValueOrDefault() > 0)
                    await context.Request.Body.CopyToAsync(context.Response.Body);
            });
            routeBuilder.MapVerb("PUT", "/echo", async context =>
            {
                context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
                if (context.Request.ContentLength.GetValueOrDefault() > 0)
                    await context.Request.Body.CopyToAsync(context.Response.Body);
            });
            routeBuilder.MapVerb("PATCH", "/echo", async context =>
            {
                context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
                if (context.Request.ContentLength.GetValueOrDefault() > 0)
                    await context.Request.Body.CopyToAsync(context.Response.Body);
            });
            routeBuilder.MapVerb("DELETE", "/echo", async context =>
            {
                context.Response.ContentType = context.Request.ContentType ?? "application/octet-stream";
                if (context.Request.ContentLength.GetValueOrDefault() > 0)
                    await context.Request.Body.CopyToAsync(context.Response.Body);
            });

            routeBuilder.MapGet("/health", async context =>
            {
                context.Response.ContentType = "application/json";
                var json = System.Text.Json.JsonSerializer.Serialize(new { status = "healthy", timestamp = DateTime.UtcNow });
                await context.Response.WriteAsync(json);
            });

            var routes = routeBuilder.Build();
            app.UseRouter(routes);
        });
    })
    .Build();

Console.WriteLine($"HTTP/1.1 Echo Server: http://0.0.0.0:{Port}");
Console.WriteLine("  /echo  - 回显请求体 (GET/POST/PUT/PATCH/DELETE)");
Console.WriteLine("  /health - 健康检查");

await host.RunAsync();
