namespace better_call_saul.Services;

public interface ILoggerService
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogDebug(string message);
    void LogCritical(string message, Exception? exception = null);
}