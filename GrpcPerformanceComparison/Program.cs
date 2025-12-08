using BenchmarkDotNet.Running;
using GrpcPerformanceComparison;

Console.WriteLine("=== gRPC 性能比较：Grpc.Core vs grpc-dotnet ===\n");
Console.WriteLine("说明：");
Console.WriteLine("• Grpc.Core 需要原生 C++ 库，在 macOS ARM64 上可能不可用");
Console.WriteLine("• 如果 Grpc.Core 不可用，相关测试会被自动跳过");
Console.WriteLine("• 序列化/反序列化测试使用 Google.Protobuf，两者性能相同");
Console.WriteLine("• 实际的 RPC 调用性能差异主要体现在网络层和通道管理上");
Console.WriteLine();

// 运行 BenchmarkDotNet 基准测试
Console.WriteLine("正在运行性能基准测试...\n");
var summary = BenchmarkRunner.Run<GrpcBenchmarks>();

Console.WriteLine("\n=== 测试完成 ===");
Console.WriteLine("详细结果已保存在 BenchmarkDotNet.Artifacts 目录中");
