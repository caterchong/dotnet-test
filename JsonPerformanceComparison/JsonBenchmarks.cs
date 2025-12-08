using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonPerformanceComparison;

[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net90)]
public class JsonBenchmarks
{
    private readonly List<Person> _testData;
    private readonly string _jsonString;
    private readonly JsonSerializerOptions _systemTextJsonOptions;
    private readonly JsonSerializerSettings _newtonsoftSettings;

    public JsonBenchmarks()
    {
        // 生成测试数据
        _testData = GenerateTestData(20);
        
        // 预序列化一次以生成 JSON 字符串
        _jsonString = System.Text.Json.JsonSerializer.Serialize(_testData);
        
        // System.Text.Json 配置
        _systemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        // Newtonsoft.Json 配置
        _newtonsoftSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None
        };
    }

    private List<Person> GenerateTestData(int count)
    {
        var random = new Random(42); // 固定种子以确保可重复性
        var data = new List<Person>();
        
        for (int i = 0; i < count; i++)
        {
            data.Add(new Person
            {
                Id = i,
                Name = $"Person {i}",
                Email = $"person{i}@example.com",
                Age = random.Next(18, 80),
                BirthDate = DateTime.Now.AddYears(-random.Next(18, 80)),
                Address = new Address
                {
                    Street = $"{random.Next(1, 999)} Main St",
                    City = $"City {i % 50}",
                    Country = $"Country {i % 10}",
                    ZipCode = $"{random.Next(10000, 99999)}"
                },
                Hobbies = new List<string> { "Reading", "Swimming", "Coding", "Gaming" },
                Metadata = new Dictionary<string, object>
                {
                    { "Score", random.Next(0, 100) },
                    { "Active", i % 2 == 0 },
                    { "Level", random.Next(1, 10) }
                }
            });
        }
        
        return data;
    }

    [Benchmark(Baseline = true)]
    public string SystemTextJson_Serialize()
    {
        return System.Text.Json.JsonSerializer.Serialize(_testData, _systemTextJsonOptions);
    }

    [Benchmark]
    public string NewtonsoftJson_Serialize()
    {
        return JsonConvert.SerializeObject(_testData, _newtonsoftSettings);
    }

    [Benchmark]
    public List<Person> SystemTextJson_Deserialize()
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<Person>>(_jsonString, _systemTextJsonOptions)!;
    }

    [Benchmark]
    public List<Person> NewtonsoftJson_Deserialize()
    {
        return JsonConvert.DeserializeObject<List<Person>>(_jsonString, _newtonsoftSettings)!;
    }
}

