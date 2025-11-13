using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer;

public class LLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly LLMProvider _provider;

    public LLMService(LLMProvider provider = LLMProvider.Ollama, string? apiKey = null, string? model = null)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _provider = provider;
        _apiKey = apiKey;

        // Configure based on provider
        (_apiUrl, _model) = provider switch
        {
            LLMProvider.Ollama => ("http://localhost:11434/api/chat", model ?? "llama3.2"),
            LLMProvider.OpenRouter => ("https://openrouter.ai/api/v1/chat/completions", model ?? "meta-llama/llama-3.2-3b-instruct:free"),
            LLMProvider.Groq => ("https://api.groq.com/openai/v1/chat/completions", model ?? "llama-3.1-8b-instant"),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };

        if (provider != LLMProvider.Ollama && string.IsNullOrEmpty(_apiKey))
        {
            throw new ArgumentException($"API key required for provider: {provider}");
        }
    }

    public async Task<string> ChatAsync(string prompt, string? systemPrompt = null)
    {
        var messages = new List<Message>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new Message { Role = "system", Content = systemPrompt });
        }
        
        messages.Add(new Message { Role = "user", Content = prompt });

        return await SendRequestAsync(messages);
    }

    public async Task<string> ChatWithHistoryAsync(List<Message> messages)
    {
        return await SendRequestAsync(messages);
    }

    private async Task<string> SendRequestAsync(List<Message> messages)
    {
        try
        {
            object requestBody;
            
            if (_provider == LLMProvider.Ollama)
            {
                // Ollama format
                requestBody = new
                {
                    model = _model,
                    messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                    stream = false
                };
            }
            else
            {
                // OpenAI-compatible format (OpenRouter, Groq)
                requestBody = new
                {
                    model = _model,
                    messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
                };
            }

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add authorization header if needed
            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            // OpenRouter-specific headers
            if (_provider == LLMProvider.OpenRouter)
            {
                request.Headers.Add("HTTP-Referer", "https://github.com/your-repo");
                request.Headers.Add("X-Title", "MCP Server");
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);

            // Parse response based on provider
            if (_provider == LLMProvider.Ollama)
            {
                var messageContent = responseJson.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return messageContent ?? "";
            }
            else
            {
                // OpenAI-compatible format
                var messageContent = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return messageContent ?? "";
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"LLM API request failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error communicating with LLM: {ex.Message}", ex);
        }
    }
}

public enum LLMProvider
{
    Ollama,      // Local, free, runs on your machine
    OpenRouter,  // Cloud, has free models
    Groq         // Cloud, free tier available
}

public class Message
{
    public string Role { get; set; } = "user"; // "system", "user", or "assistant"
    public string Content { get; set; } = "";
}
