using better_call_saul.Models;

namespace better_call_saul.Models.ViewModels;

public class AnalysisDisplayViewModel
{
    public CaseAnalysis? Analysis { get; set; }
    public bool IsAnalyzing { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CanAnalyze { get; set; }
    public DateTime? LastAnalyzed { get; set; }
}