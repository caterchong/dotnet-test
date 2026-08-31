using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoServer;

public class Program
{
    // HTTPS 端口，同一个端口上通过 TLS ALPN 协商 h2 / http/1.1
    public const int HttpsPort = 8443;

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
                logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
                logging.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogLevel.Warning);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(opt =>
                {
                    opt.Limits.MinRequestBodyDataRate = null;
                    opt.ListenAnyIP(HttpsPort, listenOptions =>
                    {
                        // 关键：同一监听端点同时支持 HTTP/1.1 和 HTTP/2，
                        // 由 TLS ALPN 决定最终用哪个。
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps();
                    });
                });
                webBuilder.UseStartup<Startup>();
            });
}
