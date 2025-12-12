# JsonApiCmp

JSON API 性能比较项目：System.Text.Json vs Newtonsoft.Json

## 项目说明

本项目比较了 `System.Text.Json` 和 `Newtonsoft.Json` 两个库在对象序列化和反序列化方面的性能差异，并展示了如何同时支持两种库的 JsonProperty 特性。

## 同时支持两种库的 JsonProperty

在 C# 中，可以在同一个属性上同时使用两种库的特性：

```csharp
using System.Text.Json.Serialization;
using Newtonsoft.Json;

public class Person
{
    // 同时使用两种库的特性来指定 JSON 属性名
    [JsonPropertyName("fullName")]  // System.Text.Json
    [JsonProperty("fullName")]      // Newtonsoft.Json
    public string Name { get; set; }
}
```

### 特性说明

- **System.Text.Json**: 使用 `JsonPropertyName` 特性（位于 `System.Text.Json.Serialization` 命名空间）
- **Newtonsoft.Json**: 使用 `JsonProperty` 特性（位于 `Newtonsoft.Json` 命名空间）

### 优势

1. **兼容性**: 同一个模型类可以在两种库之间无缝切换
2. **灵活性**: 可以根据项目需求选择不同的 JSON 库
3. **迁移友好**: 在从 Newtonsoft.Json 迁移到 System.Text.Json 时，可以逐步迁移

## 运行基准测试

```bash
dotnet run -c Release
```

## 基准测试内容

- 列表序列化（List<Person>）
- 列表反序列化（List<Person>）
- 单个对象序列化（Person）
- 单个对象反序列化（Person）

## 依赖项

- BenchmarkDotNet 0.15.8
- Newtonsoft.Json 13.0.4
- System.Text.Json (内置在 .NET 9.0)

## 注意事项

1. 两种库的特性可以同时存在，不会冲突
2. 确保两个特性的属性名保持一致，以保证序列化/反序列化的一致性
3. System.Text.Json 是 .NET 内置库，性能通常更好
4. Newtonsoft.Json 功能更丰富，但性能相对较低
