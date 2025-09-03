namespace better_call_saul.Models;

public class DocumentContent
{
    public string ExtractedText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileType { get; set; } = string.Empty;
    public bool ExtractionSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}