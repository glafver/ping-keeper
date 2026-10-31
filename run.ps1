# Ping Keeper — запуск локально (фоновый, переживает закрытие консоли).
# Стартует сервис на http://localhost:8080 в demo-режиме (данные в памяти).
$ErrorActionPreference = 'Stop'

$dotnet = (Get-Command dotnet).Source
$root   = $PSScriptRoot

# Уже запущен?
$running = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Ping Keeper уже запущен (pid $($running.OwningProcess)). http://localhost:8080/" -ForegroundColor Green
    exit 0
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS        = 'http://localhost:8080'

Start-Process -FilePath $dotnet `
  -ArgumentList 'bin\Release\net8.0\PingKeeper.dll' `
  -WorkingDirectory $root `
  -RedirectStandardOutput "$root\server.log" `
  -RedirectStandardError  "$root\server.err.log" `
  -WindowStyle Hidden

Start-Sleep -Seconds 4
$pid = (Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue).OwningProcess
if ($pid) {
    Write-Host "Ping Keeper запущен (pid $pid). http://localhost:8080/  (admin / demo)" -ForegroundColor Green
} else {
    Write-Host "Не удалось запустить. Смотри server.err.log" -ForegroundColor Red
}
