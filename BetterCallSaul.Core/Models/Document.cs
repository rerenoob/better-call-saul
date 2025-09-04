using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models;

public class Document : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(500)]
    public string? StoragePath { get; set; }

    public DocumentType Type { get; set; } = DocumentType.Other;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }

    // Foreign keys
    public Guid CaseId { get; set; }
    public Guid? UploadedById { get; set; }

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
    public virtual User? UploadedBy { get; set; }
    public virtual DocumentText? ExtractedText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}