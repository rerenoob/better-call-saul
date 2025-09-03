namespace better_call_saul.Models;

public class CaseAnalysis : BaseEntity
{
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string KeyPoints { get; set; } = string.Empty; // JSON array
    public double ConfidenceScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string AnalysisVersion { get; set; } = "1.0";
}