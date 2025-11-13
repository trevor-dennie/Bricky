# Bricky - BuilderTrend Documentation MCP Server

A specialized Model Context Protocol (MCP) server in .NET 8 that provides **AI-powered search** of BuilderTrend help documentation. Built to help developers and support teams quickly find relevant BuilderTrend articles using natural language queries.

## Overview

Bricky is an MCP server specifically designed to make BuilderTrend documentation accessible to AI assistants and chat applications. It crawls and indexes BuilderTrend help articles, then uses semantic search (powered by local or cloud LLMs) to find the most relevant articles based on the meaning of your query, not just keywords.

## Key Features

### 🎯 Core Functionality

**BuilderTrend Documentation Search** (`bt_documentation` tool)
- Crawls and indexes BuilderTrend help articles from buildertrend.com/help-articles
- **Semantic Search**: Understands the meaning of your questions, not just keywords
- Finds articles about concepts even if they don't use your exact words
- Smart caching with persistent storage for instant startup
- Returns top 5 most relevant articles with URLs and relevance scores

### 🔍 Semantic Search 🆕

The core feature that makes Bricky powerful:
- **Full content indexing** - Fetches and indexes actual article content (not just titles)
- **AI-powered embeddings** - Uses your configured LLM to understand content meaning
- **Automatic fallback** - Falls back to keyword search if semantic search fails
- **Persistent cache** - Stores embeddings to disk for instant subsequent searches
- **Smart updates** - Detects when articles change and re-indexes only what's needed

📚 **[Semantic Search Documentation](dotnet-mcp-server/SEMANTIC_SEARCH.md)**  
📚 **[Full Content Indexing Details](dotnet-mcp-server/FULL_CONTENT.md)**  
📚 **[Cache System Documentation](dotnet-mcp-server/CACHE.md)**

### 💬 Chat Interfaces

**BrickyChat GUI** - Modern WPF chat application
```powershell
cd BrickyChat
.\bricky.ps1
```
- Clippy-style friendly assistant with speech bubble  
- Visual chat history
- 🔊 Text-to-speech accessibility support
- Perfect for end users and demos

📚 **[BrickyChat Documentation](BrickyChat/README.md)**

**Console Chat Mode** - Terminal-based interface
```powershell
cd dotnet-mcp-server
.\chat.ps1
```
- Test your LLM configuration
- Experiment with BuilderTrend queries
- Debug search results
- Perfect for development and testing

📚 **[Console Chat Documentation](dotnet-mcp-server/CONSOLE_CHAT.md)**

**Utility Tools** (included for MCP protocol compliance)
- `echo` - Simple echo for testing MCP communication
- `get_time` - Returns current server time
- `ask_llm` - Direct LLM access for general questions

### 🤖 LLM Integration

Semantic search requires an LLM for generating embeddings. Choose from:
- **Ollama** (recommended) - Free, local, unlimited, private
- **OpenRouter** - Cloud, free tier available, multi-model
- **Groq** - Cloud, fast inference, generous free tier

📚 **[Complete LLM Setup Guide](dotnet-mcp-server/LLM_SETUP.md)**

## Use Cases

**For AI Assistants**: Connect Claude Desktop, VS Code Copilot, or other MCP clients to give them instant access to BuilderTrend documentation.

**For Support Teams**: Quickly find relevant help articles using natural language queries instead of exact keyword matching.

**For Developers**: Integrate BuilderTrend documentation search into your applications via the MCP protocol.

## Prerequisites

- .NET 8.0 SDK or later
- An LLM provider (Ollama, OpenRouter, or Groq) for semantic search
- Internet connection to access buildertrend.com

## Quick Start

1. **Clone and build**
   ```powershell
   cd Bricky/dotnet-mcp-server
   dotnet build
   ```

2. **Configure your LLM** (see [LLM Setup Guide](dotnet-mcp-server/LLM_SETUP.md))
   ```powershell
   # Copy example config
   copy appsettings.example.json appsettings.json
   
   # Edit appsettings.json with your LLM settings
   notepad appsettings.json
   ```

3. **Run the server**
   ```powershell
   dotnet run
   ```

The server communicates via stdin/stdout using JSON-RPC 2.0 protocol, making it compatible with any MCP client.

## How It Works

1. **First Run**: Bricky crawls BuilderTrend help articles and fetches full content
2. **Indexing**: Your LLM generates embeddings (vector representations) of each article's content
3. **Caching**: Embeddings are saved to disk (`cache/` directory) for instant future startups
4. **Search**: When you query, Bricky:
   - Generates an embedding of your question
   - Finds articles with similar embeddings (similar meaning)
   - Returns the top 5 most relevant results with URLs
5. **Fallback**: If semantic search fails, automatically falls back to keyword matching

## Project Structure

```
Bricky/
├── BrickyChat/                  # GUI Chat Application (WPF)
│   ├── MainWindow.xaml          # UI layout
│   ├── MainWindow.xaml.cs       # Chat logic
│   ├── App.xaml                 # WPF styles
│   ├── BrickyChatApp.cs         # Entry point
│   ├── BrickyChat.csproj        # Project file
│   ├── assets/BrickyV1.png      # Mascot image
│   └── README.md                # BrickyChat docs
├── dotnet-mcp-server/           # MCP Server & Shared Services
│   ├── Program.cs               # MCP protocol implementation
│   ├── BTDocumentationService.cs # BuilderTrend crawler & search
│   ├── SemanticSearchService.cs  # Semantic search engine
│   ├── LLMService.cs            # LLM integration layer
│   ├── EmbeddingCache.cs        # Persistent cache management
│   ├── ConsoleChat.cs           # Console chat interface
│   ├── appsettings.example.json # Configuration template
│   ├── McpServer.csproj         # MCP Server project
│   ├── ChatConsole.csproj       # Console chat project
│   └── cache/                   # Cached embeddings (auto-created)
└── README.md                    # This file
```

