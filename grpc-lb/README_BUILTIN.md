# 使用 gRPC .NET 内置负载均衡功能

根据 [Microsoft 官方文档](https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-10.0)，gRPC .NET 从 **2.45.0 版本**开始支持客户端负载均衡。

## 内置功能

### Resolver（解析器）

1. **DnsResolverFactory** - DNS 解析器
   - 通过查询 DNS A 记录获取地址列表
   - 支持定期刷新（refreshInterval）
   - 适用于 Kubernetes headless service

2. **StaticResolverFactory** - 静态解析器
   - 使用应用指定的静态地址列表
   - 适用于已知地址的场景

### Load Balancer（负载均衡器）

1. **PickFirstLoadBalancerFactory** - pick_first
   - 尝试连接地址直到成功
   - 所有调用都发送到第一个成功连接

2. **RoundRobinLoadBalancerFactory** - round_robin
   - 尝试连接所有地址
   - 使用轮询算法在成功连接间分配调用

## 使用示例

### 方式1: 使用 DNS Resolver + Round-Robin

```csharp
var services = new ServiceCollection();

// 注册 DNS Resolver（每30秒刷新）
services.AddSingleton<ResolverFactory>(
    sp => new DnsResolverFactory(refreshInterval: TimeSpan.FromSeconds(30)));

var serviceProvider = services.BuildServiceProvider();

// 配置 round_robin 负载均衡
var serviceConfig = new ServiceConfig
{
    LoadBalancingConfigs = { new LoadBalancingConfig("round_robin") }
};

var channel = GrpcChannel.ForAddress(
    "dns:///my-example-host:50051",
    new GrpcChannelOptions
    {
        Credentials = ChannelCredentials.Insecure,
        ServiceConfig = serviceConfig,
        ServiceProvider = serviceProvider
    });
```

### 方式2: 使用 Static Resolver + Round-Robin

```csharp
var services = new ServiceCollection();

// 注册 Static Resolver
var staticAddresses = new[]
{
    new BalancerAddress("localhost", 50051),
    new BalancerAddress("localhost", 50052),
    new BalancerAddress("localhost", 50053)
};

services.AddSingleton<ResolverFactory>(
    sp => new StaticResolverFactory(addr => staticAddresses));

var serviceProvider = services.BuildServiceProvider();

// 配置 round_robin 负载均衡
var serviceConfig = new ServiceConfig
{
    LoadBalancingConfigs = { new LoadBalancingConfig("round_robin") }
};

var channel = GrpcChannel.ForAddress(
    "static:///my-example-host",
    new GrpcChannelOptions
    {
        Credentials = ChannelCredentials.Insecure,
        ServiceConfig = serviceConfig,
        ServiceProvider = serviceProvider
    });
```

## 完整示例代码

查看 `GrpcClient/ProgramBuiltInV2_Example.cs` 获取完整实现示例。

## 与自定义实现的对比

### 使用内置功能的优势：
- ✅ 官方支持，经过充分测试
- ✅ 代码更简洁
- ✅ 自动处理连接管理和故障恢复
- ✅ 支持 DNS 自动刷新

### 自定义实现的优势：
- ✅ 完全控制负载均衡逻辑
- ✅ 可以添加自定义健康检查
- ✅ 可以添加自定义指标和监控
- ✅ 可以自定义故障处理策略

## 建议

对于大多数场景，**推荐使用 gRPC .NET 内置的负载均衡功能**。只有在需要特殊功能（如自定义健康检查、自定义指标等）时，才考虑自定义实现。

## 参考文档

- [gRPC client-side load balancing - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-10.0)
- 要求：.NET 5+ 和 Grpc.Net.Client 2.45.0+
