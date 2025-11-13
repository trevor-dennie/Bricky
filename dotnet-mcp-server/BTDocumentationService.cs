using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace McpServer;

public class BTDocumentationService
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _baseUrl = "https://buildertrend.com/help-articles/";
    private List<HelpArticle>? _cachedArticles;
    private DateTime? _cacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
    private readonly LLMService? _llmService;
    private SemanticSearchService? _semanticSearch;
    private bool _isSemanticIndexed = false;

    public BTDocumentationService(LLMService? llmService = null)
    {
        _llmService = llmService;
        if (_llmService != null)
        {
            _semanticSearch = new SemanticSearchService(_llmService);
        }
    }

    public async Task<string> SearchDocumentationAsync(string query, bool useSemanticSearch = true)
    {
        try
        {
            // Get or refresh articles cache
            var articles = await GetArticlesAsync();
            
            if (articles.Count == 0)
            {
                return "No articles found. The BuilderTrend help site may be unavailable.";
            }

            List<SearchResult> results;

            // Try semantic search first if enabled and available
            if (useSemanticSearch && _semanticSearch != null && _llmService != null)
            {
                try
                {
                    // Index articles if not already done
                    if (!_isSemanticIndexed)
                    {
                        await LogAsync("Performing first-time semantic indexing...");
                        await _semanticSearch.IndexArticlesAsync(articles);
                        _isSemanticIndexed = true;
                        await LogAsync("Semantic indexing complete!");
                    }

                    await LogAsync("Using semantic search...");
                    results = await _semanticSearch.SearchAsync(query, 5);
                    
                    if (results.Count > 0)
                    {
                        await LogAsync($"Semantic search found {results.Count} results");
                    }
                    else
                    {
                        await LogAsync("Semantic search found no results, falling back to keyword search");
                        results = SearchArticles(articles, query);
                    }
                }
                catch (Exception ex)
                {
                    await LogAsync($"Semantic search failed: {ex.Message}, falling back to keyword search");
                    results = SearchArticles(articles, query);
                }
            }
            else
            {
                await LogAsync("Using keyword search...");
                results = SearchArticles(articles, query);
            }

            // Search articles for relevant content
            // var results = SearchArticles(articles, query);

            if (results.Count == 0)
            {
                return $"No relevant articles found for query: '{query}'\n\nTry rephrasing your question or using different keywords.";
            }

            // Format results
            var sb = new StringBuilder();
            var searchMethod = (useSemanticSearch && _semanticSearch != null && _isSemanticIndexed) 
                ? "semantic" 
                : "keyword";
            sb.AppendLine($"Found {results.Count} relevant article(s) for '{query}' (using {searchMethod} search):\n");

            int count = 1;
            foreach (var result in results.Take(5)) // Limit to top 5 results
            {
                sb.AppendLine($"{count}. {result.Title}");
                sb.AppendLine($"   URL: {result.Url}");
                sb.AppendLine($"   Relevance Score: {result.Score:F2}");
                if (!string.IsNullOrEmpty(result.Snippet))
                {
                    sb.AppendLine($"   Summary: {result.Snippet}");
                }
                sb.AppendLine();
                count++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching documentation: {ex.Message}";
        }
    }

    private async Task<List<HelpArticle>> GetArticlesAsync()
    {
        // Check cache
        if (_cachedArticles != null && _cacheTime.HasValue && 
            DateTime.Now - _cacheTime.Value < _cacheExpiration)
        {
            return _cachedArticles;
        }

        // Crawl the help articles page
        var articles = new List<HelpArticle>();

        try
        {
            var html = await _httpClient.GetStringAsync(_baseUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Look for article links - adjust selectors based on actual site structure
            var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            
            if (linkNodes != null)
            {
                foreach (var link in linkNodes)
                {
                    var href = link.GetAttributeValue("href", "");
                    var text = CleanText(link.InnerText);

                    // Filter for help article links
                    if (IsHelpArticleLink(href) && !string.IsNullOrWhiteSpace(text))
                    {
                        var fullUrl = href.StartsWith("http") ? href : new Uri(new Uri(_baseUrl), href).ToString();
                        
                        articles.Add(new HelpArticle
                        {
                            Title = text,
                            Url = fullUrl,
                            Description = text
                        });
                    }
                }
            }

            // Try to get more detailed information from article list elements
            var articleElements = doc.DocumentNode.SelectNodes("//article") ?? 
                                 doc.DocumentNode.SelectNodes("//div[contains(@class, 'article')]") ??
                                 doc.DocumentNode.SelectNodes("//div[contains(@class, 'help')]");

            if (articleElements != null)
            {
                foreach (var article in articleElements)
                {
                    var titleNode = article.SelectSingleNode(".//h2 | .//h3 | .//a");
                    var linkNode = article.SelectSingleNode(".//a[@href]");
                    var descNode = article.SelectSingleNode(".//p | .//div[contains(@class, 'description')]");

                    if (titleNode != null && linkNode != null)
                    {
                        var title = CleanText(titleNode.InnerText);
                        var url = linkNode.GetAttributeValue("href", "");
                        var desc = descNode != null ? CleanText(descNode.InnerText) : "";

                        if (!string.IsNullOrWhiteSpace(title) && IsHelpArticleLink(url))
                        {
                            var fullUrl = url.StartsWith("http") ? url : new Uri(new Uri(_baseUrl), url).ToString();
                            
                            // Add or update article
                            var existing = articles.FirstOrDefault(a => a.Url == fullUrl);
                            if (existing != null)
                            {
                                existing.Description = desc;
                            }
                            else
                            {
                                articles.Add(new HelpArticle
                                {
                                    Title = title,
                                    Url = fullUrl,
                                    Description = desc
                                });
                            }
                        }
                    }
                }
            }

            // Remove duplicates
            articles = articles
                .GroupBy(a => a.Url)
                .Select(g => g.First())
                .ToList();

            await LogAsync($"Crawled {articles.Count} articles from BuilderTrend help site");

            // Fetch full content for each article (for semantic search)
            if (_llmService != null)
            {
                await LogAsync("Fetching full content for articles...");
                await FetchArticleContentAsync(articles);
                await LogAsync("Article content fetching complete");
            }

            // Cache results
            _cachedArticles = articles;
            _cacheTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await LogAsync($"Error crawling articles: {ex.Message}");
            
            // Return cached articles if available, even if expired
            if (_cachedArticles != null)
            {
                return _cachedArticles;
            }
        }

        return articles;
    }

    private List<SearchResult> SearchArticles(List<HelpArticle> articles, string query)
    {
        var results = new List<SearchResult>();
        var queryTerms = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var article in articles)
        {
            var score = 0.0;
            var titleLower = article.Title.ToLower();
            var descLower = article.Description.ToLower();
            var matchedSnippets = new List<string>();

            foreach (var term in queryTerms)
            {
                // Title matches are weighted more heavily
                if (titleLower.Contains(term))
                {
                    score += 3.0;
                }

                // Description matches
                if (descLower.Contains(term))
                {
                    score += 1.0;
                    
                    // Extract snippet around the match
                    var index = descLower.IndexOf(term);
                    if (index >= 0)
                    {
                        var start = Math.Max(0, index - 50);
                        var length = Math.Min(150, article.Description.Length - start);
                        var snippet = article.Description.Substring(start, length).Trim();
                        if (start > 0) snippet = "..." + snippet;
                        if (start + length < article.Description.Length) snippet += "...";
                        matchedSnippets.Add(snippet);
                    }
                }

                // Exact phrase match bonus
                if (titleLower.Contains(query.ToLower()))
                {
                    score += 5.0;
                }
            }

            if (score > 0)
            {
                results.Add(new SearchResult
                {
                    Title = article.Title,
                    Url = article.Url,
                    Score = score,
                    Snippet = matchedSnippets.FirstOrDefault() ?? article.Description.Substring(0, Math.Min(150, article.Description.Length))
                });
            }
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    private async Task FetchArticleContentAsync(List<HelpArticle> articles)
    {
        int fetchedCount = 0;
        int totalArticles = articles.Count;

        foreach (var article in articles)
        {
            try
            {
                fetchedCount++;
                if (fetchedCount % 10 == 0)
                {
                    await LogAsync($"Fetching content... {fetchedCount}/{totalArticles}");
                }

                var html = await _httpClient.GetStringAsync(article.Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors to find article content
                var contentNode = 
                    doc.DocumentNode.SelectSingleNode("//article") ??
                    doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-content')]") ??
                    doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'content')]") ??
                    doc.DocumentNode.SelectSingleNode("//main") ??
                    doc.DocumentNode.SelectSingleNode("//div[@id='content']") ??
                    doc.DocumentNode.SelectSingleNode("//body");

                if (contentNode != null)
                {
                    // Extract text content, removing scripts and styles
                    foreach (var script in contentNode.SelectNodes(".//script") ?? new HtmlNodeCollection(null))
                    {
                        script.Remove();
                    }
                    foreach (var style in contentNode.SelectNodes(".//style") ?? new HtmlNodeCollection(null))
                    {
                        style.Remove();
                    }

                    var fullContent = CleanText(contentNode.InnerText);
                    
                    // Store full content, but limit to reasonable size
                    if (fullContent.Length > 10000)
                    {
                        article.FullContent = fullContent.Substring(0, 10000) + "...";
                    }
                    else
                    {
                        article.FullContent = fullContent;
                    }

                    // Update description if it's just the title
                    if (string.IsNullOrEmpty(article.Description) || article.Description == article.Title)
                    {
                        // Use first paragraph or first 300 chars as description
                        var firstPara = fullContent.Length > 300 
                            ? fullContent.Substring(0, 300) + "..." 
                            : fullContent;
                        article.Description = firstPara;
                    }
                }
                
                // Small delay to be nice to the server
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                await LogAsync($"Error fetching content for {article.Url}: {ex.Message}");
                // Continue with other articles
            }
        }

        await LogAsync($"Fetched full content for {fetchedCount} articles");
    }

    private bool IsHelpArticleLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return false;
        
        return href.Contains("/help-articles/") || 
               href.Contains("/help/") ||
               (href.StartsWith("/") && !href.Contains("javascript:") && !href.Contains("#"));
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        
        text = HtmlEntity.DeEntitize(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private async Task LogAsync(string message)
    {
        await Console.Error.WriteLineAsync($"[BTDoc] {message}");
    }
}

public class HelpArticle
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public string FullContent { get; set; } = "";
}

public class SearchResult
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public double Score { get; set; }
    public string Snippet { get; set; } = "";
}
