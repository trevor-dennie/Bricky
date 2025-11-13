# LLM Integration Guide

This MCP server now supports integration with free-tier LLMs! You can choose from three options:

## 🚀 Quick Start Options

### Option 1: Ollama (Recommended for Local Use)
**Pros:** Completely free, runs locally, no API keys needed, privacy-friendly
**Cons:** Requires installation and downloads models (~2-4GB)

1. **Install Ollama:**
   - Download from [ollama.com](https://ollama.com)
   - Windows: Run the installer
   
2. **Pull a model:**
   ```powershell
   ollama pull llama3.2
   ```

3. **Configure `appsettings.json`:**
   ```json
   {
     "LLM": {
       "Provider": "Ollama",
       "ApiKey": "",
       "Model": "llama3.2"
     }
   }
   ```

4. **Run your MCP server:**
   ```powershell
   dotnet run
   ```

### Option 2: OpenRouter (Free Cloud Models)
**Pros:** No local installation, free tier available, access to multiple models
**Cons:** Requires API key, rate limits on free tier

1. **Get API Key:**
   - Visit [openrouter.ai/keys](https://openrouter.ai/keys)
   - Sign up and get your free API key

2. **Configure `appsettings.json`:**
   ```json
   {
     "LLM": {
       "Provider": "OpenRouter",
       "ApiKey": "your-api-key-here",
       "Model": "meta-llama/llama-3.2-3b-instruct:free"
     }
   }
   ```

   **Other free models on OpenRouter:**
   - `meta-llama/llama-3.2-3b-instruct:free`
   - `meta-llama/llama-3.2-1b-instruct:free`
   - `google/gemma-2-9b-it:free`

### Option 3: Groq (Fast Free Cloud API)
**Pros:** Very fast inference, generous free tier, no installation
**Cons:** Requires API key, rate limits

1. **Get API Key:**
   - Visit [console.groq.com](https://console.groq.com)
   - Sign up and create an API key

2. **Configure `appsettings.json`:**
   ```json
   {
     "LLM": {
       "Provider": "Groq",
       "ApiKey": "your-api-key-here",
       "Model": "llama-3.1-8b-instant"
     }
   }
   ```

   **Free models on Groq:**
   - `llama-3.1-8b-instant`
   - `llama-3.2-3b-preview`
   - `mixtral-8x7b-32768`
   - `gemma2-9b-it`

## 🛠️ Using the LLM Tool

Once configured, your MCP server will expose an `ask_llm` tool that can be called with:

```json
{
  "name": "ask_llm",
  "arguments": {
    "prompt": "What is the capital of France?",
    "systemPrompt": "You are a helpful geography assistant." // Optional
  }
}
```

### Example Usage in MCP Client

The tool will be available alongside your other tools (echo, get_time, bt_documentation).

## 📊 Comparison Table

| Provider   | Free Tier | Speed      | Privacy    | Setup Difficulty |
|------------|-----------|------------|------------|------------------|
| Ollama     | Unlimited | Medium     | Complete   | Medium           |
| OpenRouter | Limited   | Medium     | Cloud      | Easy             |
| Groq       | Generous  | Very Fast  | Cloud      | Easy             |

## 🔧 Troubleshooting

### Ollama Issues
- **"Connection refused"**: Make sure Ollama is running (`ollama serve`)
- **"Model not found"**: Pull the model first (`ollama pull llama3.2`)
- Check if Ollama is running: `ollama list`

### API Key Issues
- Verify your API key is correct in `appsettings.json`
- Check your API key hasn't expired
- Ensure you're within rate limits

### General Issues
- Check the console error output for detailed error messages
- Verify `appsettings.json` is in the same directory as the executable
- Make sure the Provider name matches exactly (case-sensitive)

## 🎯 Best Practices

1. **For Development:** Use Ollama for unlimited free queries
2. **For Production:** Consider Groq for speed or OpenRouter for model variety
3. **For Privacy:** Always use Ollama when handling sensitive data
4. **System Prompts:** Use system prompts to guide the LLM's behavior and get better results

## 📝 Example Prompts

```
Simple question:
"Explain what MCP (Model Context Protocol) is in simple terms"

With system prompt:
prompt: "How do I debug a C# application?"
systemPrompt: "You are an expert C# developer. Provide concise, practical advice."

Complex query:
"Write a function that calculates the Fibonacci sequence in C#"
```

## 🔗 Additional Resources

- [Ollama Documentation](https://github.com/ollama/ollama)
- [OpenRouter Documentation](https://openrouter.ai/docs)
- [Groq Documentation](https://console.groq.com/docs)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
