using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║    MCP Server Test Client                ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.WriteLine();

var query = args.Length > 0 ? string.Join(" ", args) : "project schedule management";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"Query: \"{query}\"");
Console.ResetColor();
Console.WriteLine();

try
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project ../McpServer.csproj",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi)!;
    
    Console.WriteLine("Starting MCP server...");
    
    // Wait for server to output startup message
    var startupLine = await process.StandardError.ReadLineAsync();
    while (startupLine != null && !startupLine.Contains("MCP Server starting"))
    {
        startupLine = await process.StandardError.ReadLineAsync();
    }
    Console.WriteLine($"  {startupLine}");
    await Task.Delay(500);
    
    // Initialize
    var initMsg = @"{""jsonrpc"":""2.0"",""id"":1,""method"":""initialize"",""params"":{""protocolVersion"":""2024-11-05"",""capabilities"":{},""clientInfo"":{""name"":""test"",""version"":""1.0""}}}";
    await process.StandardInput.WriteLineAsync(initMsg);
    await process.StandardInput.FlushAsync();
    
    var initResponse = await process.StandardOutput.ReadLineAsync();
    Console.WriteLine("✅ Initialized");
    Console.WriteLine();
    
    // Call bt_documentation
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Calling bt_documentation...");
    Console.WriteLine("(First run: ~90s for content fetch, Cached: ~2s)");
    Console.ResetColor();
    Console.WriteLine();
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Start reading stderr for progress
    var stderrTask = Task.Run(async () =>
    {
        while (!process.StandardError.EndOfStream)
        {
            var line = await process.StandardError.ReadLineAsync();
            if (line != null && (line.Contains("Fetching") || line.Contains("Loading") || line.Contains("Indexing")))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {line}");
                Console.ResetColor();
            }
        }
    });
    
    var toolMsg = $@"{{""jsonrpc"":""2.0"",""id"":2,""method"":""tools/call"",""params"":{{""name"":""bt_documentation"",""arguments"":{{""query"":""{query}""}}}}}}";
    await process.StandardInput.WriteLineAsync(toolMsg);
    await process.StandardInput.FlushAsync();
    
    var toolResponse = await process.StandardOutput.ReadLineAsync();
    stopwatch.Stop();
    
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ RESULTS:");
    Console.ResetColor();
    Console.WriteLine("═══════════════════════════════════════════════");
    Console.WriteLine();
    
    if (toolResponse != null)
    {
        using var doc = JsonDocument.Parse(toolResponse);
        if (doc.RootElement.TryGetProperty("result", out var result))
        {
            if (result.TryGetProperty("content", out var content))
            {
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var text))
                    {
                        var str = text.GetString();
                        Console.WriteLine(str);
                    }
                }
            }
        }
        else if (doc.RootElement.TryGetProperty("error", out var error))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {error}");
            Console.ResetColor();
        }
    }
    
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"⏱️  Completed in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
    Console.ResetColor();
    
    process.Kill();
    await process.WaitForExitAsync();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}
