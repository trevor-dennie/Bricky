# Test Semantic Search

Write-Host "=== Semantic Search Test ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will test the semantic search feature by querying BuilderTrend documentation." -ForegroundColor White
Write-Host ""

# Check configuration
if (-not (Test-Path "appsettings.json")) {
    Write-Host "❌ appsettings.json not found!" -ForegroundColor Red
    exit 1
}

$config = Get-Content "appsettings.json" | ConvertFrom-Json
Write-Host "📋 LLM Configuration:" -ForegroundColor Cyan
Write-Host "  Provider: $($config.LLM.Provider)" -ForegroundColor White
Write-Host "  Model: $($config.LLM.Model)" -ForegroundColor White
Write-Host ""

Write-Host "Starting MCP server to test semantic search..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Test Query: 'how to create an invoice'" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected behavior:" -ForegroundColor White
Write-Host "  1. First search will index articles (may take 1-3 minutes)" -ForegroundColor Gray
Write-Host "  2. Look for '[Semantic] Indexing...' messages in output" -ForegroundColor Gray
Write-Host "  3. Results should show 'using semantic search'" -ForegroundColor Gray
Write-Host "  4. Subsequent searches will be fast (cached)" -ForegroundColor Gray
Write-Host ""

Write-Host "To test manually:" -ForegroundColor Yellow
Write-Host "  1. Start MCP server: dotnet run" -ForegroundColor Cyan
Write-Host "  2. Send JSON-RPC request with bt_documentation tool" -ForegroundColor Cyan
Write-Host "  3. Check stderr for semantic search logs" -ForegroundColor Cyan
Write-Host ""

Write-Host "Note: Semantic search requires LLM to be configured." -ForegroundColor Yellow
Write-Host "Using Ollama is recommended for unlimited indexing." -ForegroundColor Yellow
