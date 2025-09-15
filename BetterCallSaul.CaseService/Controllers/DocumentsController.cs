using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.CaseService.Models.Entities;
using BetterCallSaul.CaseService.Repositories;
using BetterCallSaul.CaseService.Services.FileProcessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.CaseService.Controllers;

[ApiController]
[Route("api/cases/{caseId}/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentRepository documentRepository,
        IFileUploadService fileUploadService,
        ILogger<DocumentsController> logger)
    {
        _documentRepository = documentRepository;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDocument>>> GetDocuments(string caseId)
    {
        try
        {
            var documents = await _documentRepository.GetByCaseIdAsync(caseId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for case {CaseId}", caseId);
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDocument>> GetDocument(string caseId, string id)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null || document.CaseId != caseId)
            {
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId} for case {CaseId}", id, caseId);
            return StatusCode(500, "An error occurred while retrieving the document");
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadResult>> UploadDocument(string caseId, IFormFile file, [FromQuery] string userId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            var uploadSessionId = Guid.NewGuid().ToString();
            var result = await _fileUploadService.UploadFileAsync(file, caseId, userId, uploadSessionId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for case {CaseId}", caseId);
            return StatusCode(500, "An error occurred while uploading the document");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentDocument>> UpdateDocument(string caseId, string id, DocumentDocument document)
    {
        try
        {
            if (id != document.Id || caseId != document.CaseId)
            {
                return BadRequest("ID or CaseId mismatch");
            }

            if (!await _documentRepository.ExistsAsync(id))
            {
                return NotFound();
            }

            var updatedDocument = await _documentRepository.UpdateAsync(id, document);
            return Ok(updatedDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} for case {CaseId}", id, caseId);
            return StatusCode(500, "An error occurred while updating the document");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(string caseId, string id)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null || document.CaseId != caseId)
            {
                return NotFound();
            }

            var success = await _documentRepository.DeleteAsync(id);
            if (!success)
            {
                return StatusCode(500, "Failed to delete document");
            }

            // Also delete the physical file
            await _fileUploadService.DeleteFileAsync(document.StoragePath);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for case {CaseId}", id, caseId);
            return StatusCode(500, "An error occurred while deleting the document");
        }
    }
}