# Test semantic search by sending JSON-RPC requests to the MCP server
# This simulates what an MCP client would do

Write-Host "=== Testing Semantic Search ===" -ForegroundColor Cyan
Write-Host ""

# Test queries
$testQuery = "how to create an invoice"

Write-Host "Test Query: '$testQuery'" -ForegroundColor Yellow
Write-Host ""
Write-Host "Expected: First search will index articles (1-3 min), then return semantic results" -ForegroundColor Gray
Write-Host ""

# Create the JSON-RPC request
$request = @{
    jsonrpc = "2.0"
    id = 1
    method = "tools/call"
    params = @{
        name = "bt_documentation"
        arguments = @{
            query = $testQuery
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending request to MCP server..." -ForegroundColor Cyan
Write-Host ""

# Send request to the running server via stdin
$request | Out-File -FilePath "test_request.json" -Encoding UTF8

Write-Host "Request saved to test_request.json" -ForegroundColor Green
Write-Host ""
Write-Host "To test manually:" -ForegroundColor Yellow
Write-Host "1. Make sure MCP server is running (dotnet run --project McpServer.csproj)" -ForegroundColor White
Write-Host "2. Send the request:" -ForegroundColor White
Write-Host "   Get-Content test_request.json | dotnet run --project McpServer.csproj" -ForegroundColor Cyan
Write-Host ""
Write-Host "Watch for these log messages in stderr:" -ForegroundColor Yellow
Write-Host "  [Semantic] Indexing N articles..." -ForegroundColor Gray
Write-Host "  [Semantic] Indexed N articles with embeddings" -ForegroundColor Gray
Write-Host "  [BTDoc] Using semantic search..." -ForegroundColor Gray
Write-Host "  [BTDoc] Semantic search found N results" -ForegroundColor Gray
Write-Host ""
Write-Host "The response should include: '(using semantic search)'" -ForegroundColor Green
