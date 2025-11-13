# Setting Up Bricky MCP Server in VS Code

This guide will help you connect the Bricky MCP server to VS Code so you can interact with BuilderTrend documentation through GitHub Copilot Chat.

## Prerequisites

1. **VS Code** with GitHub Copilot installed
2. **.NET 8.0 SDK** installed
3. **Ollama** (or another LLM provider) running locally

## Step 1: Create appsettings.json

Navigate to the `dotnet-mcp-server` directory and create your configuration:

```bash
cd c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server
copy appsettings.example.json appsettings.json
```

Edit `appsettings.json` to configure your LLM provider:

**For Ollama (default):**
```json
{
  "LLM": {
    "Provider": "Ollama",
    "ApiKey": "",
    "Model": "llama3.2"
  }
}
```

**For OpenRouter:**
```json
{
  "LLM": {
    "Provider": "OpenRouter",
    "ApiKey": "your-api-key-here",
    "Model": "meta-llama/llama-3.2-3b-instruct:free"
  }
}
```

**For Groq:**
```json
{
  "LLM": {
    "Provider": "Groq",
    "ApiKey": "your-api-key-here",
    "Model": "llama-3.1-8b-instant"
  }
}
```

## Step 2: Build the MCP Server

Build the project to ensure everything compiles:

```bash
cd c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server
dotnet build
```

## Step 3: Configure VS Code MCP Settings

You need to add the Bricky MCP server to your VS Code settings. There are two ways to do this:

### Option A: Using VS Code Settings UI (Recommended)

1. Open VS Code
2. Press `Ctrl+Shift+P` and search for "Preferences: Open User Settings (JSON)"
3. Add the following configuration to your `settings.json`:

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

### Option B: Using the compiled executable (Faster)

First, publish the server:

```bash
cd c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server
dotnet publish -c Release -o publish
```

Then add this to your VS Code `settings.json`:

```json
{
  "github.copilot.chat.mcp.servers": {
    "bricky": {
      "command": "c:\\Users\\trevor.Dennie\\source\\repos\\trevor-dennie\\Bricky\\dotnet-mcp-server\\publish\\McpServer.exe"
    }
  }
}
```

## Step 4: Restart VS Code

After adding the configuration:
1. Close all VS Code windows
2. Reopen VS Code
3. The Bricky MCP server should now be available

## Step 5: Test the Connection

Open GitHub Copilot Chat in VS Code and try these commands:

1. **Test the echo tool:**
   ```
   @bricky echo "Hello Bricky!"
   ```

2. **Get current time:**
   ```
   @bricky get current time
   ```

3. **Search BuilderTrend documentation:**
   ```
   @bricky search BuilderTrend documentation for "how to add users"
   ```

4. **Ask the LLM directly:**
   ```
   @bricky ask llm "What is BuilderTrend?"
   ```

## Available Tools

Once connected, Bricky provides these tools:

### 1. **bt_documentation**
Searches BuilderTrend help articles for relevant information.
```
@bricky search documentation for "purchase orders"
```

### 2. **ask_llm** (if LLM configured)
Ask questions to the configured LLM.
```
@bricky ask llm "Explain semantic search"
```

### 3. **echo**
Simple echo tool for testing.
```
@bricky echo "test message"
```

### 4. **get_time**
Returns the current server time.
```
@bricky get current time
```

## Troubleshooting

### MCP Server Not Appearing in Copilot

1. Check VS Code Developer Tools (`Help` → `Toggle Developer Tools`)
2. Look for any errors related to MCP servers
3. Verify the paths in your `settings.json` are correct
4. Make sure .NET 8.0 SDK is in your PATH

### "LLM service is not configured" Error

1. Verify `appsettings.json` exists in the `dotnet-mcp-server` directory
2. Check that your LLM provider is running (e.g., Ollama on port 11434)
3. For API-based providers, verify your API key is valid

### BuilderTrend Search Taking Too Long

The first search takes 50-90 seconds as it:
1. Crawls the BuilderTrend help site
2. Generates embeddings for all articles
3. Caches the results for future searches

Subsequent searches are much faster (1-2 seconds).

### Rebuild the Embedding Cache

If search results seem outdated:
```bash
cd c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server
rm embeddings_cache.json
```

Next search will rebuild the cache with fresh data.

## Advanced Configuration

### Using a Different LLM Model

Edit `appsettings.json` and change the `Model` field:

```json
{
  "LLM": {
    "Provider": "Ollama",
    "Model": "llama3.1"
  }
}
```

### Running in Development Mode

For debugging, run the server manually:

```bash
cd c:\Users\trevor.Dennie\source\repos\trevor-dennie\Bricky\dotnet-mcp-server
dotnet run
```

Then interact via stdio (type JSON-RPC commands).

## Example Workflows

### Document Research Workflow

1. **Start broad:**
   ```
   @bricky search documentation for "project management"
   ```

2. **Narrow down:**
   ```
   @bricky search documentation for "creating a new project"
   ```

3. **Ask follow-up questions:**
   ```
   @bricky ask llm "Based on the BuilderTrend docs, what are the key steps to set up a new project?"
   ```

### Getting Help Workflow

1. **Find relevant articles:**
   ```
   @bricky search documentation for "user permissions"
   ```

2. **Get AI explanation:**
   ```
   @bricky ask llm "Summarize the user permission levels in BuilderTrend"
   ```

---

**Need help?** Check the main README or create an issue in the repository.
