using System.ComponentModel.DataAnnotations;

namespace better_call_saul.Models.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}