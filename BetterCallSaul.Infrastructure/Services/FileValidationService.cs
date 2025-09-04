using BetterCallSaul.Core.Models;
using BetterCallSaul.Infrastructure.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services;

public class FileValidationService : IFileValidationService
{
    private readonly IVirusScanningService _virusScanningService;
    private readonly ILogger<FileValidationService> _logger;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private static readonly string[] _supportedExtensions = { ".pdf", ".doc", ".docx", ".txt" };

    public FileValidationService(
        IVirusScanningService virusScanningService,
        ILogger<FileValidationService> logger)
    {
        _virusScanningService = virusScanningService;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateFileAsync(IFormFile file)
    {
        try
        {
            // Basic validation
            var (isValid, errors) = FileUploadValidator.ValidateFile(file);
            if (!isValid)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ValidationErrors = errors,
                    Status = FileValidationStatus.Invalid
                };
            }

            // Check file size
            if (!await IsFileSizeWithinLimitAsync(file.Length))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size exceeds the maximum limit of {MaxFileSize / (1024 * 1024)}MB",
                    Status = FileValidationStatus.SizeExceeded
                };
            }

            // Check file type
            if (!await IsFileTypeSupportedAsync(file.FileName))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File type not supported. Supported types: {string.Join(", ", _supportedExtensions)}",
                    Status = FileValidationStatus.TypeNotSupported
                };
            }

            // Virus scanning
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return await ValidateFileAsync(tempFilePath, file.FileName);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file: {FileName}", file.FileName);
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}",
                Status = FileValidationStatus.Error
            };
        }
    }

    public async Task<ValidationResult> ValidateFileAsync(string filePath, string fileName)
    {
        try
        {
            // Virus scanning
            var scanResult = await _virusScanningService.ScanFileAsync(filePath, fileName);

            if (scanResult.Status == ScanStatus.Infected)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File contains malware: {scanResult.VirusName}",
                    ScanResult = scanResult,
                    Status = FileValidationStatus.Infected
                };
            }

            if (scanResult.Status != ScanStatus.Clean)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Virus scan failed: {scanResult.ErrorMessage}",
                    ScanResult = scanResult,
                    Status = FileValidationStatus.Error
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                ScanResult = scanResult,
                Status = FileValidationStatus.Valid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file: {FileName}", fileName);
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}",
                Status = FileValidationStatus.Error
            };
        }
    }

    public Task<bool> IsFileTypeSupportedAsync(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return Task.FromResult(_supportedExtensions.Contains(extension));
    }

    public Task<bool> IsFileSizeWithinLimitAsync(long fileSize)
    {
        return Task.FromResult(fileSize <= MaxFileSize);
    }
}