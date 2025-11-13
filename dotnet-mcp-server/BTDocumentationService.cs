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

    public async Task<string> SearchDocumentationAsync(string query)
    {
        try
        {
            // Get or refresh articles cache
            var articles = await GetArticlesAsync();
            
            if (articles.Count == 0)
            {
                return "No articles found. The BuilderTrend help site may be unavailable.";
            }

            // Search articles for relevant content
            var results = SearchArticles(articles, query);

            if (results.Count == 0)
            {
                return $"No relevant articles found for query: '{query}'\n\nTry rephrasing your question or using different keywords.";
            }

            // Format results
            var sb = new StringBuilder();
            sb.AppendLine($"Found {results.Count} relevant article(s) for '{query}':\n");

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

            // Cache results
            _cachedArticles = articles;
            _cacheTime = DateTime.Now;

            await LogAsync($"Crawled {articles.Count} articles from BuilderTrend help site");
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
}

public class SearchResult
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public double Score { get; set; }
    public string Snippet { get; set; } = "";
}
