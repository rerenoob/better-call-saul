using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class MockAIServiceTests
{
    private readonly Mock<ILogger<MockAIService>> _loggerMock;
    private readonly MockAIService _service;

    public MockAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<MockAIService>>();
        _service = new MockAIService(_loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new AIRequest
        {
            DocumentText = "Test legal document content",
            CaseContext = "Test case context",
            MaxTokens = 2000,
            Temperature = 0.3
        };

        // Act
        var result = await _service.AnalyzeCaseAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedText);
        Assert.Contains("AI-Powered Legal Case Analysis", result.GeneratedText);
        Assert.Equal("mock-ai-model-v2", result.Model);
        Assert.InRange(result.ConfidenceScore, 0.7, 0.95);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 1000);
        Assert.NotNull(result.Metadata);
        Assert.True((bool)result.Metadata["mockService"]);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // Arrange
        var request = new AIRequest { DocumentText = "Test" };
        var cancellationToken = new CancellationToken(canceled: true);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _service.AnalyzeCaseAsync(request, cancellationToken));
    }

    [Fact]
    public async Task GenerateLegalAnalysisAsync_ValidInput_ReturnsSuccessResponse()
    {
        // Arrange
        var documentText = "Legal document text for analysis";
        var caseContext = "Case background and context information";

        // Act
        var result = await _service.GenerateLegalAnalysisAsync(documentText, caseContext);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedText);
        Assert.Contains("Comprehensive Legal Document Analysis", result.GeneratedText);
        Assert.Contains(documentText.Length.ToString(), result.GeneratedText);
        Assert.Equal("mock-legal-model-pro", result.Model);
        Assert.InRange(result.ConfidenceScore, 0.75, 0.95);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 800);
    }

    [Fact]
    public async Task PredictCaseOutcomeAsync_ValidInput_ReturnsSuccessResponse()
    {
        // Arrange
        var caseDetails = "Case involving contract dispute with clear evidence of breach";

        // Act
        var result = await _service.PredictCaseOutcomeAsync(caseDetails);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedText);
        Assert.Contains("AI-Powered Case Outcome Prediction", result.GeneratedText);
        Assert.True(result.GeneratedText.Contains("Favorable") || result.GeneratedText.Contains("Unfavorable"));
        Assert.Contains("%", result.GeneratedText);
        Assert.Equal("mock-prediction-model-ai", result.Model);
        Assert.InRange(result.ConfidenceScore, 0.6, 0.95);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 500);
    }

    [Fact]
    public async Task SummarizeLegalDocumentAsync_ValidInput_ReturnsSuccessResponse()
    {
        // Arrange
        var documentText = "This is a lengthy legal document containing multiple sections, arguments, and evidence. The document discusses various legal precedents and provides detailed analysis of the case facts.";

        // Act
        var result = await _service.SummarizeLegalDocumentAsync(documentText);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedText);
        Assert.Contains("AI-Generated Legal Document Summary", result.GeneratedText);
        Assert.Contains(documentText.Length.ToString(), result.GeneratedText);
        Assert.Equal("mock-summary-model-xl", result.Model);
        Assert.InRange(result.ConfidenceScore, 0.85, 0.95);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 400);
    }

    [Fact]
    public async Task SummarizeLegalDocumentAsync_NullInput_ReturnsSuccessResponse()
    {
        // Arrange
        string documentText = "";

        // Act
        var result = await _service.SummarizeLegalDocumentAsync(documentText);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedText);
        Assert.Contains("0 characters", result.GeneratedText);
    }

    [Fact]
    public async Task StreamAnalysisAsync_ValidRequest_ReturnsStreamingChunks()
    {
        // Arrange
        var request = new AIRequest
        {
            DocumentText = "Test streaming document",
            CaseContext = "Test streaming context"
        };

        // Act
        var stream = _service.StreamAnalysisAsync(request);
        var chunks = new List<string>();
        
        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.True(chunks.Count >= 10);
        Assert.Contains("Initializing legal analysis engine", chunks[0]);
        Assert.Contains("Processing document content", chunks[1]);
        Assert.Contains("Analysis complete", chunks[^1]);
    }

    [Fact]
    public async Task StreamAnalysisAsync_CancellationRequested_ReturnsPartialStream()
    {
        // Arrange
        var request = new AIRequest { DocumentText = "Test" };
        var cts = new CancellationTokenSource();
        var chunks = new List<string>();

        // Act
        var stream = _service.StreamAnalysisAsync(request, cts.Token);
        
        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
            if (chunks.Count >= 3)
            {
                cts.Cancel();
                break;
            }
        }

        // Assert - Should have received at least 3 chunks before cancellation
        Assert.True(chunks.Count >= 3);
        Assert.Contains("Initializing", chunks[0]);
    }

    [Fact]
    public void GenerateMockAnalysis_ValidRequest_ReturnsFormattedAnalysis()
    {
        // Arrange
        var request = new AIRequest
        {
            DocumentText = "Test document",
            CaseContext = "Test context"
        };

        // Act - Use reflection to test private method
        var method = typeof(MockAIService).GetMethod("GenerateMockAnalysis", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(_service, new object[] { request }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("AI-Powered Legal Case Analysis", result);
        Assert.Contains("Key Legal Findings", result);
        Assert.Contains("Strategic Recommendations", result);
        Assert.Contains("%", result);
    }

    [Fact]
    public void GenerateMockLegalAnalysis_ValidInput_ReturnsFormattedAnalysis()
    {
        // Arrange
        var documentText = "Legal document content";
        var caseContext = "Case background information";

        // Act - Use reflection to test private method
        var method = typeof(MockAIService).GetMethod("GenerateMockLegalAnalysis", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(_service, new object[] { documentText, caseContext }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Comprehensive Legal Document Analysis", result);
        Assert.Contains(documentText.Length.ToString(), result);
        Assert.Contains("Document Quality Assessment", result);
    }

    [Fact]
    public void GenerateMockPrediction_ValidInput_ReturnsFormattedPrediction()
    {
        // Arrange
        var caseDetails = "Case prediction details";

        // Act - Use reflection to test private method
        var method = typeof(MockAIService).GetMethod("GenerateMockPrediction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(_service, new object[] { caseDetails, true, 0.75 }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("AI-Powered Case Outcome Prediction", result);
        Assert.True(result.Contains("Favorable") || result.Contains("Unfavorable"));
        Assert.Contains("%", result);
    }

    [Fact]
    public void GenerateMockSummary_ValidInput_ReturnsFormattedSummary()
    {
        // Arrange
        var documentText = "Document to summarize";

        // Act - Use reflection to test private method
        var method = typeof(MockAIService).GetMethod("GenerateMockSummary", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(_service, new object[] { documentText }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("AI-Generated Legal Document Summary", result);
        Assert.Contains(documentText.Length.ToString(), result);
        Assert.Contains("Key Points Identified", result);
    }

    [Fact]
    public async Task AllMethods_ReturnDifferentMockResponses_ForVariedInputs()
    {
        // Test that different inputs produce different (but consistent) mock responses
        var request1 = new AIRequest { DocumentText = "Input 1", CaseContext = "Context 1" };
        var request2 = new AIRequest { DocumentText = "Input 2", CaseContext = "Context 2" };

        var result1 = await _service.AnalyzeCaseAsync(request1);
        var result2 = await _service.AnalyzeCaseAsync(request2);

        // Responses should be different but both valid
        Assert.NotEqual(result1.GeneratedText, result2.GeneratedText);
        Assert.True(result1.Success);
        Assert.True(result2.Success);
    }
}