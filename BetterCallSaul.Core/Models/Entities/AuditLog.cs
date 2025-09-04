using BetterCallSaul.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    [MaxLength(1000)]
    public string? OldValues { get; set; }

    [MaxLength(1000)]
    public string? NewValues { get; set; }

    public AuditLogLevel Level { get; set; } = AuditLogLevel.Info;

    // Foreign key
    public Guid? UserId { get; set; }

    // Navigation property
    public virtual User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }
}