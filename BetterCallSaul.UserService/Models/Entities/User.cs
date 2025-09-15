using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.UserService.Models.Entities;

public class User : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? BarNumber { get; set; }

    [MaxLength(100)]
    public string? LawFirm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual ICollection<RegistrationCode> RegistrationCodes { get; set; } = new List<RegistrationCode>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}