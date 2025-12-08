namespace JsonPerformanceComparison;

// 测试数据模型
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime BirthDate { get; set; }
    public Address Address { get; set; } = new();
    public List<string> Hobbies { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

