using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace JsonApiCmp;

// 测试数据模型 - 展示如何同时支持 System.Text.Json 和 Newtonsoft.Json
public class Person
{
    // 同时使用两种库的特性来指定 JSON 属性名
    // System.Text.Json 使用 JsonPropertyName
    // Newtonsoft.Json 使用 JsonProperty
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonPropertyName("fullName")]
    [JsonProperty("fullName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    [JsonProperty("emailAddress")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    [JsonProperty("age")]
    public int Age { get; set; }

    [JsonPropertyName("birthDate")]
    [JsonProperty("birthDate")]
    public DateTime BirthDate { get; set; }

    [JsonPropertyName("address")]
    [JsonProperty("address")]
    public Address Address { get; set; } = new();

    [JsonPropertyName("hobbies")]
    [JsonProperty("hobbies")]
    public List<string> Hobbies { get; set; } = new();

    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class Address
{
    [JsonPropertyName("street")]
    [JsonProperty("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    [JsonProperty("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    [JsonProperty("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("zipCode")]
    [JsonProperty("zipCode")]
    public string ZipCode { get; set; } = string.Empty;
}
