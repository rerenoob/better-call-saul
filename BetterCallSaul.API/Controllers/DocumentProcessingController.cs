using BetterCallSaul.Core.Models;
using BetterCallSaul.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentProcessingController : ControllerBase
{
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<DocumentProcessingController> _logger;

    public DocumentProcessingController(
        ITextExtractionService textExtractionService,
        ILogger<DocumentProcessingController> logger)
    {
        _textExtractionService = textExtractionService;
        _logger = logger;
    }

    [HttpPost("extract-text")]
    public async Task<ActionResult<TextExtractionResult>> ExtractText([FromForm] Guid documentId, [FromForm] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return BadRequest(new TextExtractionResult 
                { 
                    Success = false, 
                    ErrorMessage = "File not found or invalid path" 
                });
            }

            var fileName = Path.GetFileName(filePath);
            var result = await _textExtractionService.ExtractTextAsync(filePath, fileName);

            if (result.Success)
            {
                _logger.LogInformation("Text extracted successfully from document {DocumentId}", documentId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Text extraction failed for document {DocumentId}: {Error}", 
                    documentId, result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text for document {DocumentId}", documentId);
            return StatusCode(500, new TextExtractionResult 
            { 
                Success = false, 
                ErrorMessage = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpPost("process-document")]
    public async Task<ActionResult<DocumentText>> ProcessDocument([FromForm] Guid documentId, [FromForm] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return BadRequest(new { error = "File not found or invalid path" });
            }

            var result = await _textExtractionService.ProcessDocumentAsync(filePath, documentId);

            _logger.LogInformation("Document processed successfully: {DocumentId}", documentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpGet("supported-formats")]
    public async Task<ActionResult<IEnumerable<string>>> GetSupportedFormats()
    {
        try
        {
            var supportedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            return Ok(supportedExtensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported formats");
            return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpPost("validate-format/{fileName}")]
    public async Task<ActionResult<bool>> ValidateFileFormat(string fileName)
    {
        try
        {
            var isSupported = await _textExtractionService.SupportsFileTypeAsync(fileName);
            return Ok(isSupported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file format: {FileName}", fileName);
            return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
        }
    }
}