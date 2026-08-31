using Microsoft.AspNetCore.Mvc;

namespace EchoServer.Controllers;

[ApiController]
[Route("echo")]
public class EchoController : ControllerBase
{
    // 服务端视角：把它实际收到的协议原样报回去，
    // 用来交叉验证客户端 response.Version 不是自己猜的。
    [HttpGet]
    public IActionResult Protocol()
    {
        var tls = HttpContext.Features.Get<Microsoft.AspNetCore.Connections.Features.ITlsHandshakeFeature>();
        return Ok(new
        {
            protocol = Request.Protocol,
            isHttp2 = HttpProtocol.IsHttp2(Request.Protocol),
            scheme = Request.Scheme,
            tlsProtocol = tls?.Protocol.ToString(),
            connectionId = HttpContext.Connection.Id,
        });
    }

    [HttpPost]
    public async Task Echo(CancellationToken cancellationToken)
    {
        Response.ContentType = Request.ContentType ?? "application/octet-stream";
        Response.Headers["X-Server-Protocol"] = Request.Protocol;
        if (Request.ContentLength.GetValueOrDefault() > 0)
        {
            await Request.Body.CopyToAsync(Response.Body, cancellationToken);
        }
    }
}
