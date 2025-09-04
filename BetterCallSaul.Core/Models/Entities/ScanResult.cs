namespace BetterCallSaul.Core.Models.Entities;

public class ScanResult
{
    public bool IsClean { get; set; }
    public bool IsInfected { get; set; }
    public string? VirusName { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? ScannerVersion { get; set; }
    public TimeSpan ScanDuration { get; set; }
    public ScanStatus Status { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
}

public enum ScanStatus
{
    Clean,
    Infected,
    Error,
    Timeout,
    ScannerUnavailable
}