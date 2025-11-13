using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

// Simple test client for MCP server
class McpTestClient
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║      MCP Server Test Client            ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        var testQuery = args.Length > 0 ? string.Join(" ", args) : "how to create an invoice";
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Test Query: \"{testQuery}\"");
        Console.ResetColor();
        Console.WriteLine();

        // Create JSON-RPC requests
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0"
                }
            }
        };

        var toolsListRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list"
        };

        var btDocRequest = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "bt_documentation",
                arguments = new
                {
                    query = testQuery
                }
            }
        };

        try
        {
            // Start MCP server process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project McpServer.csproj",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            Console.WriteLine("Starting MCP server...");
            process.Start();

            // Wait a bit for server to start
            await Task.Delay(1000);

            var stdin = process.StandardInput;
            var stdout = process.StandardOutput;

            // Send initialize
            Console.WriteLine("Sending initialize request...");
            await stdin.WriteLineAsync(JsonSerializer.Serialize(initRequest));
            await stdin.FlushAsync();
            var initResponse = await stdout.ReadLineAsync();
            Console.WriteLine("✅ Initialized");
            Console.WriteLine();

            // Send tools/list
            Console.WriteLine("Sending tools/list request...");
            await stdin.WriteLineAsync(JsonSerializer.Serialize(toolsListRequest));
            await stdin.FlushAsync();
            var toolsResponse = await stdout.ReadLineAsync();
            Console.WriteLine("✅ Got tools list");
            Console.WriteLine();

            // Send bt_documentation request
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Calling bt_documentation tool...");
            Console.WriteLine("(This will fetch full content and index - may take 1-2 minutes first time)");
            Console.ResetColor();
            Console.WriteLine();

            var startTime = DateTime.Now;
            await stdin.WriteLineAsync(JsonSerializer.Serialize(btDocRequest));
            await stdin.FlushAsync();

            // Read stderr in background to show progress
            _ = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(line);
                        Console.ResetColor();
                    }
                }
            });

            var btDocResponse = await stdout.ReadLineAsync();
            var elapsed = DateTime.Now - startTime;

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ RESPONSE RECEIVED:");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine();

            if (btDocResponse != null)
            {
                var response = JsonSerializer.Deserialize<JsonElement>(btDocResponse);
                if (response.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("content", out var content))
                    {
                        var contentArray = content.EnumerateArray();
                        foreach (var item in contentArray)
                        {
                            if (item.TryGetProperty("text", out var text))
                            {
                                Console.WriteLine(text.GetString());
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"⏱️  Request completed in {elapsed.TotalSeconds:F1} seconds");
            Console.ResetColor();

            // Cleanup
            process.Kill();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
        }
    }
}
