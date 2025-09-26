using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IFileUploadService fileUploadService,
        ICaseDocumentRepository caseDocumentRepository,
        BetterCallSaulContext context,
        ILogger<DocumentsController> logger)
    {
        _fileUploadService = fileUploadService;
        _caseDocumentRepository = caseDocumentRepository;
        _context = context;
        _logger = logger;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Verify user owns the document
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty || document.UploadedById != userId)
            {
                return Unauthorized(new { message = "Unauthorized to delete this document" });
            }

            // Get document info from NoSQL to get storage path
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(document.CaseId);
            var documentInfo = caseDocument?.Documents.FirstOrDefault(d => d.Id == id);

            // Delete the file from storage
            if (documentInfo?.StoragePath != null)
            {
                var deleteResult = await _fileUploadService.DeleteFileAsync(documentInfo.StoragePath);
                if (!deleteResult)
                {
                    _logger.LogWarning("Failed to delete file from storage: {StoragePath}", documentInfo.StoragePath);
                    // Continue with database deletion anyway
                }
            }

            // Delete from NoSQL first
            if (caseDocument != null)
            {
                var docToRemove = caseDocument.Documents.FirstOrDefault(d => d.Id == id);
                if (docToRemove != null)
                {
                    caseDocument.Documents.Remove(docToRemove);
                    caseDocument.UpdatedAt = DateTime.UtcNow;
                    caseDocument.Metadata.TotalDocuments = caseDocument.Documents.Count;
                    await _caseDocumentRepository.UpdateAsync(caseDocument);
                }
            }

            // Delete from SQL
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} deleted successfully", id);
            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Verify user owns the document
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty || document.UploadedById != userId)
            {
                return Unauthorized(new { message = "Unauthorized to access this document" });
            }

            // Get document info from NoSQL for text status
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(document.CaseId);
            var documentInfo = caseDocument?.Documents.FirstOrDefault(d => d.Id == id);

            return Ok(new
            {
                Id = document.Id,
                FileName = document.FileName,
                FileSize = document.FileSize,
                FileType = document.FileType,
                Status = document.Status.ToString(),
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                HasExtractedText = documentInfo?.ExtractedText != null,
                IsProcessed = documentInfo?.IsProcessed ?? false,
                ProcessedAt = documentInfo?.ProcessedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}