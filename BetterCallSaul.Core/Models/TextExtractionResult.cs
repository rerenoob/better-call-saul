namespace BetterCallSaul.Core.Models;

public class TextExtractionResult
{
    public bool Success { get; set; }
    public string? ExtractedText { get; set; }
    public string? ErrorMessage { get; set; }
    public double ConfidenceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public TextExtractionStatus Status { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public List<TextPage>? Pages { get; set; }
}

public enum TextExtractionStatus
{
    Success,
    Failed,
    UnsupportedFormat,
    ProcessingError,
    Timeout
}

public class TextPage
{
    public int PageNumber { get; set; }
    public string? Text { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, object>? PageMetadata { get; set; }
}