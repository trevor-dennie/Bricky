using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace McpServer;

public class EmbeddingCache
{
    private readonly string _cacheFilePath;
    private const int CACHE_VERSION = 2; // Incremented for FullContent support

    public EmbeddingCache(string cacheDirectory = "cache")
    {
        // Create cache directory if it doesn't exist
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
        
        _cacheFilePath = Path.Combine(cacheDirectory, "embeddings_cache.json");
    }

    /// <summary>
    /// Save embeddings to disk
    /// </summary>
    public async Task SaveAsync(Dictionary<string, float[]> embeddings, List<HelpArticle> articles)
    {
        try
        {
            var cache = new CacheData
            {
                Version = CACHE_VERSION,
                Timestamp = DateTime.UtcNow,
                ArticleCount = articles.Count,
                Embeddings = embeddings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                ),
                Articles = articles
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(_cacheFilePath, json);
            
            Console.Error.WriteLine($"[Cache] Saved {embeddings.Count} embeddings to {_cacheFilePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Cache] Error saving cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Load embeddings from disk
    /// </summary>
    public async Task<(Dictionary<string, float[]>? embeddings, List<HelpArticle>? articles)> LoadAsync(TimeSpan maxAge)
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                Console.Error.WriteLine("[Cache] No cache file found");
                return (null, null);
            }

            var json = await File.ReadAllTextAsync(_cacheFilePath);
            var cache = JsonSerializer.Deserialize<CacheData>(json);

            if (cache == null)
            {
                Console.Error.WriteLine("[Cache] Failed to deserialize cache");
                return (null, null);
            }

            // Check version
            if (cache.Version != CACHE_VERSION)
            {
                Console.Error.WriteLine($"[Cache] Cache version mismatch (found {cache.Version}, expected {CACHE_VERSION})");
                return (null, null);
            }

            // Check age
            var age = DateTime.UtcNow - cache.Timestamp;
            if (age > maxAge)
            {
                Console.Error.WriteLine($"[Cache] Cache expired (age: {age.TotalHours:F1} hours, max: {maxAge.TotalHours:F1} hours)");
                return (null, null);
            }

            // Convert back to float arrays
            var embeddings = cache.Embeddings.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray()
            );

            Console.Error.WriteLine($"[Cache] Loaded {embeddings.Count} embeddings from cache (age: {age.TotalHours:F1} hours)");
            
            return (embeddings, cache.Articles);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Cache] Error loading cache: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Clear the cache file
    /// </summary>
    public void Clear()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
                Console.Error.WriteLine("[Cache] Cache cleared");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Cache] Error clearing cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get cache info
    /// </summary>
    public async Task<CacheInfo?> GetInfoAsync()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(_cacheFilePath);
            var json = await File.ReadAllTextAsync(_cacheFilePath);
            var cache = JsonSerializer.Deserialize<CacheData>(json);

            if (cache == null)
            {
                return null;
            }

            return new CacheInfo
            {
                Exists = true,
                FilePath = _cacheFilePath,
                FileSize = fileInfo.Length,
                Version = cache.Version,
                Timestamp = cache.Timestamp,
                Age = DateTime.UtcNow - cache.Timestamp,
                ArticleCount = cache.ArticleCount,
                EmbeddingCount = cache.Embeddings.Count
            };
        }
        catch
        {
            return null;
        }
    }

    private class CacheData
    {
        public int Version { get; set; }
        public DateTime Timestamp { get; set; }
        public int ArticleCount { get; set; }
        public Dictionary<string, List<float>> Embeddings { get; set; } = new();
        public List<HelpArticle> Articles { get; set; } = new();
    }
}

public class CacheInfo
{
    public bool Exists { get; set; }
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Age { get; set; }
    public int ArticleCount { get; set; }
    public int EmbeddingCount { get; set; }

    public string GetSummary()
    {
        if (!Exists)
        {
            return "No cache file exists";
        }

        var sizeMB = FileSize / (1024.0 * 1024.0);
        return $"Cache: {EmbeddingCount} embeddings, {ArticleCount} articles, {Age.TotalHours:F1}h old, {sizeMB:F2} MB";
    }
}
