using BetterCallSaul.Core.Models.NoSQL;

namespace BetterCallSaul.Core.Interfaces.Repositories;

public interface ICaseDocumentRepository
{
    Task<CaseDocument?> GetByIdAsync(Guid caseId);
    Task<CaseDocument> CreateAsync(CaseDocument document);
    Task<CaseDocument> UpdateAsync(CaseDocument document);
    Task DeleteAsync(Guid caseId);
    Task<List<CaseDocument>> GetByUserIdAsync(Guid userId);
    Task<List<CaseDocument>> SearchAsync(CaseSearchCriteria criteria);
    Task<CaseAnalysisStats> GetAnalysisStatsAsync(Guid caseId);
    Task<bool> ExistsAsync(Guid caseId);
    Task<List<CaseDocument>> GetPagedAsync(int page, int pageSize);
    Task<long> CountAsync();
    Task<long> CountByUserAsync(Guid userId);
}

public class CaseSearchCriteria
{
    public Guid? UserId { get; set; }
    public string? SearchText { get; set; }
    public List<string>? Tags { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public bool? HasAnalysis { get; set; }
    public double? MinViabilityScore { get; set; }
    public List<string>? DocumentTypes { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDescending { get; set; } = true;
}

public class CaseAnalysisStats
{
    public int TotalAnalyses { get; set; }
    public int CompletedAnalyses { get; set; }
    public int PendingAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public double AverageViabilityScore { get; set; }
    public double AverageConfidenceScore { get; set; }
    public DateTime? LastAnalyzedAt { get; set; }
    public List<string> TopLegalIssues { get; set; } = new();
    public List<string> TopRecommendations { get; set; } = new();
}