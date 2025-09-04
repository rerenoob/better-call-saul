using BetterCallSaul.Core.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public interface IFileValidationService
{
    Task<ValidationResult> ValidateFileAsync(IFormFile file);
    Task<ValidationResult> ValidateFileAsync(string filePath, string fileName);
    Task<bool> IsFileTypeSupportedAsync(string fileName);
    Task<bool> IsFileSizeWithinLimitAsync(long fileSize);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? ValidationErrors { get; set; }
    public ScanResult? ScanResult { get; set; }
    public FileValidationStatus Status { get; set; }
}

public enum FileValidationStatus
{
    Valid,
    Invalid,
    Infected,
    Error,
    SizeExceeded,
    TypeNotSupported
}