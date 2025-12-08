# gRPC 性能比较：Grpc.Core vs grpc-dotnet

这个项目用于比较 Grpc.Core（传统实现）和 grpc-dotnet（现代实现）两个 gRPC 库的性能差异。

## 项目结构

- `Program.cs` - 程序入口，运行基准测试
- `Protos/greeter.proto` - gRPC 服务定义文件
- `GreeterService.cs` - gRPC 服务实现
- `GrpcBenchmarks.cs` - BenchmarkDotNet 基准测试类

## 背景说明

### Grpc.Core
- 传统的 gRPC C# 实现
- 基于原生 C++ gRPC 库
- 在 .NET 5+ 上已被标记为废弃
- 需要原生依赖

### grpc-dotnet
- 现代的纯 C# 实现
- 基于 .NET 的 HTTP/2 实现
- 完全托管，无需原生依赖
- 与 ASP.NET Core 深度集成
- 微软推荐的新实现

## 运行方式

### 1. 编译项目
```bash
dotnet build
```

### 2. 运行性能测试（Release 模式）
```bash
dotnet run -c Release
```

**注意**：BenchmarkDotNet 需要在 Release 模式下运行才能获得准确的性能数据。

## 测试内容

项目会测试以下场景：

1. **序列化性能**（Protobuf）
   - `Protobuf_Serialize` - 使用 Google.Protobuf 序列化
   - `Protobuf_Deserialize` - 使用 Google.Protobuf 反序列化
   - **注意**：两者都使用相同的 Google.Protobuf 库，性能应该相同

2. **RPC 调用性能**
   - `GrpcCore_RpcCall` - 使用 Grpc.Core 进行 RPC 调用
   - `GrpcDotnet_RpcCall` - 使用 grpc-dotnet 进行 RPC 调用
   - **这是真正的性能差异所在**

## 测试环境

- 每个测试都会启动独立的 gRPC 服务器
- Grpc.Core 服务器运行在端口 50051
- grpc-dotnet 服务器运行在端口 50052
- 使用本地回环地址（localhost）避免网络延迟影响

## 预期结果

根据微软的官方基准测试和社区反馈：

- **序列化/反序列化**：两者性能相同（都使用 Google.Protobuf）
- **RPC 调用**：grpc-dotnet 通常性能更好，特别是在高并发场景下
- **内存占用**：grpc-dotnet 通常更低（纯托管实现）
- **启动时间**：grpc-dotnet 更快（无需加载原生库）

## 重要说明

1. **Grpc.Core 已废弃**：微软已停止对 Grpc.Core 的积极开发，推荐迁移到 grpc-dotnet
2. **平台兼容性**：
   - Grpc.Core 需要原生库，在某些平台（如 macOS ARM64）可能不可用
   - 如果 Grpc.Core 不可用，相关测试会被自动跳过
   - grpc-dotnet 是纯托管实现，在所有平台上都可用
3. **实际项目**：新项目应该使用 grpc-dotnet

## 测试结果示例

在 macOS ARM64 上的测试结果：

| 测试项 | 性能 | 说明 |
|--------|------|------|
| Protobuf 序列化 | 323.2 ns | 基准 |
| Protobuf 反序列化 | 427.6 ns | 比序列化慢约32% |
| Grpc.Core RPC 调用 | N/A | 平台不支持 |
| grpc-dotnet RPC 调用 | 81.6 μs | 包含网络开销 |

**注意**：RPC 调用的时间包含了网络往返时间，实际性能取决于网络延迟和服务器处理时间。

## 输出

测试完成后，会在控制台显示性能对比结果，详细报告保存在 `BenchmarkDotNet.Artifacts` 目录中。
