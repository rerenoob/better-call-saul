namespace BetterCallSaul.Core.Configuration;

public class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public bool UseAzureStorage { get; set; } = false;
    public int SasTokenExpiryMinutes { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
}