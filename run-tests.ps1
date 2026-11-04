# Script chay kiem thu tu dong xUnit
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"

dotnet test

