namespace better_call_saul.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
}