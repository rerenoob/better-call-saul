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

    public async Task<UploadResult> UploadFileAsync(IFormFile file, Guid userId, string uploadSessionId)
    {
        var storageResult = await UploadFileToStorageAsync(file, userId, uploadSessionId, null);
        return ConvertToUploadResult(storageResult);
    }

    public async Task<UploadResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var storageResult = await UploadFileToStorageAsync(file, userId, uploadSessionId, caseId);
        return ConvertToUploadResult(storageResult);
    }

    async Task<StorageResult> IStorageService.UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        return await UploadFileToStorageAsync(file, userId, uploadSessionId, caseId);
    }

    public async Task<StorageResult> UploadFileToStorageAsync(IFormFile file, Guid userId, string uploadSessionId, Guid? caseId = null)
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

            // Validate user exists to prevent foreign key constraint violations
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                result.Success = false;
                result.Message = "Invalid user - user not found or inactive";
                result.ErrorCode = "INVALID_USER";
                return result;
            }

            // Generate unique filename
            var uniqueFileName = await GenerateUniqueFileNameAsync(file.FileName);

            // Store file using configured storage service (Local or AWS S3)
            var storagePath = await StoreFileAsync(file, uniqueFileName);

            // Create document record with case assignment if provided
            var document = new Document
            {
                FileName = uniqueFileName,
                FileType = Path.GetExtension(file.FileName).ToLowerInvariant(),
                FileSize = file.Length,
                CaseId = caseId, // Assign case if provided, otherwise null
                UploadedById = userId,
                Status = Core.Enums.DocumentStatus.Uploaded
            };

            // Create the minimal SQL record with better error handling
            try
            {
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Database error saving document: {FileName}. User ID: {UserId}. Inner Exception: {InnerException}",
                    file.FileName, userId, dbEx.InnerException?.Message);

                // Clean up uploaded file if database save failed
                await DeleteFileAsync(storagePath);

                result.Success = false;
                result.Message = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                result.ErrorCode = "DATABASE_ERROR";
                return result;
            }

            try
            {
                // Perform text extraction and store everything in NoSQL (NoSQL-first approach)
                await ProcessDocumentWithTextExtractionAsync(document, file.FileName, storagePath, userId);

                _logger.LogInformation("Document {DocumentId} processed using NoSQL-first approach (case assigned: {CaseAssigned})", document.Id, caseId.HasValue && caseId != Guid.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during NoSQL-first document processing: {FileName}", file.FileName);

                // Update SQL status to failed for tracking
                document.Status = Core.Enums.DocumentStatus.Failed;
                await _context.SaveChangesAsync();

                // Clean up uploaded file if processing failed
                await DeleteFileAsync(storagePath);

                throw;
            }

            result.Success = true;
            result.FileName = document.FileName;
            result.FileSize = document.FileSize;
            result.FileType = document.FileType;
            result.Message = "File uploaded and text extracted successfully";

            // Add FileId to metadata for database reference
            result.Metadata ??= new Dictionary<string, string>();
            result.Metadata["FileId"] = document.Id.ToString();
            result.Metadata["TextExtractionComplete"] = "true"; // Flag for AI analysis
            result.FileId = document.Id; // Set FileId for linking to cases later

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

    public Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
    {
        // For local file storage, we don't generate secure URLs
        // In production, this would be handled by a proper storage service
        // For development, return the local file path
        return Task.FromResult(storagePath);
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

    private async Task ProcessDocumentWithTextExtractionAsync(Document document, string originalFileName, string storagePath, Guid userId)
    {
        try
        {
            // Update SQL document status to processing
            document.Status = Core.Enums.DocumentStatus.Processing;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Starting NoSQL-first text extraction for document: {DocumentId}", document.Id);

            // Create or get the appropriate case document collection
            var userDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
            var targetCaseId = document.CaseId ?? Guid.Empty; // Use actual case ID or empty for unassigned
            var caseDocument = userDocuments.FirstOrDefault(cd => cd.CaseId == targetCaseId);
            if (caseDocument == null)
            {
                caseDocument = new CaseDocument
                {
                    CaseId = targetCaseId, // Use actual case ID or empty for unassigned
                    UserId = userId
                };
            }

            // Create comprehensive document info for NoSQL with all details
            var documentInfo = new DocumentInfo
            {
                Id = document.Id,
                FileName = document.FileName,
                OriginalFileName = originalFileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                StoragePath = storagePath,
                Type = DocumentType.Other,
                Status = DocumentStatus.Processing,
                IsProcessed = false,
                UploadedById = document.UploadedById,
                CreatedAt = document.CreatedAt,
                Metadata = new Dictionary<string, object>(),
                ProcessingMetadata = new TextProcessingMetadata()
            };

            // Check if text extraction is supported for this file type
            if (!await _textExtractionService.SupportsFileTypeAsync(originalFileName))
            {
                _logger.LogWarning("Text extraction not supported for file type: {FileType}", document.FileType);

                documentInfo.Status = DocumentStatus.Processed; // Mark as processed since we can't extract text
                documentInfo.IsProcessed = true;
                documentInfo.ProcessedAt = DateTime.UtcNow;
                documentInfo.ProcessingMetadata.ProcessingFlags.Add("text_extraction_not_supported");

                document.Status = DocumentStatus.Processed;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Extract text using the text extraction service
                var extractionResult = await _textExtractionService.ExtractTextAsync(storagePath, originalFileName);

                if (extractionResult.Success && !string.IsNullOrEmpty(extractionResult.ExtractedText))
                {
                    // Store extracted text directly in NoSQL
                    documentInfo.ExtractedText = new DocumentTextInfo
                    {
                        Id = Guid.NewGuid(),
                        FullText = extractionResult.ExtractedText,
                        ConfidenceScore = extractionResult.ConfidenceScore,
                        PageCount = extractionResult.Pages?.Count ?? 1,
                        CharacterCount = extractionResult.ExtractedText.Length,
                        Language = "en", // Default to English
                        ExtractionMetadata = extractionResult.Metadata,
                        Pages = extractionResult.Pages?.Select(p => new TextPageInfo
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
                        CreatedAt = DateTime.UtcNow
                    };

                    // Set processing metadata
                    documentInfo.ProcessingMetadata.ExtractionMethod = "AWS_Textract";
                    documentInfo.ProcessingMetadata.QualityScore = extractionResult.ConfidenceScore;
                    documentInfo.ProcessingMetadata.ProcessingVersion = "1.0";
                    documentInfo.ProcessingMetadata.LanguageDetected = "en";

                    documentInfo.Status = DocumentStatus.Processed;
                    documentInfo.IsProcessed = true;
                    documentInfo.ProcessedAt = DateTime.UtcNow;

                    document.Status = DocumentStatus.Processed;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Text extraction completed for document: {DocumentId}. Extracted {CharCount} characters.",
                        document.Id, extractionResult.ExtractedText.Length);
                }
                else
                {
                    _logger.LogError("CRITICAL: Text extraction failed for document: {DocumentId} (File: {FileName}). Error: {Error}. Status: {Status}, ProcessingTime: {ProcessingTime}ms. This will cause AI analysis to fail.",
                        document.Id, document.FileName, extractionResult.ErrorMessage, extractionResult.Status, extractionResult.ProcessingTime.TotalMilliseconds);

                    // Store failure information in NoSQL
                    documentInfo.Status = DocumentStatus.Failed;
                    documentInfo.Metadata = new Dictionary<string, object>
                    {
                        ["extraction_error"] = extractionResult.ErrorMessage ?? "Unknown error",
                        ["extraction_status"] = extractionResult.Status.ToString(),
                        ["processing_time_ms"] = extractionResult.ProcessingTime.TotalMilliseconds,
                        ["failed_at"] = DateTime.UtcNow,
                        ["file_type"] = document.FileType,
                        ["file_size"] = document.FileSize
                    };

                    documentInfo.ProcessingMetadata.ProcessingFlags.Add("extraction_failed");
                    documentInfo.ProcessingMetadata.ProcessingFlags.Add("ai_analysis_will_fail");

                    document.Status = DocumentStatus.Failed;
                    await _context.SaveChangesAsync();

                    // Create audit log for OCR failure
                    await CreateAuditLogAsync(
                        "OCR_FAILURE",
                        $"Text extraction failed for document {document.Id}: {extractionResult.ErrorMessage}",
                        "Document",
                        document.Id,
                        AuditLogLevel.Error
                    );
                }
            }

            // Store in NoSQL (primary storage for document content)
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

            _logger.LogInformation("Stored document {DocumentId} with extracted text in NoSQL for case {CaseId}", document.Id, targetCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during NoSQL-first processing for document: {DocumentId}. Exception type: {ExceptionType}",
                document.Id, ex.GetType().Name);

            document.Status = DocumentStatus.Failed;
            await _context.SaveChangesAsync();

            // Create audit log for critical processing failure
            await CreateAuditLogAsync(
                "DOCUMENT_PROCESSING_CRITICAL_FAILURE",
                $"Critical document processing error for document {document.Id}: {ex.Message}",
                "Document",
                document.Id,
                AuditLogLevel.Critical
            );

            throw; // Re-throw to trigger cleanup in calling method
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

    /// <summary>
    /// Links an uploaded document to a case after upload
    /// </summary>
    public async Task<bool> LinkDocumentToCaseAsync(Guid documentId, Guid caseId)
    {
        try
        {
            // Update SQL document
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for case linking", documentId);
                return false;
            }

            // Verify case exists
            var caseExists = await _context.Cases.AnyAsync(c => c.Id == caseId);
            if (!caseExists)
            {
                _logger.LogWarning("Case {CaseId} not found for document linking", caseId);
                return false;
            }

            document.CaseId = caseId;
            document.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Move document in NoSQL from unassigned to case-specific collection
            await MoveDocumentToCase(documentId, caseId, document.UploadedById ?? Guid.Empty);

            _logger.LogInformation("Successfully linked document {DocumentId} to case {CaseId}", documentId, caseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking document {DocumentId} to case {CaseId}", documentId, caseId);
            return false;
        }
    }

    private async Task MoveDocumentToCase(Guid documentId, Guid caseId, Guid userId)
    {
        try
        {
            // Get document from unassigned collection
            var userDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
            var unassignedCollection = userDocuments.FirstOrDefault(cd => cd.CaseId == Guid.Empty);
            if (unassignedCollection == null)
            {
                _logger.LogWarning("No unassigned document collection found for user {UserId}", userId);
                return;
            }

            var documentInfo = unassignedCollection.Documents.FirstOrDefault(d => d.Id == documentId);
            if (documentInfo == null)
            {
                _logger.LogWarning("Document {DocumentId} not found in unassigned collection", documentId);
                return;
            }

            // Remove from unassigned collection
            unassignedCollection.Documents.Remove(documentInfo);
            unassignedCollection.Metadata.TotalDocuments = unassignedCollection.Documents.Count;
            await _caseDocumentRepository.UpdateAsync(unassignedCollection);

            // Add to case collection
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(caseId);
            if (caseDocument == null)
            {
                caseDocument = new CaseDocument
                {
                    CaseId = caseId,
                    UserId = userId
                };
            }

            caseDocument.Documents.Add(documentInfo);
            caseDocument.Metadata.TotalDocuments = caseDocument.Documents.Count;
            caseDocument.UpdatedAt = DateTime.UtcNow;

            if (caseDocument.Id == default)
            {
                await _caseDocumentRepository.CreateAsync(caseDocument);
            }
            else
            {
                await _caseDocumentRepository.UpdateAsync(caseDocument);
            }

            _logger.LogInformation("Moved document {DocumentId} from unassigned to case {CaseId}", documentId, caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving document {DocumentId} to case {CaseId}", documentId, caseId);
        }
    }
}