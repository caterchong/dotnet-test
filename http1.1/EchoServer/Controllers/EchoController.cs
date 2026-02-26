using Microsoft.AspNetCore.Mvc;

namespace EchoServer.Controllers;

[ApiController]
[Route("echo")]
public class EchoController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    public async Task Echo(CancellationToken cancellationToken)
    {
        Response.ContentType = Request.ContentType ?? "application/octet-stream";
        if (Request.ContentLength.GetValueOrDefault() > 0)
        {
            await Request.Body.CopyToAsync(Response.Body, cancellationToken);
        }
    }
}
