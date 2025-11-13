# .NET MCP Server

A basic Model Context Protocol (MCP) server implementation in .NET 8.

## Overview

This is a simple MCP server that demonstrates the core concepts of the Model Context Protocol. It implements a JSON-RPC interface over stdio and provides three tools:

- **echo**: Echoes back a message
- **get_time**: Returns the current server time
- **bt_documentation**: Searches BuilderTrend help articles for relevant information

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
├── BTDocumentationService.cs   # BuilderTrend help article crawler
├── McpServer.csproj            # Project file
├── Properties/
│   └── launchSettings.json     # Launch configuration
└── README.md                   # This file
```

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
