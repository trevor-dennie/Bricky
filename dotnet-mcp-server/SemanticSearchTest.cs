using System;
using System.Threading.Tasks;
using McpServer;

// Simple test program to verify semantic search works

class SemanticSearchTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   Semantic Search Test Program         ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        // Load LLM config
        var config = LoadConfiguration();
        if (config == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Failed to load configuration");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"📋 Config: {config.Provider} / {config.Model ?? "(default)"}");
        Console.WriteLine();

        // Initialize services
        LLMService? llmService = null;
        try
        {
            llmService = new LLMService(config.Provider, config.ApiKey, config.Model);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ LLM Service initialized");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Failed to initialize LLM: {ex.Message}");
            Console.ResetColor();
            return;
        }

        var btDocService = new BTDocumentationService(llmService);
        Console.WriteLine("✅ BTDocumentationService initialized");
        Console.WriteLine();

        // Test query
        var testQuery = args.Length > 0 ? string.Join(" ", args) : "how to create an invoice";
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"🔍 Test Query: \"{testQuery}\"");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⏳ Searching... (first search will index articles - may take 1-3 minutes)");
        Console.ResetColor();
        Console.WriteLine();

        try
        {
            var startTime = DateTime.Now;
            var result = await btDocService.SearchDocumentationAsync(testQuery, useSemanticSearch: true);
            var elapsed = DateTime.Now - startTime;

            Console.WriteLine("═══════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ RESULTS:");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine(result);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"⏱️  Search completed in {elapsed.TotalSeconds:F1} seconds");
            Console.ResetColor();
            Console.WriteLine();

            // Second search to test caching
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🔍 Testing cached search (should be instant)...");
            Console.ResetColor();
            Console.WriteLine();

            startTime = DateTime.Now;
            result = await btDocService.SearchDocumentationAsync("project schedule management", useSemanticSearch: true);
            elapsed = DateTime.Now - startTime;

            Console.WriteLine("═══════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ CACHED SEARCH RESULTS:");
            Console.ResetColor();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine(result);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"⏱️  Cached search completed in {elapsed.TotalSeconds:F1} seconds");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
        }
    }

    private static LLMConfig? LoadConfiguration()
    {
        try
        {
            string jsonPath = "appsettings.json";
            if (!File.Exists(jsonPath))
            {
                jsonPath = "../../../appsettings.json";
                if (!File.Exists(jsonPath))
                {
                    return null;
                }
            }

            var json = File.ReadAllText(jsonPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
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
        catch
        {
            return null;
        }
    }
}
