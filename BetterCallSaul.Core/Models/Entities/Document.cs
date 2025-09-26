using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BetterCallSaul.Core.Models.Entities;

public class Document : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DocumentType Type { get; set; } = DocumentType.Other;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public bool IsProcessed { get; set; } = false;

    // Foreign keys
    public Guid CaseId { get; set; }
    public Guid? UploadedById { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual Case Case { get; set; } = null!;
    [JsonIgnore]
    public virtual User? UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}