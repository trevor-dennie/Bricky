# Test script for MCP server
Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║      MCP Server Test Script            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Starting MCP server..." -ForegroundColor Yellow
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project McpServer.csproj" -NoNewWindow -PassThru -RedirectStandardInput "server-stdin.txt" -RedirectStandardOutput "server-stdout.txt" -RedirectStandardError "server-stderr.txt"

Start-Sleep -Seconds 2
Write-Host "✅ Server started (PID: $($serverProcess.Id))" -ForegroundColor Green
Write-Host ""

try {
    Write-Host "Sending initialize request..." -ForegroundColor Yellow
    Get-Content test-init.json | Out-File -FilePath server-stdin.txt -Encoding UTF8 -Append
    Start-Sleep -Seconds 1
    
    Write-Host "Sending bt_documentation request: 'project schedule management'" -ForegroundColor Yellow
    Write-Host "(This may take 1-2 minutes first time...)" -ForegroundColor Gray
    Write-Host ""
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Get-Content test-btdoc.json | Out-File -FilePath server-stdin.txt -Encoding UTF8 -Append
    
    # Wait for response
    $timeout = 180 # 3 minutes
    $waited = 0
    while ($waited -lt $timeout) {
        Start-Sleep -Seconds 1
        $waited++
        
        if (Test-Path "server-stderr.txt") {
            $stderr = Get-Content "server-stderr.txt" -Tail 5
            if ($stderr) {
                foreach ($line in $stderr) {
                    if ($line -match "Fetching content|articles|Indexing|Loading cache") {
                        Write-Host $line -ForegroundColor Gray
                    }
                }
            }
        }
        
        if (Test-Path "server-stdout.txt") {
            $stdout = Get-Content "server-stdout.txt"
            $responses = $stdout | Where-Object { $_ -match '"result"' }
            if ($responses.Count -ge 2) {
                break
            }
        }
    }
    
    $stopwatch.Stop()
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ RESPONSE RECEIVED:" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    if (Test-Path "server-stdout.txt") {
        $stdout = Get-Content "server-stdout.txt"
        $btDocResponse = $stdout | Where-Object { $_ -match '"method".*"tools/call"' -or $_ -match '"id".*2' } | Select-Object -Last 1
        
        if ($btDocResponse) {
            $json = $btDocResponse | ConvertFrom-Json
            if ($json.result.content) {
                foreach ($item in $json.result.content) {
                    Write-Host $item.text
                    Write-Host ""
                }
            }
        }
    }
    
    Write-Host "⏱️  Request completed in $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Gray
    
} finally {
    Write-Host ""
    Write-Host "Stopping server..." -ForegroundColor Yellow
    Stop-Process -Id $serverProcess.Id -Force
    Write-Host "✅ Done" -ForegroundColor Green
    
    # Cleanup temp files
    Remove-Item -Path "server-*.txt" -ErrorAction SilentlyContinue
}
