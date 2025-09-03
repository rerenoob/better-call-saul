using better_call_saul.Models;

namespace better_call_saul.Services;

public interface IAzureOpenAIService
{
    Task<AIAnalysisResult> AnalyzeDocumentAsync(string documentText, string analysisType);
    Task<string> GenerateSummaryAsync(string documentText);
    Task<bool> IsServiceAvailableAsync();
}