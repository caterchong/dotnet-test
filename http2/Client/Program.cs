using System.Net;
using System.Text.Json;

string baseUrl = args.FirstOrDefault(a => a.StartsWith("http", StringComparison.OrdinalIgnoreCase))?.TrimEnd('/')
                 ?? "https://localhost:8443";
string url = baseUrl + "/echo";

// 服务端用的是 dotnet dev-certs 自签证书，这里跳过链校验，只关注协议协商。
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
};

using var client = new HttpClient(handler);

// ===== 需求里给定的两行设置 =====
client.DefaultRequestVersion = HttpVersion.Version20;
client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
// ================================

Console.WriteLine($"target                    : {url}");
Console.WriteLine($"client.DefaultRequestVersion : {client.DefaultRequestVersion}");
Console.WriteLine($"client.DefaultVersionPolicy  : {client.DefaultVersionPolicy}");
Console.WriteLine();

var results = new List<Row>();

// 用例 1：便捷方法，完全不碰 HttpRequestMessage
results.Add(await RunAsync("1. client.GetAsync(url)", null));

// 用例 2：需求的核心 —— 自己 new HttpRequestMessage，不设置 Version/VersionPolicy
results.Add(await RunAsync("2. new HttpRequestMessage + SendAsync (不设 Version)", req => { }));

// 用例 3：自己构造，并显式把两个属性设成和 client 默认值一样
results.Add(await RunAsync("3. 显式 req.Version=2.0 + OrLower", req =>
{
    req.Version = HttpVersion.Version20;
    req.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}));

// 用例 4：显式降到 1.1，证明请求上的设置优先于 client 默认值
results.Add(await RunAsync("4. 显式 req.Version=1.1 + OrLower", req =>
{
    req.Version = HttpVersion.Version11;
    req.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
}));

// 用例 5：强制 2.0，证明服务端确实支持 h2（排除“服务端不支持所以降级”的可能）
results.Add(await RunAsync("5. 显式 req.Version=2.0 + RequestVersionExact", req =>
{
    req.Version = HttpVersion.Version20;
    req.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
}));

Console.WriteLine();
Console.WriteLine("==================================== 结果汇总 ====================================");
Console.WriteLine($"{"用例",-52} {"发送前req",-9} {"响应",-6} {"服务端看到",-10}");
Console.WriteLine(new string('-', 82));
foreach (var r in results)
{
    Console.WriteLine($"{r.Name,-52} {r.RequestVersionBefore,-9} {r.ResponseVersion,-6} {r.ServerProtocol,-10}");
}
Console.WriteLine(new string('-', 82));

async Task<Row> RunAsync(string name, Action<HttpRequestMessage>? configure)
{
    Console.WriteLine($"--- {name} ---");
    try
    {
        HttpResponseMessage resp;
        string before;

        if (configure is null)
        {
            before = "(n/a)";
            resp = await client.GetAsync(url);
        }
        else
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            // HttpRequestMessage 构造完成后的原始默认值
            Console.WriteLine($"    new HttpRequestMessage 默认 Version={req.Version}, VersionPolicy={req.VersionPolicy}");
            configure(req);
            before = req.Version.ToString();
            Console.WriteLine($"    SendAsync 前          Version={req.Version}, VersionPolicy={req.VersionPolicy}");
            resp = await client.SendAsync(req);
        }

        using (resp)
        {
            string body = await resp.Content.ReadAsStringAsync();
            string serverProto = TryReadServerProtocol(body);
            Console.WriteLine($"    响应 HTTP 版本        response.Version={resp.Version}");
            Console.WriteLine($"    服务端自报            {serverProto}");
            Console.WriteLine($"    body                  {body}");
            Console.WriteLine();
            return new Row(name, before, resp.Version.ToString(), serverProto);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    失败: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine();
        return new Row(name, "-", "ERROR", ex.GetType().Name);
    }
}

static string TryReadServerProtocol(string body)
{
    try
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("protocol").GetString() ?? "?";
    }
    catch
    {
        return "?";
    }
}

record Row(string Name, string RequestVersionBefore, string ResponseVersion, string ServerProtocol);
