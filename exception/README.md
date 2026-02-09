# 异常 vs 返回值性能测试

## 快速开始

### 编译和运行
```bash
cd exception
dotnet run -c Release
```

## 项目结构

- `Program.cs` - 包含两种方法的实现和基准测试
- `ExceptionBenchmark.csproj` - 项目配置文件
- `BENCHMARK_RESULTS.md` - 详细的性能测试结果报告
- `README.md` - 本文件

## 实现架构

### 异常方式 (Exception-Based)

```
Level1_Exception
    ├─ try-catch
    └─ Level2_Exception
        ├─ try-catch
        └─ Level3_Exception
            ├─ try-catch
            └─ Level4_Exception
                ├─ try-catch
                └─ Level5_Exception
                    └─ 如果 value < 0，throw ResultException
                       否则 return value+1
```

**特点**:
- 当发生错误时，异常会沿着调用栈向上传播
- 在最外层 Level1 中捕获异常
- 正常情况下性能很好 (2.917 ns)
- 错误情况下性能极差 (14,314.337 ns)

### 返回值方式 (Return-Based)

```
Level1_Return
    ├─ 检查是否有错误
    └─ Level2_Return
        ├─ 检查是否有错误
        └─ Level3_Return
            ├─ 检查是否有错误
            └─ Level4_Return
                ├─ 检查是否有错误
                └─ Level5_Return
                    └─ 返回 Result 对象
                       包含 Value 和 IsError
```

**特点**:
- 每层都检查错误状态
- 立即返回错误，无需栈展开
- 正常情况下性能较差 (16.514 ns) - 因为要创建对象
- 错误情况下性能优异 (7.846 ns)

## 关键性能数据

### 正常情况对比
| 方式 | 时间 | 内存分配 |
|------|------|--------|
| 异常 | 2.917 ns | 0 B 🚀 |
| 返回值 | 16.514 ns | 144 B |

### 错误情况对比 
| 方式 | 时间 | 内存分配 | 性能差异 |
|------|------|--------|---------|
| 异常 | 14,314.337 ns | 544 B | **⚠️ 极慢** |
| 返回值 | 7.846 ns | 48 B | **⚡ 极快** |

**错误情况下差异**: 异常方式比返回值方式慢 **1,823 倍** ！

## 核心结论

### 😱 警示
**永远不要使用异常作为控制流**，尤其是在错误频繁发生的场景中。

### 📊 数据说话
- 错误情况性能差 1,823 倍
- 内存分配多 11 倍
- 对 GC 压力大

### 🎯 建议

选择错误处理方式的决策树：

```
错误发生频率?
├─ 罕见 (< 0.1%)  → 使用异常 ✅
├─ 偶发 (0.1%-10%)  → 使用返回值
└─ 频繁 (> 10%)  → 一定要用返回值 ⚠️
```

## 测试环境信息

- **操作系统**: Windows 11
- **处理器**: Intel Core i9-14900HX (24 物理核心)
- **.NET 版本**: 6.0.36
- **JIT 编译器**: RyuJIT AVX2
- **测试框架**: BenchmarkDotNet v0.13.2
- **测试次数**: 5 次迭代，3 次预热

## 源代码说明

### 异常方式的关键代码

```csharp
// ResultException 用于"预期的"错误
public class ResultException : Exception
{
    public int Result { get; set; }
    public ResultException(int result) => Result = result;
}

public int Execute_Exception(int value)
{
    try
    {
        return Level1_Exception(value);
    }
    catch (ResultException ex)
    {
        return ex.Result;  // 从异常中提取值
    }
}
```

**问题**: 这是反模式！不应该用异常来返回业务数据。

### 返回值方式的关键代码

```csharp
public class Result
{
    public int Value { get; set; }
    public bool IsError { get; set; }
}

public int Execute_Return(int value)
{
    var input = new Result { Value = value, IsError = false };
    var result = Level1_Return(input);
    return result.Value;
}
```

**优点**: 清晰的错误处理语义，发生错误时仍然很快。

## 参考资源

- [微软 .NET 设计指南](https://learn.microsoft.com/zh-cn/dotnet/standard/design-guidelines/exceptions)
- [BenchmarkDotNet 文档](https://benchmarkdotnet.org/)
- [C# 进阶性能优化](https://channel9.msdn.com/)

## 许可证

本测试代码用于教育目的。
