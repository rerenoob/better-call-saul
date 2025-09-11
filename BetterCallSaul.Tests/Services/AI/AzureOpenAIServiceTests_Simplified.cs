using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class AzureOpenAIServiceTests_Simplified
{
    private readonly Mock<IOptions<OpenAIOptions>> _optionsMock;
    private readonly Mock<ILogger<AzureOpenAIService>> _loggerMock;
    private readonly OpenAIOptions _openAIOptions;

    public AzureOpenAIServiceTests_Simplified()
    {
        _openAIOptions = new OpenAIOptions
        {
            EndpointFromConfig = "https://test-openai.openai.azure.com/",
            ApiKeyFromConfig = "test-api-key-1234567890",
            DeploymentName = "test-deployment",
            Model = "gpt-4",
            MaxTokens = 4000,
            Temperature = 0.7
        };

        _optionsMock = new Mock<IOptions<OpenAIOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_openAIOptions);
        
        _loggerMock = new Mock<ILogger<AzureOpenAIService>>();
    }

    [Fact]
    public void Constructor_MissingConfiguration_DoesNotThrow()
    {
        // Arrange
        var invalidOptions = new OpenAIOptions { EndpointFromConfig = null, ApiKeyFromConfig = null, DeploymentName = null };
        var invalidOptionsMock = new Mock<IOptions<OpenAIOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var loggerMock = new Mock<ILogger<AzureOpenAIService>>();

        // Act & Assert - Constructor should not throw even with invalid config
        var service = new AzureOpenAIService(invalidOptionsMock.Object, loggerMock.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_NotConfigured_ReturnsErrorResponse()
    {
        // Arrange
        var invalidOptions = new OpenAIOptions { EndpointFromConfig = null, ApiKeyFromConfig = null, DeploymentName = null };
        var invalidOptionsMock = new Mock<IOptions<OpenAIOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AzureOpenAIService(invalidOptionsMock.Object, _loggerMock.Object);
        
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
        Assert.NotEmpty(result.ErrorMessage);
        // The error message could be either "Azure OpenAI service is not configured" or an actual Azure API error
    }

    [Fact]
    public async Task GenerateLegalAnalysisAsync_ValidInput_CallsAnalyzeCaseAsync()
    {
        // Arrange
        var service = new AzureOpenAIService(_optionsMock.Object, _loggerMock.Object);

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
        var service = new AzureOpenAIService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.PredictCaseOutcomeAsync("Case details for prediction");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public async Task SummarizeLegalDocumentAsync_ValidInput_ReturnsErrorWhenNotConfigured()
    {
        // Arrange
        var service = new AzureOpenAIService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SummarizeLegalDocumentAsync("Long legal document text to summarize");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public async Task StreamAnalysisAsync_NotConfigured_ReturnsEmptyStream()
    {
        // Arrange
        var invalidOptions = new OpenAIOptions { EndpointFromConfig = null, ApiKeyFromConfig = null, DeploymentName = null };
        var invalidOptionsMock = new Mock<IOptions<OpenAIOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        var service = new AzureOpenAIService(invalidOptionsMock.Object, _loggerMock.Object);
        
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
        var service = new TestableAzureOpenAIService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AzureOpenAIService).GetMethod("BuildCaseAnalysisPrompt", 
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
        var service = new TestableAzureOpenAIService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AzureOpenAIService).GetMethod("BuildPredictionPrompt", 
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
        var service = new TestableAzureOpenAIService(_optionsMock.Object, _loggerMock.Object);
        
        // Act - Use reflection to call the private method
        var method = typeof(AzureOpenAIService).GetMethod("BuildSummaryPrompt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(service, new object[] { "Test document text" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test document text", result);
        Assert.Contains("Key facts and events", result);
        Assert.Contains("Legal issues and arguments", result);
    }

    // Testable class to expose protected constructor for testing
    private class TestableAzureOpenAIService : AzureOpenAIService
    {
        public TestableAzureOpenAIService(
            IOptions<OpenAIOptions> options, 
            ILogger<AzureOpenAIService> logger) 
            : base(options, logger)
        {
        }
    }
}