using System.Collections.Concurrent;
using System.Net;
using Grpc.Net.Client;
using GrpcLoadBalancing.Shared;

namespace GrpcLoadBalancing.Client;

/// <summary>
/// 简单的负载均衡器实现
/// </summary>
public class LoadBalancer : IDisposable
{
    private readonly List<ServerEndpoint> _endpoints = new();
    private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new();
    private readonly Random _random = new();
    private readonly Timer? _healthCheckTimer;
    private readonly Timer? _dnsRefreshTimer;
    private readonly string? _dnsAddress;
    private readonly int _dnsPort;
    private int _currentIndex = 0;
    private readonly object _lock = new();

    public LoadBalancer(string target)
    {
        if (target.StartsWith("dns://"))
        {
            var dnsTarget = target.Replace("dns://", "");
            var parts = dnsTarget.Split(':');
            _dnsAddress = parts[0];
            _dnsPort = parts.Length > 1 ? int.Parse(parts[1]) : 50051;
            
            // 启动 DNS 刷新定时器
            _dnsRefreshTimer = new Timer(RefreshDnsEndpoints, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        else
        {
            // 解析多个地址
            var addresses = target.Split(',');
            foreach (var addr in addresses)
            {
                var trimmed = addr.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    _endpoints.Add(new ServerEndpoint
                    {
                        Address = trimmed.StartsWith("http") ? trimmed : $"http://{trimmed}",
                        Healthy = true,
                        LastHealthCheck = DateTime.Now
                    });
                }
            }
        }

        // 启动健康检查定时器
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    private async void RefreshDnsEndpoints(object? state)
    {
        if (_dnsAddress == null) return;
        
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(_dnsAddress);
            var newEndpoints = addresses.Select(a => new ServerEndpoint
            {
                Address = $"http://{a}:{_dnsPort}",
                Healthy = true,
                LastHealthCheck = DateTime.Now
            }).ToList();

            lock (_lock)
            {
                // 添加新发现的端点
                foreach (var endpoint in newEndpoints)
                {
                    if (!_endpoints.Any(e => e.Address == endpoint.Address))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DNS: New endpoint discovered: {endpoint.Address}");
                        _endpoints.Add(endpoint);
                    }
                }

                // 移除不再存在的端点（保留一段时间以处理 DNS 延迟）
                var toRemove = _endpoints
                    .Where(e => !newEndpoints.Any(ne => ne.Address == e.Address) && 
                                DateTime.Now - e.LastHealthCheck > TimeSpan.FromMinutes(2))
                    .ToList();

                foreach (var endpoint in toRemove)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DNS: Endpoint removed: {endpoint.Address}");
                    _endpoints.Remove(endpoint);
                    if (_channels.TryRemove(endpoint.Address, out var channel))
                    {
                        channel.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DNS refresh error: {ex.Message}");
        }
    }

    private async void PerformHealthChecks(object? state)
    {
        var endpointsToCheck = new List<ServerEndpoint>();
        lock (_lock)
        {
            endpointsToCheck.AddRange(_endpoints);
        }

        foreach (var endpoint in endpointsToCheck)
        {
            try
            {
                var channel = GetOrCreateChannel(endpoint.Address);
                var client = new Greeter.GreeterClient(channel);
                
                var healthCheck = new HealthCheckRequest();
                var reply = await client.HealthCheckAsync(healthCheck, deadline: DateTime.UtcNow.AddSeconds(2));
                
                lock (_lock)
                {
                    endpoint.Healthy = reply.Healthy;
                    endpoint.LastHealthCheck = DateTime.Now;
                }
            }
            catch
            {
                lock (_lock)
                {
                    endpoint.Healthy = false;
                    endpoint.LastHealthCheck = DateTime.Now;
                }
            }
        }
    }

    public Greeter.GreeterClient GetClient()
    {
        ServerEndpoint? endpoint = null;
        lock (_lock)
        {
            var healthyEndpoints = _endpoints.Where(e => e.Healthy).ToList();
            if (healthyEndpoints.Count == 0)
            {
                // 如果没有健康的端点，使用所有端点
                healthyEndpoints = _endpoints.ToList();
            }

            if (healthyEndpoints.Count == 0)
            {
                throw new InvalidOperationException("No endpoints available");
            }

            // Round-robin 负载均衡
            endpoint = healthyEndpoints[_currentIndex % healthyEndpoints.Count];
            _currentIndex++;
        }

        var channel = GetOrCreateChannel(endpoint.Address);
        return new Greeter.GreeterClient(channel);
    }

    private GrpcChannel GetOrCreateChannel(string address)
    {
        return _channels.GetOrAdd(address, addr =>
        {
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            return GrpcChannel.ForAddress(addr, new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = 4 * 1024 * 1024,
                MaxSendMessageSize = 4 * 1024 * 1024,
            });
        });
    }

    public List<ServerEndpoint> GetEndpoints()
    {
        lock (_lock)
        {
            return _endpoints.ToList();
        }
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _dnsRefreshTimer?.Dispose();
        
        foreach (var channel in _channels.Values)
        {
            channel.Dispose();
        }
        _channels.Clear();
    }
}

public class ServerEndpoint
{
    public string Address { get; set; } = string.Empty;
    public bool Healthy { get; set; } = true;
    public DateTime LastHealthCheck { get; set; }
}
