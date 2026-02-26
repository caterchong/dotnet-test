# HTTP/2 Echo Server - 启动脚本 (PowerShell)

Write-Host "================================" -ForegroundColor Green
Write-Host "HTTP/2 Echo Server" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "启动 Echo 服务..." -ForegroundColor Yellow
Write-Host "服务地址: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""

Write-Host "提示: 按 Ctrl+C 停止服务" -ForegroundColor Gray

dotnet run -- --urls "http://0.0.0.0:5000"
