using System.Text.Json;

namespace better_call_saul.Models.ViewModels;

public class AnalysisResultViewModel
{
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public bool IsRecent => AnalyzedAt > DateTime.UtcNow.AddDays(-1);

    public static AnalysisResultViewModel FromCaseAnalysis(CaseAnalysis analysis)
    {
        return new AnalysisResultViewModel
        {
            Summary = analysis.Summary,
            Recommendation = analysis.Recommendation,
            KeyPoints = JsonSerializer.Deserialize<List<string>>(analysis.KeyPoints) ?? new List<string>(),
            ConfidenceScore = analysis.ConfidenceScore,
            AnalyzedAt = analysis.AnalyzedAt
        };
    }
}