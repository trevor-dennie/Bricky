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
            Console.WriteLine("  /reindex - Force re-index of documentation (clears cache)");
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
                        var searchResults = await btDocService.SearchDocumentationResultsAsync(searchQuery, useSemanticSearch: true);
                        stopwatch.Stop();
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Search completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
                        Console.ResetColor();
                        Console.WriteLine();
                        
                        if (searchResults.Count == 0)
                        {
                            Console.WriteLine("No relevant articles found.");
                            Console.WriteLine();
                            continue;
                        }
                        
                        // Show raw results
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("📚 Top Articles Found:");
                        Console.ResetColor();
                        int count = 1;
                        foreach (var result in searchResults.Take(5))
                        {
                            Console.WriteLine($"{count}. {result.Title}");
                            Console.WriteLine($"   URL: {result.Url}");
                            Console.WriteLine($"   Relevance Score: {result.Score:F2}");
                            if (!string.IsNullOrEmpty(result.Snippet))
                            {
                                Console.WriteLine($"   Summary: {result.Snippet}");
                            }
                            Console.WriteLine();
                            count++;
                        }
                        
                        // Generate AI response based on search results with FULL content
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("💬 Generating AI response...");
                        Console.ResetColor();
                        Console.WriteLine();
                        
                        // Build prompt with full article content
                        var promptBuilder = new System.Text.StringBuilder();
                        promptBuilder.AppendLine($@"Based on the following BuilderTrend documentation articles, please provide a helpful, conversational answer to the user's question: ""{searchQuery}""");
                        promptBuilder.AppendLine();
                        promptBuilder.AppendLine("Here are the most relevant articles with their full content:");
                        promptBuilder.AppendLine();
                        
                        int articleNum = 1;
                        foreach (var result in searchResults.Take(5))
                        {
                            promptBuilder.AppendLine($"=== Article {articleNum}: {result.Title} ===");
                            promptBuilder.AppendLine($"URL: {result.Url}");
                            promptBuilder.AppendLine();
                            if (!string.IsNullOrEmpty(result.FullContent))
                            {
                                // Limit content to prevent prompt overflow
                                var content = result.FullContent.Length > 15000 
                                    ? result.FullContent.Substring(0, 15000) + "... [content truncated]"
                                    : result.FullContent;
                                promptBuilder.AppendLine(content);
                            }
                            else if (!string.IsNullOrEmpty(result.Snippet))
                            {
                                promptBuilder.AppendLine(result.Snippet);
                            }
                            promptBuilder.AppendLine();
                            promptBuilder.AppendLine("---");
                            promptBuilder.AppendLine();
                            articleNum++;
                        }
                        
                        promptBuilder.AppendLine("Please provide a clear, practical answer that synthesizes the information from these articles. If the articles don't fully answer the question, mention what information is available and suggest what the user might want to explore further.");
                        
                        var aiPrompt = promptBuilder.ToString();
                        var aiResponse = await llmService.ChatAsync(aiPrompt, systemPrompt);
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("AI: ");
                        Console.ResetColor();
                        Console.WriteLine(aiResponse);
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
                else if (command == "/reindex")
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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  This will delete the cache and re-fetch all article content.");
                    Console.Write("Are you sure? (y/N): ");
                    Console.ResetColor();
                    
                    var confirm = Console.ReadLine()?.Trim().ToLower();
                    if (confirm != "y" && confirm != "yes")
                    {
                        Console.WriteLine("Cancelled.");
                        Console.WriteLine();
                        continue;
                    }
                    
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("🔄 Clearing cache and re-indexing...");
                    Console.ResetColor();
                    
                    try
                    {
                        // Delete cache file
                        var cacheFile = Path.Combine("cache", "embeddings_cache.json");
                        if (File.Exists(cacheFile))
                        {
                            File.Delete(cacheFile);
                            Console.WriteLine("✅ Cache deleted");
                        }
                        
                        // Force re-initialization by creating new service
                        btDocService = new BTDocumentationService(llmService);
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Re-indexing will occur on next search");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Re-index error: {ex.Message}");
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
