using BenchmarkDotNet.Running;
using JsonPerformanceComparison;

Console.WriteLine("=== JSON 性能比较：Newtonsoft.Json vs System.Text.Json ===\n");

// 运行 BenchmarkDotNet 基准测试
Console.WriteLine("正在运行性能基准测试...\n");
var summary = BenchmarkRunner.Run<JsonBenchmarks>();

Console.WriteLine("\n=== 测试完成 ===");
Console.WriteLine("详细结果已保存在 BenchmarkDotNet.Artifacts 目录中");
