using BenchmarkDotNet.Running;
using JsonApiCmp;

Console.WriteLine("=== JSON API 性能比较：System.Text.Json vs Newtonsoft.Json ===\n");
Console.WriteLine("本项目展示了如何同时支持两种 JSON 库的 JsonProperty 特性\n");

// 演示同时支持两种库的特性
Console.WriteLine("示例：在同一个属性上同时使用两种特性：");
Console.WriteLine("  [JsonPropertyName(\"fullName\")]  // System.Text.Json");
Console.WriteLine("  [JsonProperty(\"fullName\")]      // Newtonsoft.Json");
Console.WriteLine("  public string Name { get; set; }\n");

// 运行 BenchmarkDotNet 基准测试
Console.WriteLine("正在运行性能基准测试...\n");
var summary = BenchmarkRunner.Run<JsonApiBenchmarks>();

Console.WriteLine("\n=== 测试完成 ===");
Console.WriteLine("详细结果已保存在 BenchmarkDotNet.Artifacts 目录中");
