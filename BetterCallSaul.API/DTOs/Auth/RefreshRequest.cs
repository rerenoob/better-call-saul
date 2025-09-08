using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.API.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}