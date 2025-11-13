# Bricky MCP Server - VS Code Setup Complete! 🎉

Your Bricky MCP server is now ready to connect to VS Code!

## What We Set Up

1. ✅ **Built the MCP Server** - The dotnet-mcp-server is compiled and ready
2. ✅ **Created Setup Documentation** - Complete guides for VS Code integration
3. ✅ **Configuration Files** - Ready-to-use settings templates

## Quick Start (3 Simple Steps)

### Step 1: Add Configuration to VS Code

Open VS Code Settings JSON (`Ctrl+Shift+P` → "Preferences: Open User Settings (JSON)") and add:

```json
{
  "github.copilot.chat.mcp.servers": {
    "bricky": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "c:\\Users\\trevor.Dennie\\source\\repos\\trevor-dennie\\Bricky\\dotnet-mcp-server\\McpServer.csproj"
      ]
    }
  }
}
```

### Step 2: Restart VS Code

Close and reopen VS Code completely.

### Step 3: Test It!

Open GitHub Copilot Chat and try:
```
@bricky get current time
```

or

```
@bricky search BuilderTrend documentation for "adding users"
```

## Available Documentation

📚 **[QUICK_START_VSCODE.md](QUICK_START_VSCODE.md)** - Fast 3-step setup guide  
📚 **[MCP_SETUP.md](MCP_SETUP.md)** - Detailed setup with troubleshooting  
📚 **[vscode-settings.json](vscode-settings.json)** - Copy-paste settings template

## Available Tools via @bricky

Once connected, you can use these tools in Copilot Chat:

### 🔍 `bt_documentation`
Search BuilderTrend help articles using natural language:
```
@bricky search documentation for "purchase orders"
```

### 🤖 `ask_llm`
Ask questions directly to your configured LLM:
```
@bricky ask llm "What is semantic search?"
```

### 🔧 `get_time`
Get current server time (useful for testing):
```
@bricky get current time
```

### 📣 `echo`
Echo back a message (useful for testing MCP connection):
```
@bricky echo "Hello Bricky!"
```

## Configuration

Your MCP server uses settings from:
```
c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server\appsettings.json
```

To change LLM provider or model, edit that file.

## Troubleshooting

### Bricky not appearing in Copilot

1. Make sure GitHub Copilot is installed and active
2. Verify .NET 8.0 SDK is installed: `dotnet --version`
3. Check the path in your VS Code settings matches your actual location
4. Restart VS Code completely (close all windows)

### First search is slow

The initial BuilderTrend search takes 50-90 seconds to:
- Crawl the help site
- Generate embeddings
- Cache the results

After that, searches are fast (1-2 seconds)!

### Need more help?

See the detailed guides:
- [MCP_SETUP.md](MCP_SETUP.md) - Full setup guide
- [dotnet-mcp-server/README.md](dotnet-mcp-server/README.md) - Server documentation

---

**You're all set!** 🧱✨ Enjoy using Bricky in VS Code!
