using BetterCallSaul.Core.Models;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services;

public class ClamAvService : IVirusScanningService
{
    private readonly ILogger<ClamAvService> _logger;
    private readonly Random _random = new();
    private bool _scannerAvailable = true;

    public ClamAvService(ILogger<ClamAvService> logger)
    {
        _logger = logger;
    }

    public async Task<ScanResult> ScanFileAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (!_scannerAvailable)
            {
                return new ScanResult
                {
                    Status = ScanStatus.ScannerUnavailable,
                    ErrorMessage = "Virus scanner is currently unavailable",
                    FileName = fileName,
                    ScannedAt = DateTime.UtcNow
                };
            }

            // Simulate network delay for ClamAV scanning
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 500)));

            // Check if file exists
            if (!File.Exists(filePath))
            {
                return new ScanResult
                {
                    Status = ScanStatus.Error,
                    ErrorMessage = "File not found",
                    FileName = fileName,
                    ScannedAt = DateTime.UtcNow
                };
            }

            var fileInfo = new FileInfo(filePath);
            
            // Simulate virus detection for files containing "EICAR" test string
            var fileContent = await File.ReadAllTextAsync(filePath);
            
            if (fileContent.Contains("EICAR", StringComparison.OrdinalIgnoreCase) ||
                fileContent.Contains("X5O", StringComparison.OrdinalIgnoreCase))
            {
                return new ScanResult
                {
                    IsClean = false,
                    IsInfected = true,
                    VirusName = "EICAR-Test-File",
                    Status = ScanStatus.Infected,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    ScannerVersion = "ClamAV 1.0.0",
                    ScanDuration = DateTime.UtcNow - startTime,
                    ScannedAt = DateTime.UtcNow
                };
            }

            // 1% chance of random virus detection for testing
            if (_random.Next(100) < 1)
            {
                var fakeViruses = new[] { "Trojan.Generic", "Worm.Exploit", "Backdoor.Agent", "Ransomware.Crypto" };
                return new ScanResult
                {
                    IsClean = false,
                    IsInfected = true,
                    VirusName = fakeViruses[_random.Next(fakeViruses.Length)],
                    Status = ScanStatus.Infected,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    ScannerVersion = "ClamAV 1.0.0",
                    ScanDuration = DateTime.UtcNow - startTime,
                    ScannedAt = DateTime.UtcNow
                };
            }

            // File is clean
            return new ScanResult
            {
                IsClean = true,
                IsInfected = false,
                Status = ScanStatus.Clean,
                FileName = fileName,
                FileSize = fileInfo.Length,
                ScannerVersion = "ClamAV 1.0.0",
                ScanDuration = DateTime.UtcNow - startTime,
                ScannedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning file: {FileName}", fileName);
            return new ScanResult
            {
                Status = ScanStatus.Error,
                ErrorMessage = $"Scan error: {ex.Message}",
                FileName = fileName,
                ScannedAt = DateTime.UtcNow,
                ScanDuration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<bool> IsScannerAvailableAsync()
    {
        // Simulate occasional scanner unavailability
        if (_random.Next(100) < 5) // 5% chance of scanner being down
        {
            _scannerAvailable = false;
            await Task.Delay(TimeSpan.FromSeconds(30)); // Scanner down for 30 seconds
            _scannerAvailable = true;
            return false;
        }

        return _scannerAvailable;
    }

    public async Task<ScanResult> ScanFileContentAsync(byte[] fileContent, string fileName)
    {
        // Create temporary file and scan it
        var tempFilePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFilePath, fileContent);
            return await ScanFileAsync(tempFilePath, fileName);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}