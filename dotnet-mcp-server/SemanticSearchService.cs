using System.Text.Json;

namespace McpServer;

public class SemanticSearchService
{
    private readonly LLMService _llmService;
    private readonly EmbeddingCache _cache;
    private Dictionary<string, float[]>? _articleEmbeddings;
    private List<HelpArticle>? _indexedArticles;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24); // 24 hour cache

    public SemanticSearchService(LLMService llmService)
    {
        _llmService = llmService;
        _cache = new EmbeddingCache();
    }

    /// <summary>
    /// Generate embeddings for all articles
    /// </summary>
    public async Task IndexArticlesAsync(List<HelpArticle> articles)
    {
        // Try to load from cache first
        var (cachedEmbeddings, cachedArticles) = await _cache.LoadAsync(_cacheExpiration);
        
        if (cachedEmbeddings != null && cachedArticles != null)
        {
            // Check if articles match (same URLs)
            var cachedUrls = cachedArticles.Select(a => a.Url).ToHashSet();
            var currentUrls = articles.Select(a => a.Url).ToHashSet();
            
            if (cachedUrls.SetEquals(currentUrls))
            {
                Console.Error.WriteLine($"[Semantic] Using cached embeddings for {cachedArticles.Count} articles");
                _articleEmbeddings = cachedEmbeddings;
                _indexedArticles = cachedArticles;
                return;
            }
            else
            {
                Console.Error.WriteLine($"[Semantic] Cache invalid - article list changed (cached: {cachedArticles.Count}, current: {articles.Count})");
            }
        }

        // No valid cache, generate embeddings
        _articleEmbeddings = new Dictionary<string, float[]>();
        _indexedArticles = articles;

        Console.Error.WriteLine($"[Semantic] Indexing {articles.Count} articles...");

        foreach (var article in articles)
        {
            try
            {
                // Create text to embed - use full content if available, otherwise title + description
                string textToEmbed;
                if (!string.IsNullOrEmpty(article.FullContent))
                {
                    // Use full content for better semantic understanding
                    textToEmbed = $"{article.Title}. {article.FullContent}";
                    
                    // Limit to 2000 chars for embedding (to avoid overwhelming the LLM)
                    if (textToEmbed.Length > 2000)
                    {
                        textToEmbed = textToEmbed.Substring(0, 2000);
                    }
                }
                else
                {
                    // Fallback to title + description
                    textToEmbed = $"{article.Title}. {article.Description}";
                }
                
                var embedding = await GenerateEmbeddingAsync(textToEmbed);
                
                if (embedding != null)
                {
                    _articleEmbeddings[article.Url] = embedding;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Semantic] Error embedding article '{article.Title}': {ex.Message}");
            }
        }

        Console.Error.WriteLine($"[Semantic] Indexed {_articleEmbeddings.Count} articles with embeddings");

        // Save to cache
        await _cache.SaveAsync(_articleEmbeddings, _indexedArticles);
    }

    /// <summary>
    /// Perform semantic search using embeddings
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
    {
        if (_articleEmbeddings == null || _indexedArticles == null)
        {
            throw new InvalidOperationException("Articles not indexed. Call IndexArticlesAsync first.");
        }

        // Generate embedding for query
        var queryEmbedding = await GenerateEmbeddingAsync(query);
        if (queryEmbedding == null)
        {
            return new List<SearchResult>();
        }

        // Calculate similarity scores
        var results = new List<SearchResult>();

        foreach (var article in _indexedArticles)
        {
            if (_articleEmbeddings.TryGetValue(article.Url, out var articleEmbedding))
            {
                var similarity = CosineSimilarity(queryEmbedding, articleEmbedding);
                
                // Convert similarity to 0-10 scale for display
                var score = (similarity + 1) * 5; // Maps -1..1 to 0..10

                results.Add(new SearchResult
                {
                    Title = article.Title,
                    Url = article.Url,
                    Score = score,
                    Snippet = article.Description.Length > 200 
                        ? article.Description.Substring(0, 200) + "..." 
                        : article.Description
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Generate embedding using the LLM service (via prompt-based approach)
    /// This is a simple approach - for production, use a dedicated embedding model
    /// </summary>
    private async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        try
        {
            // Truncate very long text
            if (text.Length > 500)
            {
                text = text.Substring(0, 500);
            }

            // Use LLM to generate a simple semantic representation
            // For better results, you could use OpenAI's embedding API or a local embedding model
            var prompt = $"Extract 5 key semantic concepts from this text as comma-separated words: {text}";
            
            var response = await _llmService.ChatAsync(
                prompt,
                "You are a text analysis assistant. Extract only the key concepts as comma-separated words, nothing else."
            );

            // Convert concepts to a simple embedding vector
            return ConceptsToEmbedding(response);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Semantic] Error generating embedding: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Convert concept words to a simple embedding vector
    /// </summary>
    private float[] ConceptsToEmbedding(string concepts)
    {
        // Simple hash-based embedding (for demonstration)
        // In production, use a proper embedding model
        var words = concepts.ToLower()
            .Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(10)
            .ToArray();

        var embedding = new float[128]; // 128-dimensional vector

        foreach (var word in words)
        {
            var hash = word.GetHashCode();
            for (int i = 0; i < embedding.Length; i++)
            {
                // Use hash to populate embedding dimensions
                embedding[i] += (float)Math.Sin((hash + i) * 0.1);
            }
        }

        // Normalize
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }

        return embedding;
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Get cache information
    /// </summary>
    public async Task<CacheInfo?> GetCacheInfoAsync()
    {
        return await _cache.GetInfoAsync();
    }

    /// <summary>
    /// Clear the embedding cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _articleEmbeddings = null;
        _indexedArticles = null;
    }
}
