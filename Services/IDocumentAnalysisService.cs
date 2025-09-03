using better_call_saul.Models;

namespace better_call_saul.Services;

public interface IDocumentAnalysisService
{
    Task<CaseAnalysis> AnalyzeCaseDocumentsAsync(int caseId, string userId);
    Task<CaseAnalysis?> GetLatestAnalysisAsync(int caseId, string userId);
    Task<bool> HasAnalysisAsync(int caseId, string userId);
}