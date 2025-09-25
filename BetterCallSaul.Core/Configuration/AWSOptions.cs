namespace BetterCallSaul.Core.Configuration;

public class AWSOptions
{
    public const string SectionName = "AWS";
    
    public BedrockOptions Bedrock { get; set; } = new();
    public S3Options S3 { get; set; } = new();
    public TextractOptions Textract { get; set; } = new();
}

public class BedrockOptions
{
    public string Region { get; set; } = "us-west-2";
    public string ModelId { get; set; } = "anthropic.claude-3-5-sonnet-20241022-v2:0";
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

public class LocalStorageOptions
{
    public string BasePath { get; set; } = string.Empty;
}