namespace BetterCallSaul.Core.Configuration;

public class NoSqlOptions
{
    public const string SectionName = "NoSql";

    /// <summary>
    /// MongoDB connection string
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Database name for NoSQL operations
    /// </summary>
    public string DatabaseName { get; set; } = "BetterCallSaulDev";

    /// <summary>
    /// Maximum retry attempts for NoSQL operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds for NoSQL operations
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed logging for NoSQL operations
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 100;
}