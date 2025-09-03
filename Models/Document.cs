namespace better_call_saul.Models;

public class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public int? CaseId { get; set; }
    public Case? Case { get; set; }
}