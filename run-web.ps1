# Script tiện ích khởi chạy eShop.Web (Blazor Interactive Server)
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host " Dang khoi chay eShop Blazor Web App..." -ForegroundColor Green
Write-Host " Web Portal: http://localhost:5256" -ForegroundColor Yellow
Write-Host " Luu y: Dam bao eShop.API dang chay tai http://localhost:5000" -ForegroundColor DarkGray
Write-Host "===================================================" -ForegroundColor Cyan

dotnet run --project src/eShop.Web --urls "http://localhost:5256"

