using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Infrastructure.Services;

public interface IVirusScanningService
{
    Task<ScanResult> ScanFileAsync(string filePath, string fileName);
    Task<bool> IsScannerAvailableAsync();
    Task<ScanResult> ScanFileContentAsync(byte[] fileContent, string fileName);
}