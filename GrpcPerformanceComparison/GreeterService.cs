using Grpc.Core;

namespace GrpcPerformanceComparison;

// gRPC 服务实现
public class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = $"Hello {request.Name}",
            Id = request.Id,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public override async Task SayHelloStream(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        for (int i = 0; i < 10; i++)
        {
            await responseStream.WriteAsync(new HelloReply
            {
                Message = $"Hello {request.Name} - {i}",
                Id = request.Id + i,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
    }
}

