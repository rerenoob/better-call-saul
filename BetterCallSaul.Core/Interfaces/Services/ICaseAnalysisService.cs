using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Core.Interfaces.Services;

public interface ICaseAnalysisService
{
    Task<CaseAnalysis> AnalyzeCaseAsync(Guid caseId, Guid documentId, string documentText, CancellationToken cancellationToken = default);
    Task<CaseAnalysis> GetAnalysisAsync(Guid analysisId, CancellationToken cancellationToken = default);
    Task<List<CaseAnalysis>> GetCaseAnalysesAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task UpdateAnalysisStatusAsync(Guid analysisId, AnalysisStatus status, string? message = null, CancellationToken cancellationToken = default);
    
    // Real-time progress updates
    event EventHandler<AnalysisProgressEventArgs> AnalysisProgress;
}

public class AnalysisProgressEventArgs : EventArgs
{
    public Guid AnalysisId { get; set; }
    public Guid CaseId { get; set; }
    public AnalysisStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}