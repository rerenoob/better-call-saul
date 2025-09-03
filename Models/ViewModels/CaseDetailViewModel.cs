namespace better_call_saul.Models.ViewModels;

public class CaseDetailViewModel
{
    public Case Case { get; set; } = null!;
    public IEnumerable<DocumentSummary> Documents { get; set; } = new List<DocumentSummary>();
    public bool CanEdit { get; set; }
}

public class DocumentSummary
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
}