using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.ServiceResponses;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class FileUploadService : IFileUploadService, IStorageService
{
    private readonly BetterCallSaulContext _context;
    private readonly IFileValidationService _fileValidationService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<FileUploadService> _logger;
    private const long MaxUserUploadSizePerHour = 500 * 1024 * 1024; // 500MB per hour

    public FileUploadService(
        BetterCallSaulContext context, 
        IFileValidationService fileValidationService,
        ITextExtractionService textExtractionService,
        ILogger<FileUploadService> logger)
    {
        _context = context;
        _fileValidationService = fileValidationService;
        _textExtractionService = textExtractionService;
        _logger = logger;
    }

    public async Task<UploadResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var storageResult = await UploadFileToStorageAsync(file, caseId, userId, uploadSessionId);
        return ConvertToUploadResult(storageResult);
    }

    async Task<StorageResult> IStorageService.UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        return await UploadFileToStorageAsync(file, caseId, userId, uploadSessionId);
    }

    public async Task<StorageResult> UploadFileToStorageAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var result = new StorageResult { UploadSessionId = uploadSessionId };

        try
        {
            // Validate file with virus scanning
            var validationResult = await _fileValidationService.ValidateFileAsync(file);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = validationResult.ErrorMessage ?? "File validation failed";
                result.ErrorCode = validationResult.Status.ToString();
                
                if (validationResult.ValidationErrors != null)
                {
                    result.ValidationErrors = validationResult.ValidationErrors;
                }
                
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
            
            // Store file (for now using local storage, will integrate Azure Blob Storage later)
            var storagePath = await StoreFileAsync(file, uniqueFileName);

            // Create document record
            var document = new Document
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FileType = Path.GetExtension(file.FileName).ToLowerInvariant(),
                FileSize = file.Length,
                StoragePath = storagePath,
                CaseId = caseId,
                UploadedById = userId,
                Status = Core.Enums.DocumentStatus.Uploaded,
                Type = Core.Enums.DocumentType.Other
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Perform text extraction after successful upload
            await ExtractTextFromDocumentAsync(document, storagePath);

            result.Success = true;
            result.FileName = document.FileName;
            result.FileSize = document.FileSize;
            result.FileType = document.FileType;
            result.Message = "File uploaded and text extracted successfully";
            
            // Add FileId to metadata for database reference
            result.Metadata ??= new Dictionary<string, string>();
            result.Metadata["FileId"] = document.Id.ToString();

            _logger.LogInformation("File uploaded and processed successfully: {FileName} (ID: {FileId})", file.FileName, document.Id);
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

    public async Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
    {
        // For local file storage, we don't generate secure URLs
        // In production, this would be handled by a proper storage service
        // For development, return the local file path
        return storagePath;
    }

    private UploadResult ConvertToUploadResult(StorageResult storageResult)
    {
        var uploadResult = new UploadResult
        {
            Success = storageResult.Success,
            Message = storageResult.Message,
            StoragePath = storageResult.StoragePath,
            FileName = storageResult.FileName,
            FileSize = storageResult.FileSize,
            FileType = storageResult.FileType,
            UploadSessionId = storageResult.UploadSessionId,
            UploadedAt = storageResult.UploadedAt,
            ErrorCode = storageResult.ErrorCode,
            ValidationErrors = storageResult.ValidationErrors
        };

        // Extract FileId from metadata if available
        if (storageResult.Metadata != null && storageResult.Metadata.TryGetValue("FileId", out var fileIdStr) && Guid.TryParse(fileIdStr, out var fileId))
        {
            uploadResult.FileId = fileId;
        }

        return uploadResult;
    }

    public Task<bool> ValidateFileAsync(IFormFile file)
    {
        var (isValid, errors) = FileUploadValidator.ValidateFile(file);
        return Task.FromResult(isValid);
    }

    public Task<string> GenerateUniqueFileNameAsync(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        return Task.FromResult($"{baseName}_{timestamp}_{random}{extension}");
    }

    public async Task<string> StoreFileAsync(IFormFile file, string fileName)
    {
        // For development: store in local temp directory
        // In production, this will be replaced with Azure Blob Storage integration
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

    public async Task<long> GetTotalUploadSizeForUserAsync(Guid userId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;
        
        return await _context.Documents
            .Where(d => d.UploadedById == userId && d.CreatedAt >= cutoffTime && !d.IsDeleted)
            .SumAsync(d => d.FileSize);
    }

    private async Task ExtractTextFromDocumentAsync(Document document, string storagePath)
    {
        try
        {
            // Update document status to processing
            document.Status = Core.Enums.DocumentStatus.Processing;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Starting text extraction for document: {DocumentId}", document.Id);

            // Check if text extraction is supported for this file type
            if (!await _textExtractionService.SupportsFileTypeAsync(document.OriginalFileName ?? ""))
            {
                _logger.LogWarning("Text extraction not supported for file type: {FileType}", document.FileType);
                document.Status = Core.Enums.DocumentStatus.Failed;
                await _context.SaveChangesAsync();
                return;
            }

            // Extract text using the text extraction service
            var extractionResult = await _textExtractionService.ExtractTextAsync(storagePath, document.OriginalFileName ?? "");

            if (extractionResult.Success && !string.IsNullOrEmpty(extractionResult.ExtractedText))
            {
                // Create DocumentText record
                var documentText = new DocumentText
                {
                    DocumentId = document.Id,
                    FullText = extractionResult.ExtractedText,
                    ConfidenceScore = extractionResult.ConfidenceScore,
                    PageCount = extractionResult.Pages?.Count ?? 1,
                    CharacterCount = extractionResult.ExtractedText.Length,
                    Language = "en", // Default to English
                    ExtractionMetadata = extractionResult.Metadata,
                    Pages = extractionResult.Pages
                };

                // Link the extracted text to the document
                document.ExtractedText = documentText;
                document.Status = Core.Enums.DocumentStatus.Processed;

                _logger.LogInformation("Text extraction completed for document: {DocumentId}. Extracted {CharCount} characters.", 
                    document.Id, extractionResult.ExtractedText.Length);
            }
            else
            {
                _logger.LogError("Text extraction failed for document: {DocumentId}. Error: {Error}. Status: {Status}, ProcessingTime: {ProcessingTime}ms", 
                    document.Id, extractionResult.ErrorMessage, extractionResult.Status, extractionResult.ProcessingTime.TotalMilliseconds);
                document.Status = Core.Enums.DocumentStatus.Failed;
                document.Metadata = new Dictionary<string, object>
                {
                    ["extraction_error"] = extractionResult.ErrorMessage ?? "Unknown error",
                    ["extraction_status"] = extractionResult.Status.ToString(),
                    ["processing_time_ms"] = extractionResult.ProcessingTime.TotalMilliseconds,
                    ["failed_at"] = DateTime.UtcNow
                };
                
                // Create audit log for OCR failure
                await CreateAuditLogAsync(
                    "OCR_FAILURE",
                    $"Text extraction failed for document {document.Id}: {extractionResult.ErrorMessage}",
                    "Document",
                    document.Id,
                    AuditLogLevel.Error
                );
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during text extraction for document: {DocumentId}. Exception type: {ExceptionType}", 
                document.Id, ex.GetType().Name);
            document.Status = Core.Enums.DocumentStatus.Failed;
            document.Metadata = new Dictionary<string, object>
            {
                ["extraction_error"] = $"Critical error: {ex.Message}",
                ["exception_type"] = ex.GetType().Name,
                ["exception_stack_trace"] = ex.StackTrace ?? "No stack trace",
                ["failed_at"] = DateTime.UtcNow
            };
            
            // Create audit log for critical OCR failure
            await CreateAuditLogAsync(
                "OCR_CRITICAL_FAILURE",
                $"Critical text extraction error for document {document.Id}: {ex.Message}",
                "Document",
                document.Id,
                AuditLogLevel.Critical
            );
            
            await _context.SaveChangesAsync();
        }
    }

    private async Task CreateAuditLogAsync(string action, string description, string entityType, Guid entityId, AuditLogLevel level)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                Level = level,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {Action}", action);
        }
    }
}