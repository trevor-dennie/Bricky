# LLM Integration Test Script
# This script tests the LLM service locally before running the full MCP server

Write-Host "=== LLM Service Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if appsettings.json exists
if (-not (Test-Path "appsettings.json")) {
    Write-Host "❌ appsettings.json not found!" -ForegroundColor Red
    Write-Host "Creating default configuration..." -ForegroundColor Yellow
    
    $defaultConfig = @{
        LLM = @{
            Provider = "Ollama"
            ApiKey = ""
            Model = ""
        }
        Instructions = @{
            Ollama = "Install Ollama from https://ollama.com, then run: ollama pull llama3.2"
            OpenRouter = "Get free API key from https://openrouter.ai/keys. Free model: meta-llama/llama-3.2-3b-instruct:free"
            Groq = "Get free API key from https://console.groq.com. Free models: llama-3.1-8b-instant, mixtral-8x7b-32768"
        }
    }
    
    $defaultConfig | ConvertTo-Json -Depth 3 | Set-Content "appsettings.json"
    Write-Host "✅ Created appsettings.json with default Ollama configuration" -ForegroundColor Green
}

# Read configuration
$config = Get-Content "appsettings.json" | ConvertFrom-Json
$provider = $config.LLM.Provider

Write-Host "📋 Current Configuration:" -ForegroundColor Cyan
Write-Host "  Provider: $provider" -ForegroundColor White
Write-Host "  Model: $($config.LLM.Model)" -ForegroundColor White

# Test based on provider
switch ($provider) {
    "Ollama" {
        Write-Host ""
        Write-Host "Testing Ollama connection..." -ForegroundColor Yellow
        
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5 -ErrorAction Stop
            $models = ($response.Content | ConvertFrom-Json).models
            
            if ($models) {
                Write-Host "✅ Ollama is running!" -ForegroundColor Green
                Write-Host "Available models:" -ForegroundColor Cyan
                foreach ($model in $models) {
                    Write-Host "  - $($model.name)" -ForegroundColor White
                }
                
                # Check if configured model exists
                $configuredModel = $config.LLM.Model
                if ([string]::IsNullOrEmpty($configuredModel)) {
                    $configuredModel = "llama3.2"
                }
                
                $modelExists = $models | Where-Object { $_.name -like "$configuredModel*" }
                if ($modelExists) {
                    Write-Host ""
                    Write-Host "✅ Model '$configuredModel' is available!" -ForegroundColor Green
                } else {
                    Write-Host ""
                    Write-Host "⚠️  Model '$configuredModel' not found" -ForegroundColor Yellow
                    Write-Host "Run: ollama pull $configuredModel" -ForegroundColor Cyan
                }
            }
        }
        catch {
            Write-Host "❌ Cannot connect to Ollama" -ForegroundColor Red
            Write-Host "Make sure Ollama is installed and running" -ForegroundColor Yellow
            Write-Host "Download from: https://ollama.com" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "After installing, run:" -ForegroundColor Yellow
            Write-Host "  ollama pull llama3.2" -ForegroundColor Cyan
        }
    }
    
    "OpenRouter" {
        Write-Host ""
        if ([string]::IsNullOrEmpty($config.LLM.ApiKey)) {
            Write-Host "❌ OpenRouter API key not configured" -ForegroundColor Red
            Write-Host "Get your free API key from: https://openrouter.ai/keys" -ForegroundColor Cyan
            Write-Host "Then update appsettings.json with your key" -ForegroundColor Yellow
        } else {
            Write-Host "✅ API key configured" -ForegroundColor Green
            Write-Host "Recommended free model: meta-llama/llama-3.2-3b-instruct:free" -ForegroundColor Cyan
        }
    }
    
    "Groq" {
        Write-Host ""
        if ([string]::IsNullOrEmpty($config.LLM.ApiKey)) {
            Write-Host "❌ Groq API key not configured" -ForegroundColor Red
            Write-Host "Get your free API key from: https://console.groq.com" -ForegroundColor Cyan
            Write-Host "Then update appsettings.json with your key" -ForegroundColor Yellow
        } else {
            Write-Host "✅ API key configured" -ForegroundColor Green
            Write-Host "Recommended free models:" -ForegroundColor Cyan
            Write-Host "  - llama-3.1-8b-instant" -ForegroundColor White
            Write-Host "  - mixtral-8x7b-32768" -ForegroundColor White
        }
    }
    
    default {
        Write-Host ""
        Write-Host "❌ Unknown provider: $provider" -ForegroundColor Red
        Write-Host "Supported providers: Ollama, OpenRouter, Groq" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Make sure your LLM provider is properly configured" -ForegroundColor White
Write-Host "2. Run the MCP server: dotnet run" -ForegroundColor White
Write-Host "3. The 'ask_llm' tool will be available if LLM is configured correctly" -ForegroundColor White
Write-Host ""
Write-Host "For detailed setup instructions, see: LLM_SETUP.md" -ForegroundColor Yellow
