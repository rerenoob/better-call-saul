using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Core.Services;

public interface IAzureOpenAIService
{
    Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default);
    Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default);
    Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default);
    
    // Streaming methods for real-time updates
    IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default);
}