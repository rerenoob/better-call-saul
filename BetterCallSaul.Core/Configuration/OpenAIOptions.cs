namespace BetterCallSaul.Core.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    
    public string? Endpoint => Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? EndpointFromConfig;
    public string? ApiKey => Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? ApiKeyFromConfig;
    
    public string? EndpointFromConfig { get; set; }
    public string? ApiKeyFromConfig { get; set; }
    public string? DeploymentName { get; set; }
    public string? Model { get; set; } = "gpt-4";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 2000;
}