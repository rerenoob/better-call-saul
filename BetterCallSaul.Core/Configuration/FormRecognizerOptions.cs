namespace BetterCallSaul.Core.Configuration;

public class FormRecognizerOptions
{
    public const string SectionName = "AzureFormRecognizer";
    
    public string? Endpoint => Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_ENDPOINT") ?? EndpointFromConfig;
    public string? ApiKey => Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_API_KEY") ?? ApiKeyFromConfig;
    
    public string? EndpointFromConfig { get; set; }
    public string? ApiKeyFromConfig { get; set; }
    public string? ModelId { get; set; } = "prebuilt-document";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 60;
    public double ConfidenceThreshold { get; set; } = 0.7;
    public bool IncludeFieldElements { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 3000;
    public int MaxPollingTimeMs { get; set; } = 120000;
}