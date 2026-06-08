using MySqlConnector;

namespace MySqlConnectorCmp.Services;

/// <summary>
/// 模式1: 每个请求持有一个 MySqlConnection，3 次 DB 访问复用同一连接，请求结束后释放
/// </summary>
public class SingleConnectionService
{
    private readonly string _connectionString;

    public SingleConnectionService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<string> ExecuteAsync(CancellationToken ct = default)
    {
        var results = new List<string>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        for (int i = 0; i < 3; i++)
        {
            await using var cmd = new MySqlCommand("SELECT 1", connection);
            var result = await cmd.ExecuteScalarAsync(ct);
            results.Add(result?.ToString() ?? "null");

            // 每次 DB 访问后做 idle 操作：计算斐波那契数
            Fibonacci(30);
        }

        return $"SingleConnection: [{string.Join(", ", results)}]";
    }

    /// <summary>
    /// 递归计算斐波那契数，模拟 idle 操作
    /// </summary>
    private static long Fibonacci(int n)
    {
        if (n <= 1) return n;
        return Fibonacci(n - 1) + Fibonacci(n - 2);
    }
}
