namespace better_call_saul.Models;

public class AIAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> KeyPoints { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}