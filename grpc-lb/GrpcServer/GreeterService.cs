using Grpc.Core;
using GrpcLoadBalancing.Shared;

namespace GrpcLoadBalancing.Server;

public class GreeterService : Greeter.GreeterBase
{
    private readonly int _serverPort;
    private readonly string _serverHost;

    public GreeterService(int port, string hostname)
    {
        _serverPort = port;
        _serverHost = hostname;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var reply = new HelloReply
        {
            Message = $"Hello {request.Name} from server {_serverHost}:{_serverPort}",
            Id = request.Id,
            ServerPort = _serverPort,
            ServerHost = _serverHost,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server {_serverPort} handled request: {request.Name} (ID: {request.Id})");
        
        return Task.FromResult(reply);
    }

    public override Task<HealthCheckReply> HealthCheck(HealthCheckRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckReply
        {
            Healthy = true,
            ServerPort = _serverPort
        });
    }
}
