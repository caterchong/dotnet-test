using System.Diagnostics;

namespace GrpcLoadBalancing.TestRunner;

class Program
{
    private static readonly List<Process> ServerProcesses = new();
    private static Process? ClientProcess;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== gRPC Load Balancing Test Runner ===\n");

        var serverPath = args.Length > 0 ? args[0] : "../GrpcServer/bin/Debug/net9.0/GrpcServer.dll";
        var clientPath = args.Length > 1 ? args[1] : "../GrpcClient/bin/Debug/net9.0/GrpcClient.dll";

        try
        {
            // 启动3个服务器实例
            Console.WriteLine("Starting 3 gRPC server instances...");
            StartServer(serverPath, 50051);
            StartServer(serverPath, 50052);
            StartServer(serverPath, 50053);
            
            await Task.Delay(2000); // 等待服务器启动

            // 测试场景1: 基本负载均衡测试
            Console.WriteLine("\n=== Test 1: Basic Load Balancing ===");
            await RunClientTest(clientPath, "http://localhost:50051,http://localhost:50052,http://localhost:50053", 30, 500);

            // 测试场景2: 添加新服务器实例
            Console.WriteLine("\n=== Test 2: Adding New Server Instance ===");
            Console.WriteLine("Starting 4th server on port 50054...");
            StartServer(serverPath, 50054);
            await Task.Delay(2000);
            await RunClientTest(clientPath, "http://localhost:50051,http://localhost:50052,http://localhost:50053,http://localhost:50054", 30, 500);

            // 测试场景3: 移除服务器实例
            Console.WriteLine("\n=== Test 3: Removing Server Instance ===");
            Console.WriteLine("Stopping server on port 50054...");
            StopServer(50054);
            await Task.Delay(2000);
            await RunClientTest(clientPath, "http://localhost:50051,http://localhost:50052,http://localhost:50053", 30, 500);

            // 测试场景4: 服务器宕机测试
            Console.WriteLine("\n=== Test 4: Server Failure Test ===");
            Console.WriteLine("Simulating server failure on port 50052...");
            StopServer(50052);
            await Task.Delay(2000);
            await RunClientTest(clientPath, "http://localhost:50051,http://localhost:50052,http://localhost:50053", 30, 500);

            // 测试场景5: 恢复服务器
            Console.WriteLine("\n=== Test 5: Server Recovery Test ===");
            Console.WriteLine("Restarting server on port 50052...");
            StartServer(serverPath, 50052);
            await Task.Delay(2000);
            await RunClientTest(clientPath, "http://localhost:50051,http://localhost:50052,http://localhost:50053", 30, 500);

            Console.WriteLine("\n=== All Tests Completed ===");
        }
        finally
        {
            // 清理所有进程
            Console.WriteLine("\nCleaning up...");
            StopAllServers();
            if (ClientProcess != null && !ClientProcess.HasExited)
            {
                ClientProcess.Kill();
            }
        }
    }

    static void StartServer(string serverPath, int port)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{serverPath}\" {port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server-{port}] {e.Data}");
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server-{port}-ERROR] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        ServerProcesses.Add(process);
        Console.WriteLine($"Server started on port {port} (PID: {process.Id})");
    }

    static void StopServer(int port)
    {
        var process = ServerProcesses.FirstOrDefault(p => 
            !p.HasExited && p.StartInfo.Arguments.Contains(port.ToString()));
        
        if (process != null)
        {
            process.Kill();
            process.WaitForExit(5000);
            ServerProcesses.Remove(process);
            Console.WriteLine($"Server on port {port} stopped");
        }
    }

    static void StopAllServers()
    {
        foreach (var process in ServerProcesses)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            process.Dispose();
        }
        ServerProcesses.Clear();
    }

    static async Task RunClientTest(string clientPath, string target, int duration, int interval)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{clientPath}\" \"{target}\" {duration} {interval}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            }
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Client] {e.Data}");
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Client-ERROR] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        ClientProcess = process;
        await process.WaitForExitAsync();
        ClientProcess = null;
    }
}
