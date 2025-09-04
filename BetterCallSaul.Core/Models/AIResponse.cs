namespace BetterCallSaul.Core.Models;

public class AIResponse
{
    public bool Success { get; set; }
    public string? GeneratedText { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? Model { get; set; }
    public double ConfidenceScore { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}