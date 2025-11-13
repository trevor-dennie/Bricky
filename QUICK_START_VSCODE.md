# Quick Start: Connect Bricky to VS Code

Follow these simple steps to connect the Bricky MCP server to VS Code:

## Step 1: Open VS Code Settings

1. Open VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Type "Preferences: Open User Settings (JSON)"
4. Press Enter

## Step 2: Add Bricky MCP Configuration

Copy and paste this configuration into your `settings.json`:

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

**Note:** If you already have other settings in your `settings.json`, just add the `"github.copilot.chat.mcp.servers"` section to your existing configuration.

## Step 3: Restart VS Code

1. Close all VS Code windows
2. Reopen VS Code

## Step 4: Test Bricky

Open GitHub Copilot Chat and try:

```
@bricky search BuilderTrend documentation for "how to add users"
```

or

```
@bricky get current time
```

## That's It!

You're now connected to Bricky! 🧱✨

---

## Common Issues

### "bricky" not showing up in Copilot Chat

- Make sure GitHub Copilot extension is installed and enabled
- Check that you have .NET 8.0 SDK installed: `dotnet --version`
- Verify the path in your settings.json matches your actual project location

### BuilderTrend search is slow

The first search takes 50-90 seconds to build the search index. After that, searches are much faster (1-2 seconds).

---

For more detailed setup instructions, see [MCP_SETUP.md](MCP_SETUP.md)
