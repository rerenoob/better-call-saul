namespace better_call_saul.Models;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
}