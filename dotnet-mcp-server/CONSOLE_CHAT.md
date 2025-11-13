# Console Chat Mode

An interactive console interface for chatting with your configured LLM directly from the command line.

## 🚀 Quick Start

```powershell
# Option 1: Use the convenience script
.\chat.ps1

# Option 2: Run directly
dotnet run --project ChatConsole.csproj
```

## 💬 Features

- **Interactive Chat**: Type questions and get instant responses
- **Conversation History**: Maintains context throughout the session
- **System Prompts**: Set custom instructions for the AI
- **Color-Coded Output**: Easy to read conversation flow
- **Simple Commands**: Built-in commands for common tasks

## 🎮 Commands

| Command | Description |
|---------|-------------|
| `/exit` or `/quit` | Exit the application |
| `/clear` | Clear conversation history and start fresh |
| `/history` | Display the full conversation history |
| `/system` | Set a system prompt to guide AI behavior |

## 📝 Usage Examples

### Basic Chat
```
You: What is the capital of France?
AI: The capital of France is Paris.

You: What's the population?
AI: As of 2024, the population of Paris is approximately 2.1 million people...
```

### Using System Prompts
```
You: /system
System Prompt: You are an expert C# developer. Provide concise, working code examples.

You: How do I read a file?
AI: Here's how to read a file in C#:

using System.IO;

string content = File.ReadAllText("file.txt");
```

### Viewing History
```
You: /history

=== Conversation History ===
[System]: You are an expert C# developer...
You: How do I read a file?
AI: Here's how to read a file in C#...
```

## ⚙️ Configuration

The console chat uses the same `appsettings.json` configuration as the MCP server:

```json
{
  "LLM": {
    "Provider": "OpenRouter",
    "ApiKey": "your-api-key",
    "Model": "meta-llama/llama-3.2-3b-instruct:free"
  }
}
```

Supported providers:
- **Ollama** (local, free, unlimited)
- **OpenRouter** (cloud, free tier)
- **Groq** (cloud, fast, free tier)

See [LLM_SETUP.md](LLM_SETUP.md) for configuration details.

## 🎨 Interface

```
╔════════════════════════════════════════╗
║      LLM Console Chat Interface        ║
╚════════════════════════════════════════╝

✅ Connected to OpenRouter
   Model: meta-llama/llama-3.2-3b-instruct:free

Commands:
  /clear   - Clear conversation history
  /history - Show conversation history
  /exit    - Exit the application
  /system  - Set system prompt

You: [your message here]
AI: [response here]
```

## 💡 Tips

1. **Set a System Prompt Early**: Use `/system` to set the AI's role before asking questions
   - For code help: "You are an expert programmer"
   - For explanations: "You are a patient teacher"
   - For reviews: "You are a senior code reviewer"

2. **Use Conversation History**: The AI remembers previous messages, so you can have natural follow-up conversations

3. **Clear When Switching Topics**: Use `/clear` to start fresh when changing subjects

4. **Check History**: Use `/history` to see what the AI remembers

## 🔧 Troubleshooting

### "Could not load configuration"
- Ensure `appsettings.json` exists in the `dotnet-mcp-server` directory
- Verify the JSON is valid

### "Failed to initialize LLM service"
- **Ollama**: Make sure Ollama is running and the model is pulled
- **OpenRouter/Groq**: Verify your API key is correct

### Connection Errors
- **Ollama**: Check if Ollama is running: `ollama list`
- **Cloud APIs**: Check your internet connection and API key validity

## 🆚 Console Chat vs MCP Server

| Feature | Console Chat | MCP Server |
|---------|--------------|------------|
| Interface | Direct CLI | JSON-RPC over stdio |
| Use Case | Quick testing, exploration | Integration with MCP clients |
| Conversation | Interactive, multi-turn | Single tool calls |
| History | Maintained in session | Stateless |
| Output | Formatted, colored | JSON responses |

## 🎯 Use Cases

### 1. Quick Questions
Perfect for getting quick answers without setting up an MCP client:
```
You: What's the difference between async and await?
AI: [detailed explanation]
```

### 2. Testing Prompts
Test different system prompts before using them in your MCP tools:
```
You: /system
System Prompt: You are a technical writer...
You: Explain dependency injection
```

### 3. Code Generation
Get code snippets quickly:
```
You: Write a C# function to calculate Fibonacci
AI: [code example]
```

### 4. Learning and Exploration
Have a conversation to understand complex topics:
```
You: What is Model Context Protocol?
AI: [explanation]
You: How does it differ from REST APIs?
AI: [comparison]
```

## 🔗 Related Documentation

- [LLM_SETUP.md](LLM_SETUP.md) - Configure your LLM provider
- [EXAMPLES.md](EXAMPLES.md) - MCP server usage examples
- [README.md](README.md) - Main project documentation
