using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public interface IVirusScanningService
{
    Task<ScanResult> ScanFileAsync(string filePath, string fileName);
    Task<bool> IsScannerAvailableAsync();
    Task<ScanResult> ScanFileContentAsync(byte[] fileContent, string fileName);
}