namespace BetterCallSaul.Core.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? DeploymentName { get; set; }
    public string? Model { get; set; } = "gpt-4";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 2000;
}