using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.UserService.Models.Entities;

public class RegistrationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsUsed { get; set; } = false;

    public Guid? UsedByUserId { get; set; }
    
    public DateTime? UsedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User? UsedByUser { get; set; }

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}