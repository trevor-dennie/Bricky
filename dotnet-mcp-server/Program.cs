using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration
        var config = LoadConfiguration();
        
        var server = new MCPServer(config);
        await server.RunAsync();
    }

    private static LLMConfig LoadConfiguration()
    {
        try
        {
            if (File.Exists("appsettings.json"))
            {
                var json = File.ReadAllText("appsettings.json");
                var doc = JsonDocument.Parse(json);
                var llmSection = doc.RootElement.GetProperty("LLM");
                
                var providerStr = llmSection.GetProperty("Provider").GetString() ?? "Ollama";
                var apiKey = llmSection.GetProperty("ApiKey").GetString();
                var model = llmSection.GetProperty("Model").GetString();

                return new LLMConfig
                {
                    Provider = Enum.Parse<LLMProvider>(providerStr, true),
                    ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
                    Model = string.IsNullOrWhiteSpace(model) ? null : model
                };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not load configuration: {ex.Message}");
        }

        // Default to Ollama
        return new LLMConfig { Provider = LLMProvider.Ollama };
    }
}


public class MCPServer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly BTDocumentationService _btDocService;
    private readonly LLMService? _llmService;

    public MCPServer(LLMConfig? config = null)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        
        // Initialize LLM service if configured
        if (config != null)
        {
            try
            {
                _llmService = new LLMService(config.Provider, config.ApiKey, config.Model);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Could not initialize LLM service: {ex.Message}");
            }
        }
        
        // Initialize BTDocService with LLM service for semantic search
        _btDocService = new BTDocumentationService(_llmService);
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
        var tools = new List<object>
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
        };

        // Add LLM tool if service is available
        if (_llmService != null)
        {
            tools.Add(new
            {
                name = "ask_llm",
                description = "Ask a question to the configured LLM (AI assistant). Useful for getting AI-generated answers, analysis, or assistance with various tasks.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        prompt = new
                        {
                            type = "string",
                            description = "The question or prompt to send to the LLM"
                        },
                        systemPrompt = new
                        {
                            type = "string",
                            description = "Optional system prompt to set context or instructions for the LLM"
                        }
                    },
                    required = new[] { "prompt" }
                }
            });
        }

        return new JsonRPCResponse
        {
            Jsonrpc = "2.0",
            Id = request.Id,
            Result = new
            {
                tools = tools.ToArray()
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
                "ask_llm" => await HandleAskLLMToolAsync(arguments),
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

    private async Task<object> HandleAskLLMToolAsync(JsonElement? arguments)
    {
        if (_llmService == null)
        {
            throw new Exception("LLM service is not configured. Please check appsettings.json");
        }

        if (arguments == null || !arguments.Value.TryGetProperty("prompt", out var promptElement))
        {
            throw new Exception("Missing required parameter: prompt");
        }

        var prompt = promptElement.GetString() ?? "";
        
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new Exception("Prompt parameter cannot be empty");
        }

        string? systemPrompt = null;
        if (arguments.Value.TryGetProperty("systemPrompt", out var systemElement))
        {
            systemPrompt = systemElement.GetString();
        }

        await LogAsync($"Sending prompt to LLM: {prompt}");
        var result = await _llmService.ChatAsync(prompt, systemPrompt);
        
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
