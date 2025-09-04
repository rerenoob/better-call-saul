namespace BetterCallSaul.Core.Models;

public class DocumentText
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string? FullText { get; set; }
    public double ConfidenceScore { get; set; }
    public int PageCount { get; set; }
    public long CharacterCount { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, object>? ExtractionMetadata { get; set; }
    public List<TextPage>? Pages { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public virtual Document? Document { get; set; }
}