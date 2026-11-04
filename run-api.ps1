# Script tiện ích khởi chạy eShop.API với môi trường .NET SDK cục bộ
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host " Dang khoi chay eShop Web API..." -ForegroundColor Green
Write-Host " API Documentation: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "===================================================" -ForegroundColor Cyan

dotnet run --project src/eShop.API --urls "http://localhost:5000"

