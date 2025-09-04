namespace BetterCallSaul.Core.Models.Entities;

public class AIRequest
{
    public string? Prompt { get; set; }
    public string? DocumentText { get; set; }
    public string? CaseContext { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.3;
    public string? Model { get; set; }
    public bool Stream { get; set; } = false;
}