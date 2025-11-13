# 🔍 Semantic Search for BuilderTrend Documentation

Your MCP server now includes **semantic search** capabilities for BuilderTrend help articles! This provides much better search results based on meaning rather than just keyword matching.

## ✨ What is Semantic Search?

Semantic search understands the *meaning* of your query, not just matching keywords. For example:

### Keyword Search (Old):
- Query: "create invoice"
- Matches: Articles with exact words "create" AND "invoice"
- Misses: Articles about "generating bills" or "billing clients"

### Semantic Search (New):
- Query: "create invoice"
- Matches: Articles about invoicing, billing, payment requests, generating bills, etc.
- Understands: Related concepts and synonyms

## 🚀 How It Works

1. **First Use**: When you first search, the system:
   - Crawls BuilderTrend help articles
   - Uses your LLM to extract semantic concepts from each article
   - Builds an embedding index (cached for 1 hour)

2. **Subsequent Searches**: 
   - Converts your query to semantic embeddings
   - Finds articles with similar meanings using cosine similarity
   - Returns the most relevant results ranked by relevance

3. **Fallback**: If semantic search fails or isn't available, it falls back to keyword search

## 📊 Comparison

| Feature | Keyword Search | Semantic Search |
|---------|---------------|-----------------|
| Speed | Fast | Slower first time (indexing) |
| Accuracy | Good for exact matches | Better for concepts |
| Synonyms | ❌ Misses | ✅ Finds |
| Related Topics | ❌ Misses | ✅ Finds |
| Setup | None | Requires LLM |

## 🎯 Usage

### Via MCP Server

The `bt_documentation` tool now uses semantic search automatically when LLM is configured:

```json
{
  "name": "bt_documentation",
  "arguments": {
    "query": "how to bill a customer"
  }
}
```

**Response includes search method:**
```
Found 5 relevant article(s) for 'how to bill a customer' (using semantic search):

1. Creating Invoices
   URL: https://buildertrend.com/help-articles/invoices
   Relevance Score: 8.45
   Summary: Learn how to create and send invoices to clients...
```

### Disabling Semantic Search

If you want to use keyword search only, you can modify the code to pass `useSemanticSearch: false`:

```csharp
await _btDocService.SearchDocumentationAsync(query, useSemanticSearch: false);
```

## ⚙️ Configuration

Semantic search requires an LLM to be configured. It works with any of your providers:

```json
{
  "LLM": {
    "Provider": "Ollama",  // or "OpenRouter", "Groq"
    "ApiKey": "",
    "Model": "llama3.2"
  }
}
```

### Recommended Setup

- **Ollama**: Best choice - unlimited local requests for indexing
- **OpenRouter/Groq**: Works but may hit rate limits during indexing

## 📈 Performance

### First Search (with indexing):
- Small site (~50 articles): 30-60 seconds
- Medium site (~100 articles): 1-3 minutes
- Large site (~500 articles): 5-15 minutes
- **Cache saved to disk** - subsequent runs load instantly!

### Subsequent Searches:
- Near instant (<2 seconds) loading from cache
- Index cached for 24 hours
- No re-indexing needed after restart

### Performance Improvement:
- **Without cache**: Every restart requires 40-60 second indexing
- **With cache**: Loads in ~2 seconds after first run
- **24x faster** with persistent caching!

📚 **[See Cache Documentation](CACHE.md)** for details on persistent storage.

## 🔧 Technical Details

### Embedding Generation
1. For each article, extracts title + description
2. Uses LLM to identify 5 key semantic concepts
3. Converts concepts to 128-dimensional embedding vector
4. Normalizes vector for cosine similarity

### Search Process
1. Convert query to embedding vector
2. Calculate cosine similarity with all article embeddings
3. Rank by similarity score
4. Return top 5 results

### Caching Strategy
- Articles cached for 1 hour
- Embeddings cached with articles
- Automatic re-indexing when cache expires

## 💡 Example Queries

### Good Queries for Semantic Search

✅ **Conceptual**: "how to manage project costs"
- Finds: budgeting, expenses, cost tracking, change orders

✅ **Action-based**: "send invoice to client"
- Finds: invoicing, billing, payment requests, statements

✅ **Problem-solving**: "client can't see their schedule"
- Finds: permissions, access, client portal, sharing

### Less Effective Queries

⚠️ **Too specific**: "click the blue button in top right"
- Better: Use keyword search for UI-specific queries

⚠️ **Single words**: "invoice"
- Better: "how to create an invoice"

## 🐛 Troubleshooting

### "Semantic search failed, falling back to keyword search"

**Causes:**
1. LLM not configured
2. LLM rate limits exceeded
3. Network issues with cloud LLM

**Solutions:**
- Check `appsettings.json` configuration
- Use Ollama for unlimited local processing
- Wait a few minutes and try again (for cloud providers)

### Slow First Search

This is normal! The system is:
- Crawling all help articles
- Generating embeddings for each article
- Building the search index

**Solution:** Be patient on first search. Subsequent searches will be fast!

### "Articles not indexed"

**Cause:** LLM service not available during indexing

**Solution:**
- Ensure LLM is running (Ollama) or API key is valid
- Check logs for specific errors
- Restart MCP server after fixing LLM configuration

## 📝 Logs

Monitor semantic search activity in stderr:

```
[Semantic] Indexing 47 articles...
[Semantic] Indexed 47 articles with embeddings
[BTDoc] Using semantic search...
[BTDoc] Semantic search found 5 results
```

## 🎯 Best Practices

1. **Use Ollama**: For unlimited embedding generation during indexing
2. **Natural Language**: Write queries as questions or descriptions
3. **Be Specific**: More context = better results
4. **Check Search Method**: Response indicates if semantic or keyword search was used
5. **Monitor Logs**: Check stderr for indexing progress

## 🔮 Future Enhancements

Potential improvements:
- Use dedicated embedding models (sentence-transformers)
- Persistent embedding cache across restarts
- Incremental indexing (only new articles)
- Hybrid search (combine semantic + keyword scores)
- User feedback to improve relevance

---

**Your BuilderTrend documentation searches are now smarter!** 🎉
