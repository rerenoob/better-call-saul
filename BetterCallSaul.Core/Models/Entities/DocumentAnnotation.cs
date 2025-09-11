using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class DocumentAnnotation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid DocumentId { get; set; }
    public virtual Document Document { get; set; } = null!;
    
    public int PageNumber { get; set; }
    
    public AnnotationType Type { get; set; }
    
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    // Position and dimensions for the annotation
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    [MaxLength(20)]
    public string Color { get; set; } = "#ffff00";
    
    public Guid CreatedBy { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum AnnotationType
{
    Highlight,
    Note,
    Rectangle,
    Circle,
    Arrow,
    Text,
    Stamp,
    Signature
}