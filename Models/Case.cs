namespace better_call_saul.Models;

public class Case : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.New;
    public DateTime? ClosedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public enum CaseStatus
{
    New = 0,
    InReview = 1,
    NeedsMoreInfo = 2,
    Completed = 3,
    Closed = 4
}