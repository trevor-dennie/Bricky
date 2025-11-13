# Start Console Chat Interface
Write-Host "Starting BuilderTrend Console Chat..." -ForegroundColor Cyan
Write-Host ""
Set-Location $PSScriptRoot
dotnet run --project ChatConsole.csproj
