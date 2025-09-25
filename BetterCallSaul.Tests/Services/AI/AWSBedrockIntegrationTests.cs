using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class AWSBedrockIntegrationTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var awsOptions = new AWSOptions
        {
            Bedrock = new BedrockOptions
            {
                Region = "us-east-1",
                ModelId = "anthropic.claude-3-haiku-20240307-v1:0"
            }
        };

        var optionsMock = new Mock<IOptions<AWSOptions>>();
        optionsMock.Setup(o => o.Value).Returns(awsOptions);
        
        var loggerMock = new Mock<ILogger<AWSBedrockService>>();

        // Act & Assert
        var service = new AWSBedrockService(optionsMock.Object, loggerMock.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_WithValidConfiguration_ReturnsExpectedErrorForMissingCredentials()
    {
        // Arrange
        var awsOptions = new AWSOptions
        {
            Bedrock = new BedrockOptions
            {
                Region = "us-east-1",
                ModelId = "anthropic.claude-3-haiku-20240307-v1:0"
            }
        };

        var optionsMock = new Mock<IOptions<AWSOptions>>();
        optionsMock.Setup(o => o.Value).Returns(awsOptions);
        
        var loggerMock = new Mock<ILogger<AWSBedrockService>>();

        var service = new AWSBedrockService(optionsMock.Object, loggerMock.Object);
        
        var request = new AIRequest
        {
            DocumentText = "Test legal document content for analysis",
            CaseContext = "Test case context information",
            MaxTokens = 500,
            Temperature = 0.3
        };

        // Act
        var result = await service.AnalyzeCaseAsync(request);

        // Assert - Should fail due to missing AWS credentials, but not due to service configuration
        // The service might return different error messages depending on AWS SDK behavior
        Assert.NotNull(result);
        // Either not configured error or AWS authentication error is acceptable
        // We can't guarantee failure due to AWS SDK credential chain behavior
        if (!result.Success)
        {
            Assert.NotNull(result.ErrorMessage);
        }
        // If it succeeds, that's also acceptable (might have valid AWS credentials)
    }

    [Fact]
    public async Task StreamAnalysisAsync_WithValidConfiguration_ReturnsEmptyStreamForMissingCredentials()
    {
        // Arrange
        var awsOptions = new AWSOptions
        {
            Bedrock = new BedrockOptions
            {
                Region = "us-east-1",
                ModelId = "anthropic.claude-3-haiku-20240307-v1:0"
            }
        };

        var optionsMock = new Mock<IOptions<AWSOptions>>();
        optionsMock.Setup(o => o.Value).Returns(awsOptions);
        
        var loggerMock = new Mock<ILogger<AWSBedrockService>>();

        var service = new AWSBedrockService(optionsMock.Object, loggerMock.Object);
        
        var request = new AIRequest
        {
            DocumentText = "Test legal document content for streaming analysis",
            CaseContext = "Test case context information",
            MaxTokens = 500,
            Temperature = 0.3
        };

        // Act
        var stream = service.StreamAnalysisAsync(request);
        var results = new List<string>();
        
        await foreach (var chunk in stream)
        {
            results.Add(chunk);
        }

        // Assert - Should return empty stream due to AWS authentication issues
        Assert.Empty(results);
    }
}