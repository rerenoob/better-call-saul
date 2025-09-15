using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Services.AI;

public interface ICaseAnalysisService
{
    Task<CaseAnalysisDocument> AnalyzeCaseAsync(string caseId, string documentId, string documentText, CancellationToken cancellationToken = default);
    Task<CaseAnalysisDocument?> GetAnalysisAsync(string analysisId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CaseAnalysisDocument>> GetCaseAnalysesAsync(string caseId, CancellationToken cancellationToken = default);
    Task UpdateAnalysisStatusAsync(string analysisId, string status, string? message = null, CancellationToken cancellationToken = default);
    
    // Real-time progress updates
    event EventHandler<AnalysisProgressEventArgs> AnalysisProgress;
}

public class AnalysisProgressEventArgs : EventArgs
{
    public string AnalysisId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}