using BetterCallSaul.CaseService.Models.Entities;
using BetterCallSaul.CaseService.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.CaseService.Services.FileProcessing;

public class FileUploadService : IFileUploadService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<FileUploadService> _logger;
    private const long MaxUserUploadSizePerHour = 500 * 1024 * 1024; // 500MB per hour

    public FileUploadService(IDocumentRepository documentRepository, ILogger<FileUploadService> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<UploadResult> UploadFileAsync(IFormFile file, string caseId, string userId, string uploadSessionId)
    {
        var result = new UploadResult { UploadSessionId = uploadSessionId };

        try
        {
            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = validationResult.ErrorMessage ?? "File validation failed";
                result.ValidationErrors = validationResult.ValidationErrors;
                return result;
            }

            // Check user upload limits
            var userUploadSize = await GetTotalUploadSizeForUserAsync(userId, TimeSpan.FromHours(1));
            if (userUploadSize + file.Length > MaxUserUploadSizePerHour)
            {
                result.Success = false;
                result.Message = "Upload limit exceeded. Please try again later.";
                return result;
            }

            // Generate unique filename
            var uniqueFileName = await GenerateUniqueFileNameAsync(file.FileName);
            
            // Store file (in production, this would use AWS S3 or similar)
            var storagePath = await StoreFileAsync(file, uniqueFileName);

            // Create document record
            var document = new DocumentDocument
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FileType = Path.GetExtension(file.FileName).ToLowerInvariant(),
                FileSize = file.Length,
                StoragePath = storagePath,
                CaseId = caseId,
                UploadedBy = userId,
                Status = "Uploaded",
                DocumentType = "Other"
            };

            document = await _documentRepository.CreateAsync(document);

            result.Success = true;
            result.FileName = document.FileName;
            result.FileSize = document.FileSize;
            result.FileType = document.FileType;
            result.Message = "File uploaded successfully";
            result.FileId = document.Id;

            _logger.LogInformation("File uploaded successfully: {FileName} (ID: {FileId})", file.FileName, document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            result.Success = false;
            result.Message = $"Error uploading file: {ex.Message}";
            result.ErrorCode = "UPLOAD_ERROR";
        }

        return result;
    }

    public Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
    {
        // For development: return the local file path
        // In production, this would generate a signed URL for cloud storage
        return Task.FromResult(storagePath);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public async Task<long> GetTotalUploadSizeForUserAsync(string userId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        var documents = await _documentRepository.GetByUploaderAsync(userId);
        
        return documents
            .Where(d => d.CreatedAt >= cutoffTime)
            .Sum(d => d.FileSize);
    }

    private (bool IsValid, string? ErrorMessage, Dictionary<string, string>? ValidationErrors) ValidateFile(IFormFile file)
    {
        var errors = new Dictionary<string, string>();

        // Check file size (max 50MB)
        if (file.Length > 50 * 1024 * 1024)
        {
            errors["fileSize"] = "File size exceeds maximum limit of 50MB";
        }

        // Check file type
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            errors["fileType"] = $"File type {extension} is not supported";
        }

        if (errors.Any())
        {
            return (false, "File validation failed", errors);
        }

        return (true, null, null);
    }

    private Task<string> GenerateUniqueFileNameAsync(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        return Task.FromResult($"{baseName}_{timestamp}_{random}{extension}");
    }

    private async Task<string> StoreFileAsync(IFormFile file, string fileName)
    {
        // For development: store in local temp directory
        // In production, this would use AWS S3 integration
        var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "temp");
        
        if (!Directory.Exists(uploadsDirectory))
        {
            Directory.CreateDirectory(uploadsDirectory);
        }

        var filePath = Path.Combine(uploadsDirectory, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }
}