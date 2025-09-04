using Microsoft.AspNetCore.Identity;

namespace BetterCallSaul.Core.Models.Entities;

public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}