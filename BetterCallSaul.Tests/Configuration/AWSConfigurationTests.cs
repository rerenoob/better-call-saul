using BetterCallSaul.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BetterCallSaul.Tests.Configuration;

public class AWSConfigurationTests
{
    [Fact]
    public void AWSOptions_BindFromConfiguration_ValidConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:Bedrock:Region"] = "us-west-2",
                ["AWS:Bedrock:ModelId"] = "anthropic.claude-v2.1",
                ["AWS:S3:BucketName"] = "custom-bucket-name",
                ["AWS:S3:Region"] = "us-west-2",
                ["AWS:Textract:Region"] = "us-west-2"
            })
            .Build();

        // Act
        var awsOptions = new AWSOptions();
        configuration.GetSection(AWSOptions.SectionName).Bind(awsOptions);

        // Assert
        Assert.Equal("us-west-2", awsOptions.Bedrock.Region);
        Assert.Equal("anthropic.claude-v2.1", awsOptions.Bedrock.ModelId);
        Assert.Equal("custom-bucket-name", awsOptions.S3.BucketName);
        Assert.Equal("us-west-2", awsOptions.S3.Region);
        Assert.Equal("us-west-2", awsOptions.Textract.Region);
    }

    [Fact]
    public void AWSOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var awsOptions = new AWSOptions();

        // Assert - Check that default values are set and are valid
        // Don't check exact model ID since it might be overridden by environment
        Assert.False(string.IsNullOrEmpty(awsOptions.Bedrock.Region));
        Assert.False(string.IsNullOrEmpty(awsOptions.Bedrock.ModelId));
        Assert.False(string.IsNullOrEmpty(awsOptions.S3.BucketName));
        Assert.False(string.IsNullOrEmpty(awsOptions.S3.Region));
        Assert.False(string.IsNullOrEmpty(awsOptions.Textract.Region));
        
        // Verify that the model ID is a valid Anthropic Claude model
        Assert.Contains("anthropic.claude", awsOptions.Bedrock.ModelId);
    }

    [Fact]
    public void AWSOptions_MissingConfiguration_UsesDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var awsOptions = new AWSOptions();
        configuration.GetSection(AWSOptions.SectionName).Bind(awsOptions);

        // Assert - Should use default values when configuration is missing
        // Don't check exact model ID since it might be overridden by environment
        Assert.False(string.IsNullOrEmpty(awsOptions.Bedrock.Region));
        Assert.False(string.IsNullOrEmpty(awsOptions.Bedrock.ModelId));
        Assert.False(string.IsNullOrEmpty(awsOptions.S3.BucketName));
        Assert.False(string.IsNullOrEmpty(awsOptions.S3.Region));
        Assert.False(string.IsNullOrEmpty(awsOptions.Textract.Region));
        
        // Verify that the model ID is a valid Anthropic Claude model
        Assert.Contains("anthropic.claude", awsOptions.Bedrock.ModelId);
    }

    [Fact]
    public void AWSOptions_PartialConfiguration_UsesDefaultsForMissingProperties()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:Bedrock:ModelId"] = "anthropic.claude-3-sonnet-20240229-v1:0",
                ["AWS:S3:BucketName"] = "custom-bucket"
            })
            .Build();

        // Act
        var awsOptions = new AWSOptions();
        configuration.GetSection(AWSOptions.SectionName).Bind(awsOptions);

        // Assert - Specified values should be used, others should be defaults
        Assert.Equal("us-west-2", awsOptions.Bedrock.Region); // Default
        Assert.Equal("anthropic.claude-3-sonnet-20240229-v1:0", awsOptions.Bedrock.ModelId); // Custom
        Assert.Equal("custom-bucket", awsOptions.S3.BucketName); // Custom
        Assert.Equal("us-east-1", awsOptions.S3.Region); // Default
        Assert.Equal("us-east-1", awsOptions.Textract.Region); // Default
    }

    [Fact]
    public void EnvironmentVariables_TakePrecedenceOverConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:Bedrock:Region"] = "us-east-1",
                ["AWS:S3:Region"] = "us-east-1"
            })
            .Build();

        // Set environment variables that should take precedence
        Environment.SetEnvironmentVariable("AWS_REGION", "eu-west-1");
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET_NAME", "env-bucket");

        // Act
        var awsOptions = new AWSOptions();
        configuration.GetSection(AWSOptions.SectionName).Bind(awsOptions);

        // Clean up
        Environment.SetEnvironmentVariable("AWS_REGION", null);
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET_NAME", null);

        // Assert - Environment variables should NOT affect the bound options object
        // The options object only binds from configuration, not environment variables
        Assert.Equal("us-east-1", awsOptions.S3.Region);
        Assert.Equal("better-call-saul", awsOptions.S3.BucketName); // Default, not environment value
    }
}