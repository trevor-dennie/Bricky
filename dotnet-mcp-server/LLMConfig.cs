namespace McpServer;

public class LLMConfig
{
    public LLMProvider Provider { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}
