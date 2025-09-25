using BetterCallSaul.Core.Models.Entities;
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
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IFileUploadService fileUploadService,
        BetterCallSaulContext context,
        ILogger<DocumentsController> logger)
    {
        _fileUploadService = fileUploadService;
        _context = context;
        _logger = logger;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.ExtractedText)
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

            // Delete the file from storage
            var deleteResult = await _fileUploadService.DeleteFileAsync(document.StoragePath ?? "");
            if (!deleteResult)
            {
                _logger.LogWarning("Failed to delete file from storage: {StoragePath}", document.StoragePath);
                // Continue with database deletion anyway
            }

            // Delete extracted text if it exists
            if (document.ExtractedText != null)
            {
                _context.Set<DocumentText>().Remove(document.ExtractedText);
            }

            // Delete the document
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
                .Include(d => d.ExtractedText)
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

            return Ok(new
            {
                Id = document.Id,
                FileName = document.FileName,
                FileSize = document.FileSize,
                FileType = document.FileType,
                Status = document.Status.ToString(),
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                HasExtractedText = document.ExtractedText != null
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