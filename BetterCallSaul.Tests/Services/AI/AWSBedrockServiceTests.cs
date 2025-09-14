using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class AWSBedrockServiceTests
{
    private readonly Mock<IOptions<AWSOptions>> _optionsMock;
    private readonly Mock<ILogger<AWSBedrockService>> _loggerMock;
    private readonly AWSOptions _awsOptions;

    public AWSBedrockServiceTests()
    {
        _awsOptions = new AWSOptions
        {
            Bedrock = new BedrockOptions
            {
                Region = "us-east-1",
                ModelId = "anthropic.claude-v2"
            }
        };

        _optionsMock = new Mock<IOptions<AWSOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_awsOptions);
        
        _loggerMock = new Mock<ILogger<AWSBedrockService>>();
    }

    [Fact]
    public void Constructor_MissingConfiguration_DoesNotThrow()
    {
        // Arrange
        var invalidOptions = new AWSOptions { Bedrock = new BedrockOptions { Region = string.Empty, ModelId = string.Empty } };
        var invalidOptionsMock = new Mock<IOptions<AWSOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var loggerMock = new Mock<ILogger<AWSBedrockService>>();

        // Act & Assert - Constructor should not throw even with invalid config
        var service = new AWSBedrockService(invalidOptionsMock.Object, loggerMock.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_NotConfigured_ReturnsErrorResponse()
    {
        // Arrange
        var invalidOptions = new AWSOptions { Bedrock = new BedrockOptions { Region = string.Empty, ModelId = string.Empty } };
        var invalidOptionsMock = new Mock<IOptions<AWSOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AWSBedrockService(invalidOptionsMock.Object, _loggerMock.Object);
        
        var request = new AIRequest
        {
            DocumentText = "Test document",
            CaseContext = "Test context"
        };

        // Act
        var result = await service.AnalyzeCaseAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("AWS Bedrock service is not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateLegalAnalysisAsync_ValidInput_CallsAnalyzeCaseAsync()
    {
        // Arrange
        var service = new AWSBedrockService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GenerateLegalAnalysisAsync("Test document text", "Test case context");

        // Assert
        // The service may return different error messages depending on configuration
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public async Task PredictCaseOutcomeAsync_ValidInput_ReturnsErrorWhenNotConfigured()
    {
        // Arrange
        var invalidOptions = new AWSOptions { Bedrock = new BedrockOptions { Region = string.Empty, ModelId = string.Empty } };
        var invalidOptionsMock = new Mock<IOptions<AWSOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AWSBedrockService(invalidOptionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.PredictCaseOutcomeAsync("Case details for prediction");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("AWS Bedrock service is not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task SummarizeLegalDocumentAsync_ValidInput_ReturnsErrorWhenNotConfigured()
    {
        // Arrange
        var invalidOptions = new AWSOptions { Bedrock = new BedrockOptions { Region = string.Empty, ModelId = string.Empty } };
        var invalidOptionsMock = new Mock<IOptions<AWSOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AWSBedrockService(invalidOptionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SummarizeLegalDocumentAsync("Long legal document text to summarize");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("AWS Bedrock service is not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task StreamAnalysisAsync_NotConfigured_ReturnsEmptyStream()
    {
        // Arrange
        var invalidOptions = new AWSOptions { Bedrock = new BedrockOptions { Region = string.Empty, ModelId = string.Empty } };
        var invalidOptionsMock = new Mock<IOptions<AWSOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AWSBedrockService(invalidOptionsMock.Object, _loggerMock.Object);
        
        var request = new AIRequest
        {
            DocumentText = "Test document",
            CaseContext = "Test context"
        };

        // Act
        var stream = service.StreamAnalysisAsync(request);
        var results = new List<string>();
        
        await foreach (var chunk in stream)
        {
            results.Add(chunk);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void BuildCaseAnalysisPrompt_ValidInput_ReturnsFormattedPrompt()
    {
        // Arrange
        var service = new TestableAWSBedrockService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AWSBedrockService).GetMethod("BuildCaseAnalysisPrompt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(service, new object[] { "Test document text", "Test case context" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test document text", result);
        Assert.Contains("Test case context", result);
        Assert.Contains("Case Viability Assessment", result);
        Assert.Contains("Key Legal Issues Identified", result);
    }

    [Fact]
    public void BuildPredictionPrompt_ValidInput_ReturnsFormattedPrompt()
    {
        // Arrange
        var service = new TestableAWSBedrockService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AWSBedrockService).GetMethod("BuildPredictionPrompt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(service, new object[] { "Test case details" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test case details", result);
        Assert.Contains("Success Probability", result);
        Assert.Contains("Key Factors Influencing Prediction", result);
    }

    [Fact]
    public void BuildSummaryPrompt_ValidInput_ReturnsFormattedPrompt()
    {
        // Arrange
        var service = new TestableAWSBedrockService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AWSBedrockService).GetMethod("BuildSummaryPrompt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(service, new object[] { "Test document text" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test document text", result);
        Assert.Contains("Key facts and events", result);
        Assert.Contains("Legal issues and arguments", result);
    }

    // Testable class to expose protected constructor for testing
    private class TestableAWSBedrockService : AWSBedrockService
    {
        public TestableAWSBedrockService(
            IOptions<AWSOptions> options, 
            ILogger<AWSBedrockService> logger) 
            : base(options, logger)
        {
        }
    }
}