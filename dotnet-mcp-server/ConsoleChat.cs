using System.Text.Json;

namespace McpServer;

class ConsoleChat
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║      LLM Console Chat Interface        ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        // Load configuration
        var config = LoadConfiguration();
        
        if (config == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Could not load configuration from appsettings.json");
            Console.ResetColor();
            Console.WriteLine("Make sure appsettings.json exists and is properly configured.");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Initialize LLM service
        LLMService? llmService;
        BTDocumentationService? btDocService = null;
        
        try
        {
            llmService = new LLMService(config.Provider, config.ApiKey, config.Model);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Connected to {config.Provider}");
            Console.ResetColor();
            Console.WriteLine($"   Model: {config.Model ?? "(default)"}");
            Console.WriteLine();
            
            // Initialize BT Documentation service
            try
            {
                btDocService = new BTDocumentationService(llmService);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ BuilderTrend documentation search enabled");
                Console.ResetColor();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  BuilderTrend search disabled: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Failed to initialize LLM service: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Conversation history
        var conversationHistory = new List<Message>();

        // Show instructions
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Commands:");
        Console.WriteLine("  /clear   - Clear conversation history");
        Console.WriteLine("  /history - Show conversation history");
        Console.WriteLine("  /exit    - Exit the application");
        Console.WriteLine("  /system  - Set system prompt");
        if (btDocService != null)
        {
            Console.WriteLine("  /search  - Search BuilderTrend documentation");
        }
        Console.ResetColor();
        Console.WriteLine();

        string? systemPrompt = null;

        // Main chat loop
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("You: ");
            Console.ResetColor();
            
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Handle commands
            if (input.StartsWith("/"))
            {
                var command = input.ToLower().Trim();
                
                if (command == "/exit" || command == "/quit")
                {
                    Console.WriteLine();
                    Console.WriteLine("Goodbye! 👋");
                    break;
                }
                else if (command == "/clear")
                {
                    conversationHistory.Clear();
                    systemPrompt = null;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Conversation history cleared.");
                    Console.ResetColor();
                    Console.WriteLine();
                    continue;
                }
                else if (command == "/history")
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("=== Conversation History ===");
                    Console.ResetColor();
                    
                    if (systemPrompt != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"[System]: {systemPrompt}");
                        Console.ResetColor();
                    }
                    
                    if (conversationHistory.Count == 0)
                    {
                        Console.WriteLine("(empty)");
                    }
                    else
                    {
                        foreach (var msg in conversationHistory)
                        {
                            if (msg.Role == "user")
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"You: {msg.Content}");
                            }
                            else if (msg.Role == "assistant")
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"AI: {msg.Content}");
                            }
                            Console.ResetColor();
                        }
                    }
                    Console.WriteLine();
                    continue;
                }
                else if (command == "/system")
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("System Prompt: ");
                    Console.ResetColor();
                    
                    var sysPrompt = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(sysPrompt))
                    {
                        systemPrompt = sysPrompt;
                        conversationHistory.Clear(); // Clear history when changing system prompt
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ System prompt set.");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                    continue;
                }
                else if (command == "/search")
                {
                    if (btDocService == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ BuilderTrend search is not enabled.");
                        Console.ResetColor();
                        Console.WriteLine();
                        continue;
                    }
                    
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("Search Query: ");
                    Console.ResetColor();
                    
                    var searchQuery = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(searchQuery))
                    {
                        Console.WriteLine("Query cannot be empty.");
                        Console.WriteLine();
                        continue;
                    }
                    
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("🔍 Searching BuilderTrend documentation...");
                    Console.WriteLine("   (First search: ~50-90s, Cached: ~1-2s)");
                    Console.ResetColor();
                    Console.WriteLine();
                    
                    try
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var results = await btDocService.SearchDocumentationAsync(searchQuery, useSemanticSearch: true);
                        stopwatch.Stop();
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Results (in {stopwatch.Elapsed.TotalSeconds:F1}s):");
                        Console.ResetColor();
                        Console.WriteLine();
                        
                        Console.WriteLine(results);
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Search error: {ex.Message}");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                    
                    continue;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown command: {command}");
                    Console.ResetColor();
                    Console.WriteLine();
                    continue;
                }
            }

            // Add user message to history
            conversationHistory.Add(new Message { Role = "user", Content = input });

            // Build messages for API call
            var messages = new List<Message>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new Message { Role = "system", Content = systemPrompt });
            }
            messages.AddRange(conversationHistory);

            // Get response from LLM
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("AI: ");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                
                var response = await llmService.ChatWithHistoryAsync(messages);
                
                Console.WriteLine(response);
                Console.ResetColor();
                Console.WriteLine();

                // Add assistant response to history
                conversationHistory.Add(new Message { Role = "assistant", Content = response });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine();
                
                // Remove the user message since we didn't get a response
                conversationHistory.RemoveAt(conversationHistory.Count - 1);
            }
        }
    }

    private static LLMConfig? LoadConfiguration()
    {
        try
        {
            if (!File.Exists("appsettings.json"))
            {
                // Try parent directory (in case running from bin/Debug)
                if (File.Exists("../../../appsettings.json"))
                {
                    var json = File.ReadAllText("../../../appsettings.json");
                    return ParseConfig(json);
                }
                return null;
            }

            var jsonContent = File.ReadAllText("appsettings.json");
            return ParseConfig(jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load configuration: {ex.Message}");
            return null;
        }
    }

    private static LLMConfig? ParseConfig(string json)
    {
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
