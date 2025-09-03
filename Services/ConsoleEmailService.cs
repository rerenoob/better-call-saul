namespace better_call_saul.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        _logger.LogInformation("Password reset email would be sent to: {Email}", email);
        _logger.LogInformation("Reset link: {ResetLink}", resetLink);
        
        Console.WriteLine($"\n=== PASSWORD RESET EMAIL ===");
        Console.WriteLine($"To: {email}");
        Console.WriteLine($"Reset Link: {resetLink}");
        Console.WriteLine($"===========================\n");
        
        return Task.CompletedTask;
    }
}