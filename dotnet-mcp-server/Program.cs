using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new MCPServer();
        await server.RunAsync();
    }
}

public class MCPServer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly BTDocumentationService _btDocService;

    public MCPServer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        _btDocService = new BTDocumentationService();
    }

    public async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        // Log to stderr for debugging
        await LogAsync("MCP Server starting...");

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            await LogAsync($"Received: {line}");

            try
            {
                var request = JsonSerializer.Deserialize<JsonRPCRequest>(line, _jsonOptions);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                var responseJson = JsonSerializer.Serialize(response, _jsonOptions);

                await writer.WriteLineAsync(responseJson);
                await LogAsync($"Sent: {responseJson}");
            }
            catch (Exception ex)
            {
                await LogAsync($"Error: {ex.Message}");
                var errorResponse = new JsonRPCResponse
                {
                    Jsonrpc = "2.0",
                    Id = null,
                    Error = new JsonRPCError
                    {
                        Code = -32603,
                        Message = ex.Message
                    }
                };
                var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                await writer.WriteLineAsync(errorJson);
            }
        }
    }

    private async Task<JsonRPCResponse> HandleRequestAsync(JsonRPCRequest request)
    {
        await LogAsync($"Handling method: {request.Method}");

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCallAsync(request),
            "notifications/initialized" => HandleInitialized(request),
            _ => new JsonRPCResponse
            {
                Jsonrpc = "2.0",
                Id = request.Id,
                Error = new JsonRPCError
                {
                    Code = -32601,
                    Message = $"Method not found: {request.Method}"
                }
            }
        };
    }

    private JsonRPCResponse HandleInitialize(JsonRPCRequest request)
    {
        return new JsonRPCResponse
        {
            Jsonrpc = "2.0",
            Id = request.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { }
                },
                serverInfo = new
                {
                    name = "dotnet-mcp-server",
                    version = "1.0.0"
                }
            }
        };
    }

    private JsonRPCResponse HandleInitialized(JsonRPCRequest request)
    {
        // No response needed for notifications
        return new JsonRPCResponse
        {
            Jsonrpc = "2.0",
            Id = request.Id,
            Result = new { }
        };
    }

    private JsonRPCResponse HandleToolsList(JsonRPCRequest request)
    {
        return new JsonRPCResponse
        {
            Jsonrpc = "2.0",
            Id = request.Id,
            Result = new
            {
                tools = new object[]
                {
                    new
                    {
                        name = "echo",
                        description = "Echoes back the provided message",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                message = new
                                {
                                    type = "string",
                                    description = "The message to echo back"
                                }
                            },
                            required = new[] { "message" }
                        }
                    },
                    new
                    {
                        name = "get_time",
                        description = "Returns the current server time",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new { }
                        }
                    },
                    new
                    {
                        name = "bt_documentation",
                        description = "Searches BuilderTrend help articles for relevant information based on a query. Returns the top matching articles with URLs and snippets.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new
                            {
                                query = new
                                {
                                    type = "string",
                                    description = "The search query or question to find relevant BuilderTrend help articles"
                                }
                            },
                            required = new[] { "query" }
                        }
                    }
                }
            }
        };
    }

    private async Task<JsonRPCResponse> HandleToolsCallAsync(JsonRPCRequest request)
    {
        try
        {
            var paramsElement = (JsonElement)request.Params!;
            var toolName = paramsElement.GetProperty("name").GetString();
            
            var arguments = paramsElement.TryGetProperty("arguments", out var argsElement)
                ? argsElement
                : (JsonElement?)null;

            await LogAsync($"Tool call: {toolName}");

            var result = toolName switch
            {
                "echo" => HandleEchoTool(arguments),
                "get_time" => HandleGetTimeTool(),
                "bt_documentation" => await HandleBTDocumentationToolAsync(arguments),
                _ => throw new Exception($"Unknown tool: {toolName}")
            };

            return new JsonRPCResponse
            {
                Jsonrpc = "2.0",
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            await LogAsync($"Tool call error: {ex.Message}");
            return new JsonRPCResponse
            {
                Jsonrpc = "2.0",
                Id = request.Id,
                Error = new JsonRPCError
                {
                    Code = -32602,
                    Message = ex.Message
                }
            };
        }
    }

    private object HandleEchoTool(JsonElement? arguments)
    {
        if (arguments == null || !arguments.Value.TryGetProperty("message", out var messageElement))
        {
            throw new Exception("Missing required parameter: message");
        }

        var message = messageElement.GetString() ?? "";
        
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = $"Echo: {message}"
                }
            }
        };
    }

    private object HandleGetTimeTool()
    {
        var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = $"Current server time: {currentTime}"
                }
            }
        };
    }

    private async Task<object> HandleBTDocumentationToolAsync(JsonElement? arguments)
    {
        if (arguments == null || !arguments.Value.TryGetProperty("query", out var queryElement))
        {
            throw new Exception("Missing required parameter: query");
        }

        var query = queryElement.GetString() ?? "";
        
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new Exception("Query parameter cannot be empty");
        }

        await LogAsync($"Searching BuilderTrend documentation for: {query}");
        var result = await _btDocService.SearchDocumentationAsync(query);
        
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task LogAsync(string message)
    {
        await Console.Error.WriteLineAsync($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}

public class JsonRPCRequest
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class JsonRPCResponse
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRPCError? Error { get; set; }
}

public class JsonRPCError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}
