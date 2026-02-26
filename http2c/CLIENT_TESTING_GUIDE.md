# HTTP/2 Echo Server - 客户端测试工具

测试命令集合

## 前置要求
- curl 或 Invoke-WebRequest (PowerShell)
- Echo 服务运行在 http://localhost:5000

## 基本测试

### 1. 健康检查
curl -v http://localhost:5000/health

### 2. 简单 Echo (POST)
curl -X POST http://localhost:5000/echo -d "Hello Server"

### 3. 简单 Echo (GET 带路径)
curl http://localhost:5000/echo/test123

### 4. 发送 JSON 数据 (POST)
curl -X POST http://localhost:5000/echo \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello Echo Server","timestamp":"2026-02-10"}'

## 性能测试

### 1. 快速响应时间测试 (10 请求)
for i in {1..10}; do time curl -X POST http://localhost:5000/echo -d "Test $i"; done

### 2. 并发测试 (PowerShell)
$urls = @(); for ($i=0; $i -lt 50; $i++) { $urls += "http://localhost:5000/echo" }
$urls | ForEach-Object -Parallel { 
    Measure-Command { 
        Invoke-WebRequest -Uri $_ -Method Post -Body "Concurrent test" 
    } 
} -ThrottleLimit 50

### 3. 大数据测试 (发送 1 MB 数据)
$largeData = [string]::new('x', 1024*1024)
time curl -X POST http://localhost:5000/echo -d $largeData

### 4. 持续发送 100 个请求
for i in {1..100}; do curl -X POST http://localhost:5000/echo -d "Message $i" > /dev/null; done

## HTTP/2 特定测试

### 使用 nghttp2 工具 (需要安装)
# 发送单个请求
h2load -n 1 http://localhost:5000/echo

# 并发 50 连接
h2load -c 50 -m 1 http://localhost:5000/echo

# 持续发送
h2load -t 10 http://localhost:5000/echo

## Windows PowerShell 测试脚本

### 简单函数
function Test-Echo {
    param([string]$Message = "Hello Echo", [int]$Count = 1)
    
    for ($i = 0; $i -lt $Count; $i++) {
        Write-Host "Request #$($i+1)..." -ForegroundColor Cyan
        $response = Invoke-WebRequest -Uri "http://localhost:5000/echo" `
            -Method Post -Body $Message
        Write-Host "Response: $($response.Content)" -ForegroundColor Green
    }
}

### 并发测试函数
function Test-Concurrency {
    param([int]$Threads = 10, [int]$Requests = 10)
    
    $jobs = @()
    for ($i = 0; $i -lt $Threads; $i++) {
        $job = Start-Job -ScriptBlock {
            $message = "Thread $using:i - Request"
            for ($j = 0; $j -lt $using:Requests; $j++) {
                Invoke-WebRequest -Uri "http://localhost:5000/echo" `
                    -Method Post -Body "$message $j" | Out-Null
            }
        }
        $jobs += $job
    }
    
    Wait-Job -Job $jobs
    foreach ($job in $jobs) {
        Receive-Job -Job $job
    }
}

## 在线工具

### 使用 Apache Bench (ab)
ab -n 1000 -c 50 http://localhost:5000/echo

### 使用 wrk
wrk -t 4 -c 50 -d 10s http://localhost:5000/echo

## 高级测试场景

### 1. 渐进式负载测试 (1, 10, 50, 100 并发)
for concurrency in 1 10 50 100; do
    echo "Testing with $concurrency concurrent connections..."
    ab -n 1000 -c $concurrency http://localhost:5000/echo
    sleep 2
done

### 2. 持续压力测试 (30 秒)
time wrk -t 4 -c 100 -d 30s --latency http://localhost:5000/echo

### 3. 错误恢复测试 (间歇性请求)
for i in {1..100}; do
    curl -X POST http://localhost:5000/echo -d "Test $i" &
    sleep 0.01
done
wait

## 监控工具集成

### 实时监控系统资源
# Linux
watch -n 1 'ps aux | grep dotnet'

# Windows (PowerShell)
while($true) { Clear-Host; Get-Process | Where-Object {$_.Name -like "*dotnet*"} | Format-Table; Start-Sleep -Seconds 1 }

## 故障排查命令

### 检查端口是否监听
netstat -ano | findstr :5000

### 查看网络连接统计
netstat -s

### 实时监控网络 I/O
# Linux
iftop -i lo

# Windows
netsh int tcp show statsabridged

---

**提示**: 
- 运行所有测试前，确保 Echo 服务已启动
- 使用相同的条件重复测试以获得准确的性能指标
- 在高负载测试前，先用小规模测试验证基本功能
