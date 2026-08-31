# HTTP/2 协议协商测试

验证：dotnet HTTPS server 同时开启 HTTP/1.1 + HTTP/2 时，客户端设了
`DefaultRequestVersion = 2.0` / `DefaultVersionPolicy = RequestVersionOrLower`，
但自己 `new HttpRequestMessage(...)` 再 `SendAsync(request)`，实际走的是哪个协议。

## 结论

**走 HTTP/1.1，不是 HTTP/2。**

`HttpClient.DefaultRequestVersion` / `DefaultVersionPolicy` 只作用于 **HttpClient 自己创建
HttpRequestMessage 的那些便捷重载**（`GetAsync(url)` / `PostAsync(url, ...)` 等）——它在内部
new 请求对象时把这两个默认值赋进去。

一旦你自己 `new HttpRequestMessage(...)` 并交给 `SendAsync(request)`，请求对象已经带着它自己的
`Version`，client 上的默认值不会再回填：

```
new HttpRequestMessage(HttpMethod.Get, url)
  → Version = 1.1              // HttpRequestMessage 的构造默认值
  → VersionPolicy = RequestVersionOrLower
```

`Version = 1.1` + `RequestVersionOrLower` 的含义是"最高用 1.1，可以更低"，所以 TLS ALPN 只会
advertise `http/1.1`，压根不会协商到 h2。服务端支持 h2 也没用。

### 实测结果（dotnet 10.0.203, macOS, TLS 1.2, 服务端 `HttpProtocols.Http1AndHttp2`）

| 用例 | 发送前 req.Version | response.Version | 服务端 Request.Protocol |
|---|---|---|---|
| 1. `client.GetAsync(url)` | (由 client 创建) | 2.0 | HTTP/2 |
| 2. `new HttpRequestMessage` + `SendAsync`，不设 Version | 1.1 | **1.1** | **HTTP/1.1** |
| 3. 显式 `req.Version=2.0` + `OrLower` | 2.0 | 2.0 | HTTP/2 |
| 4. 显式 `req.Version=1.1` + `OrLower` | 1.1 | 1.1 | HTTP/1.1 |
| 5. 显式 `req.Version=2.0` + `RequestVersionExact` | 2.0 | 2.0 | HTTP/2 |

用例 1 与用例 2 的对比就是结论本身：同一个 HttpClient、同一组默认设置，仅仅因为请求对象由谁创建，
协议就从 h2 掉回 h1.1。

用例 5 排除了"服务端不支持 h2 所以降级"这一解释。

服务端返回的 `connectionId` 也印证了这点：用例 1/3/5 复用同一条 h2 连接（多路复用），
用例 2/4 复用另一条 h1.1 连接 —— .NET 的连接池按协议版本分开。

### 正确做法

自己构造请求时，把版本设在**请求对象**上：

```csharp
var req = new HttpRequestMessage(HttpMethod.Get, url)
{
    Version = HttpVersion.Version20,
    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
};
var resp = await client.SendAsync(req);
```

## 运行

```bash
# 终端 1：HTTPS server，8443，同端口 ALPN 协商 h1.1 / h2
dotnet run --project EchoServer

# 终端 2：跑对照实验
dotnet run --project Client
```

客户端用 `DangerousAcceptAnyServerCertificateValidator` 跳过 dev 证书链校验，
只关注协议协商；不想跳过就先 `dotnet dev-certs https --trust`。

## 结构

- `EchoServer/` — Kestrel HTTPS，`listenOptions.Protocols = HttpProtocols.Http1AndHttp2` + `UseHttps()`
  - `GET /echo` 返回服务端实际看到的 `Request.Protocol` / TLS 版本 / connectionId
  - `POST /echo` echo 请求体，响应头带 `X-Server-Protocol`
- `Client/` — 5 个对照用例，打印发送前 `req.Version`、`response.Version`、服务端自报协议