## Using the bt_documentation Tool

The primary tool for searching BuilderTrend documentation.

**Example Query**:
```json
{
  "name": "bt_documentation",
  "arguments": {
    "query": "how do I set up daily logs for my construction project?"
  }
}
```

**Response**:
```
Found 5 relevant article(s) for 'how do I set up daily logs...' (using semantic search):

1. Create and Use Daily Logs
   URL: https://buildertrend.com/help-articles/daily-logs-overview
   Relevance Score: 8.7
   Summary: Learn how to create daily logs to track daily activities...

2. Daily Log Mobile App Guide
   URL: https://buildertrend.com/help-articles/daily-logs-mobile
   Relevance Score: 7.2
   Summary: Use the BuilderTrend mobile app to create daily logs...
```

### Why Semantic Search Matters

**Traditional Keyword Search**:
- Query: "track weather on site" → Finds articles containing "track", "weather", "site"
- May miss relevant articles about "daily logs" or "site conditions"

**Bricky's Semantic Search**:
- Query: "track weather on site" → Understands you want to record site conditions
- Finds articles about daily logs, weather tracking, site documentation
- Returns relevant results even if they use different terminology

## Configuration

Create `appsettings.json` from the example:

```json
{
  "LLM": {
    "Provider": "Ollama",    // "Ollama", "OpenRouter", or "Groq"
    "ApiKey": "",            // Leave empty for Ollama, required for others
    "Model": "llama3.2"      // Model name for your provider
  }
}
```

**Ollama Setup** (Recommended):
```powershell
# Install from https://ollama.com
ollama pull llama3.2
# That's it! No API key needed
```

See **[LLM_SETUP.md](dotnet-mcp-server/LLM_SETUP.md)** for OpenRouter and Groq configuration.

## Connecting to MCP Clients

### Claude Desktop

Add to `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "buildertrend-docs": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\Bricky\\dotnet-mcp-server\\McpServer.csproj"],
      "env": {}
    }
  }
}
```

### Other MCP Clients

Bricky follows the standard MCP protocol and works with any MCP-compatible client. The server communicates via stdin/stdout using JSON-RPC 2.0.

## Performance

- **First run**: 2-5 minutes (fetches and indexes ~200+ BuilderTrend articles)
- **Subsequent runs**: ~5 seconds (loads from cache)
- **Cache size**: ~50-100MB depending on article count
- **Cache location**: `dotnet-mcp-server/cache/` directory
- **Cache validity**: Checks for article updates, only re-fetches changed content

## Testing & Development

### Console Chat Mode
Test the server interactively:
```powershell
cd dotnet-mcp-server
.\chat.ps1
```

Try queries like:
- "How do I add subcontractors to my project?"
- "What's the difference between change orders and selections?"
- "How can I track time on job sites?"

### Manual MCP Testing
Test raw JSON-RPC protocol:
```powershell
cd dotnet-mcp-server
.\test-mcp.ps1
```

Or use the Python test client:
```powershell
python test_mcp.py
```

### Debug Logging
The server logs to stderr (doesn't interfere with MCP communication):
```
[12:34:56] MCP Server starting...
[12:34:57] Crawled 247 articles from BuilderTrend help site
[12:34:57] Loading semantic search index...
[12:35:02] Semantic search index loaded (247 articles)
[12:35:03] Using semantic search...
[12:35:04] Semantic search found 5 results
```

## Troubleshooting

**"LLM service not configured"**
- Check that `appsettings.json` exists and has valid LLM configuration
- For Ollama, ensure the Ollama service is running: `ollama serve`
- For cloud providers, verify your API key is correct

**"No articles found"**
- Check internet connection
- BuilderTrend help site may be temporarily unavailable
- Clear cache and retry: `Remove-Item -Recurse cache/`

**"Semantic search failed, falling back to keyword search"**
- LLM may be overloaded or unavailable
- Check LLM service logs
- Keyword search will still work, just less accurate

**Slow first run**
- Normal! Fetching and indexing 200+ articles takes 2-5 minutes
- Subsequent runs use the cache and start in ~5 seconds

## Documentation

- **[LLM Setup Guide](dotnet-mcp-server/LLM_SETUP.md)** - Configure Ollama, OpenRouter, or Groq
- **[Semantic Search](dotnet-mcp-server/SEMANTIC_SEARCH.md)** - How semantic search works
- **[Cache System](dotnet-mcp-server/CACHE.md)** - Understanding the cache
- **[Console Chat](dotnet-mcp-server/CONSOLE_CHAT.md)** - Using the chat interface

## Contributing

This is a specialized MCP server focused on BuilderTrend documentation. To extend it:

1. **Add new documentation sources**: Modify `BTDocumentationService.cs` to crawl additional sites
2. **Improve search algorithms**: Enhance `SemanticSearchService.cs` or `SearchArticles()` 
3. **Add new tools**: Follow the MCP protocol in `Program.cs`

## License

MIT

---

**Built for**: BuilderTrend users, developers, and support teams  
**Powered by**: .NET 8, Model Context Protocol, and your choice of LLM  
**Purpose**: Make BuilderTrend documentation accessible and searchable by AI assistants
