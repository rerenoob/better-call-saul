using Microsoft.Extensions.Logging;

namespace better_call_saul.Services;

public class LoggerService : ILoggerService
{
    private readonly ILogger<LoggerService> _logger;

    public LoggerService(ILogger<LoggerService> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        _logger.LogError(exception, message);
    }

    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    public void LogCritical(string message, Exception? exception = null)
    {
        _logger.LogCritical(exception, message);
    }
}