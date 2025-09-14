using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Core.Interfaces.Services;

/// <summary>
/// Generic AI service interface for Azure/AWS provider switching
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Analyzes a legal case using AI with comprehensive request parameters
    /// </summary>
    /// <param name="request">AI request containing document text, context, and parameters</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>AI response with generated analysis and metadata</returns>
    Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates legal analysis for a document with case context
    /// </summary>
    /// <param name="documentText">The legal document text to analyze</param>
    /// <param name="caseContext">Case context and background information</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>AI response with generated legal analysis</returns>
    Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts the outcome of a legal case based on provided details
    /// </summary>
    /// <param name="caseDetails">Case details and facts for prediction</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>AI response with outcome prediction and confidence</returns>
    Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarizes a legal document concisely
    /// </summary>
    /// <param name="documentText">The legal document text to summarize</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>AI response with document summary</returns>
    Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams analysis results in real-time for better user experience
    /// </summary>
    /// <param name="request">AI request parameters</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Async enumerable of streaming text chunks</returns>
    IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default);
}