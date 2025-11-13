# Quick Reference: LLM Integration

## ⚡ TL;DR

Your MCP server now has an `ask_llm` tool! Choose one:

### 🏠 Ollama (Local, Free, Private)
```powershell
# 1. Install from https://ollama.com
# 2. Pull model
ollama pull llama3.2
# 3. Run server
dotnet run
```

### ☁️ Groq (Fast Cloud, Free Tier)
```json
// appsettings.json
{
  "LLM": {
    "Provider": "Groq",
    "ApiKey": "gsk_...",  // Get from console.groq.com
    "Model": "llama-3.1-8b-instant"
  }
}
```

### ☁️ OpenRouter (Multi-Model, Free Tier)
```json
// appsettings.json
{
  "LLM": {
    "Provider": "OpenRouter",
    "ApiKey": "sk-or-...",  // Get from openrouter.ai/keys
    "Model": "meta-llama/llama-3.2-3b-instruct:free"
  }
}
```

## 🧪 Test Your Setup
```powershell
.\test-llm.ps1
```

## 📖 Full Documentation
- **Setup**: [LLM_SETUP.md](LLM_SETUP.md)
- **Examples**: [EXAMPLES.md](EXAMPLES.md)
- **Summary**: [INTEGRATION_SUMMARY.md](INTEGRATION_SUMMARY.md)

## 🎯 Example Usage

```json
{
  "name": "ask_llm",
  "arguments": {
    "prompt": "Your question here",
    "systemPrompt": "Optional: Set context/role"
  }
}
```

## 🐛 Common Issues

| Problem | Solution |
|---------|----------|
| `ask_llm` tool missing | Check `appsettings.json` exists and is valid |
| Ollama connection failed | Install Ollama, run `ollama pull llama3.2` |
| API key error | Verify key in `appsettings.json` |

## 📊 Provider Comparison

| Feature | Ollama | OpenRouter | Groq |
|---------|--------|------------|------|
| Cost | Free | Free tier | Free tier |
| Speed | Medium | Medium | Fast |
| Privacy | 100% local | Cloud | Cloud |
| Setup | Install app | API key | API key |

**Recommended**: Start with Ollama for unlimited free usage!
