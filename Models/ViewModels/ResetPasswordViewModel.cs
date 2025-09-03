using System.ComponentModel.DataAnnotations;

namespace better_call_saul.Models.ViewModels;

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}