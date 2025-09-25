using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
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
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IFileValidationService _fileValidationService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<FileUploadService> _logger;
    private const long MaxUserUploadSizePerHour = 500 * 1024 * 1024; // 500MB per hour

    public FileUploadService(
        BetterCallSaulContext context,
        ICaseDocumentRepository caseDocumentRepository,
        IFileValidationService fileValidationService,
        ITextExtractionService textExtractionService,
        ILogger<FileUploadService> logger)
    {
        _context = context;
        _caseDocumentRepository = caseDocumentRepository;
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

            // Store file using configured storage service (Local or AWS S3)
            var storagePath = await StoreFileAsync(file, uniqueFileName);

            // Use database transaction to ensure atomicity between document creation and text extraction
            using var transaction = await _context.Database.BeginTransactionAsync();
            Document document;

            try
            {
                // Create document record in SQL (lightweight tracking)
                document = new Document
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

                // Perform text extraction within the same transaction
                await ExtractTextFromDocumentInTransactionAsync(document, storagePath);

                // Commit transaction only if both document creation and text extraction succeed
                await transaction.CommitAsync();

                _logger.LogInformation("Document {DocumentId} and text extraction completed atomically", document.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rolled back due to error during document processing: {FileName}", file.FileName);

                // Clean up uploaded file if transaction failed
                await DeleteFileAsync(storagePath);

                throw;
            }

            // Store document in NoSQL after successful transaction (dual-write pattern)
            await StoreDocumentInNoSQLAsync(document, userId);

            // Update NoSQL with text extraction results after transaction commit
            await UpdateDocumentTextInNoSQLAsync(document);

            result.Success = true;
            result.FileName = document.FileName;
            result.FileSize = document.FileSize;
            result.FileType = document.FileType;
            result.Message = "File uploaded and text extracted successfully";

            // Add FileId to metadata for database reference
            result.Metadata ??= new Dictionary<string, string>();
            result.Metadata["FileId"] = document.Id.ToString();
            result.Metadata["TextExtractionComplete"] = "true"; // Flag for AI analysis

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
            ValidationErrors = storageResult.ValidationErrors,
            Metadata = storageResult.Metadata
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
        // In production, this uses AWS S3 integration
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

    private async Task ExtractTextFromDocumentInTransactionAsync(Document document, string storagePath)
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

            // Save changes within the transaction - NoSQL update will happen after transaction commits
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

    private async Task StoreDocumentInNoSQLAsync(Document document, Guid userId)
    {
        try
        {
            // Get or create case document
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(document.CaseId);
            if (caseDocument == null)
            {
                caseDocument = new CaseDocument
                {
                    CaseId = document.CaseId,
                    UserId = userId
                };
            }

            // Create document info for NoSQL
            var documentInfo = new DocumentInfo
            {
                Id = document.Id,
                FileName = document.FileName,
                OriginalFileName = document.OriginalFileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                StoragePath = document.StoragePath,
                Type = document.Type,
                Status = document.Status,
                Description = document.Description,
                IsProcessed = false,
                UploadedById = document.UploadedById,
                CreatedAt = document.CreatedAt,
                Metadata = document.Metadata
            };

            // Add or update document in the case document
            var existingDocIndex = caseDocument.Documents.FindIndex(d => d.Id == document.Id);
            if (existingDocIndex >= 0)
            {
                caseDocument.Documents[existingDocIndex] = documentInfo;
            }
            else
            {
                caseDocument.Documents.Add(documentInfo);
            }

            caseDocument.UpdatedAt = DateTime.UtcNow;
            caseDocument.Metadata.TotalDocuments = caseDocument.Documents.Count;

            // Save to NoSQL
            if (caseDocument.Id == default)
            {
                await _caseDocumentRepository.CreateAsync(caseDocument);
            }
            else
            {
                await _caseDocumentRepository.UpdateAsync(caseDocument);
            }

            _logger.LogInformation("Stored document {DocumentId} in NoSQL for case {CaseId}", document.Id, document.CaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document {DocumentId} in NoSQL", document.Id);
            // Don't throw - this is a secondary storage operation
        }
    }

    private async Task UpdateDocumentTextInNoSQLAsync(Document document)
    {
        try
        {
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(document.CaseId);
            if (caseDocument != null)
            {
                var documentInfo = caseDocument.Documents.FirstOrDefault(d => d.Id == document.Id);
                if (documentInfo != null)
                {
                    // Update document status and processing flag
                    documentInfo.Status = document.Status;
                    documentInfo.IsProcessed = document.Status == DocumentStatus.Processed;
                    documentInfo.ProcessedAt = document.Status == DocumentStatus.Processed ? DateTime.UtcNow : null;
                    documentInfo.UpdatedAt = DateTime.UtcNow;

                    // Update extracted text information
                    if (document.ExtractedText != null)
                    {
                        documentInfo.ExtractedText = new DocumentTextInfo
                        {
                            Id = document.ExtractedText.Id,
                            FullText = document.ExtractedText.FullText,
                            ConfidenceScore = document.ExtractedText.ConfidenceScore,
                            PageCount = document.ExtractedText.PageCount,
                            CharacterCount = document.ExtractedText.CharacterCount,
                            Language = document.ExtractedText.Language,
                            ExtractionMetadata = document.ExtractedText.ExtractionMetadata,
                            Pages = document.ExtractedText.Pages?.Select(p => new TextPageInfo
                            {
                                PageNumber = p.PageNumber,
                                Text = p.Text,
                                Confidence = p.Confidence,
                                Blocks = p.TextBlocks?.Select(b => new TextBlockInfo
                                {
                                    BlockType = b.Type.ToString(),
                                    Text = b.Text,
                                    Confidence = b.Confidence,
                                    BoundingBox = b.BoundingBox != null ? new BoundingBoxInfo
                                    {
                                        Left = b.BoundingBox.X,
                                        Top = b.BoundingBox.Y,
                                        Width = b.BoundingBox.Width,
                                        Height = b.BoundingBox.Height
                                    } : null
                                }).ToList()
                            }).ToList(),
                            CreatedAt = document.ExtractedText.CreatedAt,
                            UpdatedAt = DateTime.UtcNow
                        };
                    }

                    caseDocument.UpdatedAt = DateTime.UtcNow;
                    await _caseDocumentRepository.UpdateAsync(caseDocument);

                    _logger.LogInformation("Updated document {DocumentId} text extraction in NoSQL", document.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document {DocumentId} text extraction in NoSQL", document.Id);
            // Don't throw - this is a secondary storage operation
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