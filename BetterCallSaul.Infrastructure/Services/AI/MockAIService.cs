using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.AI;

/// <summary>
/// Mock AI service implementation for development and testing environments
/// </summary>
public class MockAIService : IAIService
{
    private readonly ILogger<MockAIService> _logger;

    public MockAIService(ILogger<MockAIService> logger)
    {
        _logger = logger;
    }

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service analyzing case with prompt: {Prompt}",
            request.Prompt?.Substring(0, Math.Min(100, request.Prompt?.Length ?? 0)));

        // Simulate processing delay
        await Task.Delay(1000, cancellationToken);

        return new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockAnalysis(request),
            TokensUsed = 150,
            ProcessingTime = TimeSpan.FromMilliseconds(1000),
            Model = "mock-ai-model",
            ConfidenceScore = 0.85,
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["environment"] = "development"
            }
        };
    }

    public async Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service generating legal analysis for document of length: {Length}", documentText?.Length ?? 0);

        await Task.Delay(800, cancellationToken);

        return new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockLegalAnalysis(documentText, caseContext),
            TokensUsed = 200,
            ProcessingTime = TimeSpan.FromMilliseconds(800),
            Model = "mock-legal-model",
            ConfidenceScore = 0.82,
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["analysisType"] = "legal"
            }
        };
    }

    public async Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service predicting case outcome");

        await Task.Delay(600, cancellationToken);

        return new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockPrediction(caseDetails),
            TokensUsed = 100,
            ProcessingTime = TimeSpan.FromMilliseconds(600),
            Model = "mock-prediction-model",
            ConfidenceScore = 0.75,
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["predictionType"] = "outcome"
            }
        };
    }

    public async Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service summarizing document of length: {Length}", documentText?.Length ?? 0);

        await Task.Delay(500, cancellationToken);

        return new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockSummary(documentText),
            TokensUsed = 75,
            ProcessingTime = TimeSpan.FromMilliseconds(500),
            Model = "mock-summary-model",
            ConfidenceScore = 0.90,
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["summaryType"] = "document"
            }
        };
    }

    public async IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service streaming analysis");

        var chunks = new[]
        {
            "Analyzing case details... ",
            "Reviewing legal precedents... ",
            "Assessing evidence strength... ",
            "Generating recommendations... ",
            "Finalizing analysis... ",
            "Analysis complete."
        };

        foreach (var chunk in chunks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            await Task.Delay(200, cancellationToken);
            yield return chunk;
        }
    }

    private static string GenerateMockAnalysis(AIRequest request)
    {
        return $"""
            # Mock Legal Case Analysis

            ## Summary
            This is a mock analysis generated for development purposes. The system has processed your request and would normally provide comprehensive legal analysis here.

            ## Key Findings
            - Mock finding 1: Document contains relevant legal terminology
            - Mock finding 2: Case appears to have sufficient evidence for proceeding
            - Mock finding 3: Legal precedents suggest favorable outcome potential

            ## Recommendations
            1. Review additional documentation if available
            2. Consider consulting relevant case law
            3. Prepare for potential counterarguments

            ## Confidence Level
            Mock confidence score: 85%

            *Note: This is a mock response for development environment. In production, this would be replaced by actual AI analysis.*
            """;
    }

    private static string GenerateMockLegalAnalysis(string? documentText, string? caseContext)
    {
        return $"""
            # Mock Legal Document Analysis

            ## Document Overview
            Document length: {documentText?.Length ?? 0} characters
            Context provided: {!string.IsNullOrEmpty(caseContext)}

            ## Legal Assessment
            - Document type: Standard legal document (mock analysis)
            - Completeness: Document appears complete based on mock review
            - Legal validity: Mock assessment indicates standard format

            ## Key Legal Points
            1. Mock legal point 1 based on document content
            2. Mock legal point 2 regarding case context
            3. Mock legal point 3 for procedural considerations

            ## Next Steps
            - Mock recommendation 1
            - Mock recommendation 2
            - Mock recommendation 3

            *This is a development environment mock response.*
            """;
    }

    private static string GenerateMockPrediction(string caseDetails)
    {
        return $"""
            # Mock Case Outcome Prediction

            ## Prediction Summary
            Based on mock analysis of the provided case details, the following prediction is generated:

            **Predicted Outcome:** Favorable (Mock Prediction)
            **Confidence Level:** 75%

            ## Factors Considered (Mock)
            - Case detail analysis (simulated)
            - Legal precedent review (simulated)
            - Evidence assessment (simulated)

            ## Risk Assessment
            - Low risk factors: Mock factor 1, Mock factor 2
            - Medium risk factors: Mock factor 3
            - High risk factors: None identified in mock analysis

            *This is a mock prediction for development purposes.*
            """;
    }

    private static string GenerateMockSummary(string? documentText)
    {
        return $"""
            # Mock Document Summary

            ## Document Overview
            - Length: {documentText?.Length ?? 0} characters
            - Type: Legal document (mock classification)
            - Complexity: Standard (mock assessment)

            ## Key Points (Mock)
            1. Main legal argument or claim identified
            2. Supporting evidence mentioned
            3. Procedural requirements noted
            4. Relevant dates and deadlines highlighted

            ## Summary
            This document appears to be a standard legal filing with the expected structure and content. The mock analysis indicates all required elements are present.

            *This is a development environment mock summary.*
            """;
    }
}