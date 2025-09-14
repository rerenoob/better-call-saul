namespace BetterCallSaul.Core.Configuration;

public class CloudProviderOptions
{
    public const string SectionName = "CloudProvider";
    
    public string Active { get; set; } = "Azure";
    public AzureOptions Azure { get; set; } = new();
    public AWSOptions AWS { get; set; } = new();
}

public class AzureOptions
{
    public OpenAIOptions OpenAI { get; set; } = new();
    public AzureBlobStorageOptions BlobStorage { get; set; } = new();
    public FormRecognizerOptions FormRecognizer { get; set; } = new();
}

public class AWSOptions
{
    public BedrockOptions Bedrock { get; set; } = new();
    public S3Options S3 { get; set; } = new();
    public TextractOptions Textract { get; set; } = new();
}

public class BedrockOptions
{
    public string Region { get; set; } = "us-east-1";
    public string ModelId { get; set; } = "anthropic.claude-v2";
}

public class S3Options
{
    public string BucketName { get; set; } = "better-call-saul";
    public string Region { get; set; } = "us-east-1";
}

public class TextractOptions
{
    public string Region { get; set; } = "us-east-1";
}