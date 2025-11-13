# .NET MCP Server

A Model Context Protocol (MCP) server implementation in .NET 8 with integrated LLM support.

## Overview

This MCP server demonstrates the core concepts of the Model Context Protocol with practical tools including BuilderTrend documentation search and **LLM integration** (Ollama, OpenRouter, or Groq).

## Features

### 💬 Console Chat Mode 🆕

Interactive command-line interface for direct LLM conversations:
```powershell
.\chat.ps1
```
- Chat directly with the AI in your terminal
- Maintain conversation history
- Set system prompts for specific behaviors
- Perfect for testing and quick questions

📚 **[Console Chat Documentation](CONSOLE_CHAT.md)**

### Available Tools (MCP Server Mode)

- **echo**: Echoes back a message
- **get_time**: Returns the current server time
- **bt_documentation**: Searches BuilderTrend help articles for relevant information
- **ask_llm**: Ask questions to a free-tier LLM (AI assistant)

### 🤖 LLM Integration

Connect to free LLM providers:
- **Ollama** (local, unlimited, private)
- **OpenRouter** (cloud, free tier available)
- **Groq** (cloud, fast, generous free tier)

📚 **[See LLM Setup Guide](LLM_SETUP.md)** for detailed configuration instructions.

## Prerequisites

- .NET 8.0 SDK or later

## Building the Project

```powershell
cd dotnet-mcp-server
dotnet build
```

## Running the Server

```powershell
dotnet run
```

The server communicates via stdin/stdout using JSON-RPC 2.0 protocol.

## Project Structure

```
dotnet-mcp-server/
├── Program.cs                  # Main server implementation
├── LLMService.cs               # LLM integration service
├── BTDocumentationService.cs   # BuilderTrend help article crawler
├── appsettings.json            # LLM configuration
├── McpServer.csproj            # Project file
├── LLM_SETUP.md               # LLM setup guide
├── Properties/
│   └── launchSettings.json     # Launch configuration
└── README.md                   # This file
```

## Quick Start with LLM

1. **Choose your LLM provider** (Ollama recommended for beginners):
   ```powershell
   # Install Ollama from https://ollama.com
   ollama pull llama3.2
   ```

2. **Configure `appsettings.json`**:
   ```json
   {
     "LLM": {
       "Provider": "Ollama",
       "ApiKey": "",
       "Model": "llama3.2"
     }
   }
   ```

3. **Run the server**:
   ```powershell
   dotnet run
   ```

See **[LLM_SETUP.md](LLM_SETUP.md)** for detailed setup instructions for all providers.

## BTDocumentation Tool

The `bt_documentation` tool crawls the BuilderTrend help articles site and searches for relevant content based on your query. Features include:

- **Smart Caching**: Articles are cached for 1 hour to improve performance
- **Relevance Scoring**: Results are ranked by relevance to your query
- **Multiple Search Strategies**: Searches both titles and descriptions
- **Top Results**: Returns up to 5 most relevant articles with URLs and snippets

### Usage Example

When you call the tool with a query like "how to create a schedule", it will:
1. Crawl https://buildertrend.com/help-articles/ (if not cached)
2. Search for articles matching your query terms
3. Return the top 5 most relevant results with links and descriptions

## Implementation Details

### Core Components

1. **MCPServer Class**: Main server that handles JSON-RPC communication
2. **JSON-RPC Models**: Request/Response structures for protocol compliance
3. **Tool Handlers**: Individual methods for each tool implementation

### Supported Methods

- `initialize`: Initializes the server and exchanges capabilities
- `tools/list`: Returns the list of available tools
- `tools/call`: Executes a specific tool with provided arguments
- `notifications/initialized`: Acknowledges initialization completion

### Adding New Tools

To add a new tool:

1. Add the tool definition in `HandleToolsList()`
2. Add a case in `HandleToolsCallAsync()` switch statement
3. Implement the tool handler method

Example:

```csharp
private object HandleMyNewTool(JsonElement? arguments)
{
    // Your tool logic here
    return new
    {
        content = new[]
        {
            new
            {
                type = "text",
                text = "Tool result"
            }
        }
    };
}
```

## Testing

You can test the server by sending JSON-RPC requests via stdin. Example initialize request:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}
```

Example tool list request:

```json
{"jsonrpc":"2.0","id":2,"method":"tools/list"}
```

Example tool call (echo):

```json
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"echo","arguments":{"message":"Hello, World!"}}}
```

Example tool call (bt_documentation):

```json
{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"bt_documentation","arguments":{"query":"how to create a schedule"}}}
```

## Logging

The server logs debug information to stderr, which won't interfere with the JSON-RPC communication on stdout.

## License

MIT
