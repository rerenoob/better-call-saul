using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.API.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string RegistrationCode { get; set; } = string.Empty;

    public string? BarNumber { get; set; }

    public string? LawFirm { get; set; }
}