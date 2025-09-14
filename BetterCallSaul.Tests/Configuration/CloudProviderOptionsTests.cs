using BetterCallSaul.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BetterCallSaul.Tests.Configuration;

public class CloudProviderOptionsTests
{
    [Fact]
    public void CloudProviderOptions_LoadsFromConfiguration_WithAWSAsDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudProvider:Active"] = "AWS",
                ["CloudProvider:AWS:Bedrock:Region"] = "us-east-1",
                ["CloudProvider:AWS:S3:BucketName"] = "test-bucket",
                ["CloudProvider:AWS:Textract:Region"] = "us-west-2"
            })
            .Build();

        // Act
        var options = new CloudProviderOptions();
        configuration.GetSection(CloudProviderOptions.SectionName).Bind(options);

        // Assert
        Assert.Equal("AWS", options.Active);
        
        Assert.NotNull(options.AWS);
        Assert.NotNull(options.AWS.Bedrock);
        Assert.Equal("us-east-1", options.AWS.Bedrock.Region);
        Assert.NotNull(options.AWS.S3);
        Assert.Equal("test-bucket", options.AWS.S3.BucketName);
        Assert.NotNull(options.AWS.Textract);
        Assert.Equal("us-west-2", options.AWS.Textract.Region);
    }

    [Fact]
    public void CloudProviderOptions_EnvironmentVariable_OverridesConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CLOUD_PROVIDER", "AWS");
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudProvider:Active"] = "Azure" // This should be overridden
            })
            .Build();

        // Act
        var options = new CloudProviderOptions();
        configuration.GetSection(CloudProviderOptions.SectionName).Bind(options);
        
        // Override from environment variable (simulating Program.cs logic)
        var cloudProvider = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
        if (!string.IsNullOrEmpty(cloudProvider) && (cloudProvider == "Azure" || cloudProvider == "AWS"))
        {
            options.Active = cloudProvider;
        }

        // Assert
        Assert.Equal("AWS", options.Active);
        
        // Cleanup
        Environment.SetEnvironmentVariable("CLOUD_PROVIDER", null);
    }

    [Fact]
    public void CloudProviderOptions_InvalidEnvironmentVariable_DoesNotOverride()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CLOUD_PROVIDER", "GCP"); // Invalid value
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudProvider:Active"] = "AWS"
            })
            .Build();

        // Act
        var options = new CloudProviderOptions();
        configuration.GetSection(CloudProviderOptions.SectionName).Bind(options);
        
        // Override from environment variable (simulating Program.cs logic)
        var cloudProvider = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
        if (!string.IsNullOrEmpty(cloudProvider) && (cloudProvider == "Azure" || cloudProvider == "AWS"))
        {
            options.Active = cloudProvider;
        }

        // Assert - should remain AWS since GCP is invalid
        Assert.Equal("AWS", options.Active);
        
        // Cleanup
        Environment.SetEnvironmentVariable("CLOUD_PROVIDER", null);
    }
}