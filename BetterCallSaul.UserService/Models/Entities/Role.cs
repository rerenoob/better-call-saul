using Microsoft.AspNetCore.Identity;

namespace BetterCallSaul.UserService.Models.Entities;

public class Role : IdentityRole<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}