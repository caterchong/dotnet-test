# JSON 性能比较：Newtonsoft.Json vs System.Text.Json

这个项目用于比较 Newtonsoft.Json 和 System.Text.Json 两个 JSON 序列化库的性能差异。

## 项目结构

- `Program.cs` - 程序入口，运行基准测试
- `Models.cs` - 测试数据模型（Person、Address）
- `JsonBenchmarks.cs` - BenchmarkDotNet 基准测试类

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

1. **序列化性能**（Serialize）
   - `SystemTextJson_Serialize` - 使用 System.Text.Json 序列化
   - `NewtonsoftJson_Serialize` - 使用 Newtonsoft.Json 序列化

2. **反序列化性能**（Deserialize）
   - `SystemTextJson_Deserialize` - 使用 System.Text.Json 反序列化
   - `NewtonsoftJson_Deserialize` - 使用 Newtonsoft.Json 反序列化

## 测试数据

- 生成 1000 个 Person 对象
- 每个对象包含嵌套的 Address 对象、列表和字典
- 使用固定随机种子确保测试可重复

## 预期结果

根据微软的官方基准测试，**System.Text.Json** 通常比 **Newtonsoft.Json** 快 2-3 倍，并且内存占用更少。这是因为：

- System.Text.Json 使用 Span<T> 和 Memory<T> 等现代 .NET API
- System.Text.Json 是 .NET 内置库，无需额外依赖
- System.Text.Json 针对性能进行了深度优化

## 输出

测试完成后，会在控制台显示性能对比结果，详细报告保存在 `BenchmarkDotNet.Artifacts` 目录中。

