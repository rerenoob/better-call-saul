using better_call_saul.Models;

namespace better_call_saul.Models;

public class DashboardData
{
    public int TotalCases { get; set; }
    public int CasesThisMonth { get; set; }
    public Dictionary<CaseStatus, int> CasesByStatus { get; set; } = new();
    public List<CaseSummary> RecentCases { get; set; } = new();
    public int DocumentsProcessed { get; set; }
    public int AnalysesCompleted { get; set; }
    public bool HasData => TotalCases > 0 || DocumentsProcessed > 0 || AnalysesCompleted > 0;
}

public class CaseSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocumentCount { get; set; }
    public bool HasAnalysis { get; set; }
    public double? ConfidenceScore { get; set; }
}