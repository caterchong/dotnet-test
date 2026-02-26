# HTTP/2 Echo Server - 压测脚本 (PowerShell)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "HTTP/2 Echo Server 压测工具" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$serverUrl = "http://localhost:5000"

Write-Host "检查服务器连接..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$serverUrl/health" -TimeoutSec 5 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ 服务器连接成功" -ForegroundColor Green
    }
}
catch {
    Write-Host "✗ 无法连接到服务器" -ForegroundColor Red
    Write-Host "请确保 Echo 服务在运行: start-server.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "编译压测程序..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "运行压测..." -ForegroundColor Cyan
Write-Host ""

dotnet run --no-build -c Release --project Http2EchoServer.csproj -- benchmark
