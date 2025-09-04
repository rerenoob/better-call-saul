namespace BetterCallSaul.Core.Models;

public class UploadResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid FileId { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public string? UploadSessionId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // For error cases
    public string? ErrorCode { get; set; }
    public Dictionary<string, string>? ValidationErrors { get; set; }
}