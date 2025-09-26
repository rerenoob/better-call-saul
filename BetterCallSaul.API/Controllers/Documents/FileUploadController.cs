using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using BetterCallSaul.Infrastructure.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors("ReactFrontend")]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ICaseAnalysisService _caseAnalysisService;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly ILogger<FileUploadController> _logger;
    private readonly BetterCallSaulContext _context;
    private readonly IServiceProvider _serviceProvider;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ICaseAnalysisService caseAnalysisService,
        ICaseDocumentRepository caseDocumentRepository,
        ILogger<FileUploadController> logger,
        BetterCallSaulContext context,
        IServiceProvider serviceProvider)
    {
        _fileUploadService = fileUploadService;
        _caseAnalysisService = caseAnalysisService;
        _caseDocumentRepository = caseDocumentRepository;
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<UploadResult>> UploadFile(
        IFormFile file,
        [FromForm] Guid? caseId = null,
        [FromForm] string uploadSessionId = "")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new UploadResult 
                { 
                    Success = false, 
                    Message = "No file provided" 
                });
            }

            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new UploadResult
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            // File upload is now decoupled from case creation - NoSQL-first approach

            // Validate file
            var validationResult = FileUploadValidator.ValidateFile(file);
            if (!validationResult.IsValid)
            {
                return BadRequest(new UploadResult
                {
                    Success = false,
                    Message = "File validation failed",
                    ValidationErrors = validationResult.Errors
                });
            }

            // Upload file without case assignment using NoSQL-first approach
            UploadResult result;
            if (caseId.HasValue && caseId != Guid.Empty)
            {
                // Upload and immediately link to case
                result = await _fileUploadService.UploadFileAsync(file, caseId.Value, userId, uploadSessionId);

                if (result.Success)
                {
                    // Trigger automatic case analysis for assigned documents
                    _ = Task.Run(async () => await TriggerCaseAnalysisAsync(caseId.Value, result.FileId, file.FileName));
                }
            }
            else
            {
                // Upload without case assignment (NoSQL-first approach)
                result = await _fileUploadService.UploadFileAsync(file, userId, uploadSessionId);
            }

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, new UploadResult 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    [HttpPost("validate")]
    public ActionResult<Dictionary<string, string>> ValidateFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new Dictionary<string, string> 
                { 
                    ["file"] = "No file provided" 
                });
            }

            var (isValid, errors) = FileUploadValidator.ValidateFile(file);
            
            if (isValid)
            {
                return Ok(new Dictionary<string, string>());
            }
            else
            {
                return BadRequest(errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file: {FileName}", file?.FileName);
            return StatusCode(500, new Dictionary<string, string> 
            { 
                ["error"] = $"Internal server error: {ex.Message}" 
            });
        }
    }

    [HttpGet("limits/{userId:guid}")]
    public async Task<ActionResult<object>> GetUploadLimits(Guid userId)
    {
        try
        {
            var hourlyUsage = await _fileUploadService.GetTotalUploadSizeForUserAsync(userId, TimeSpan.FromHours(1));
            var dailyUsage = await _fileUploadService.GetTotalUploadSizeForUserAsync(userId, TimeSpan.FromHours(24));

            return Ok(new 
            {
                HourlyUsage = hourlyUsage,
                DailyUsage = dailyUsage,
                HourlyLimit = 500 * 1024 * 1024, // 500MB
                DailyLimit = 2000 * 1024 * 1024  // 2GB
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload limits for user: {UserId}", userId);
            return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpGet("sas-token/{blobName}")]
    public Task<ActionResult<object>> GenerateSasToken(string blobName, [FromQuery] int expiryMinutes = 60)
    {
        try
        {
            // SAS tokens were only available with cloud blob storage
            // This feature is not available in the current AWS/Local configuration
            return Task.FromResult<ActionResult<object>>(BadRequest(new { error = "SAS tokens are not available in the current storage configuration." }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS token for blob: {BlobName}", blobName);
            return Task.FromResult<ActionResult<object>>(StatusCode(500, new { error = $"Internal server error: {ex.Message}" }));
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


    private async Task TriggerCaseAnalysisAsync(Guid caseId, Guid documentId, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting automatic case analysis for case {CaseId}, document {DocumentId}", caseId, documentId);

            // Use a new scope to avoid ObjectDisposedException with background tasks
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BetterCallSaulContext>();
            var caseAnalysisService = scope.ServiceProvider.GetRequiredService<ICaseAnalysisService>();

            // Get document from SQL for basic info
            var document = await context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found, skipping analysis", documentId);
                return;
            }

            // Get document content from NoSQL
            var caseDocumentRepo = scope.ServiceProvider.GetRequiredService<ICaseDocumentRepository>();
            var caseDocument = await caseDocumentRepo.GetByIdAsync(caseId);
            var documentInfo = caseDocument?.Documents.FirstOrDefault(d => d.Id == documentId);

            if (documentInfo?.ExtractedText?.FullText == null)
            {
                _logger.LogWarning("No extracted text available for document {DocumentId}, skipping analysis", documentId);
                return;
            }

            // Trigger the case analysis
            await caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, documentInfo.ExtractedText.FullText);

            _logger.LogInformation("Successfully triggered automatic case analysis for case {CaseId}, document {DocumentId}", caseId, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic case analysis for case {CaseId}, document {DocumentId}", caseId, documentId);
        }
    }

    [HttpOptions]
    [HttpOptions("upload")]
    [HttpOptions("validate")]
    [HttpOptions("limits/{userId:guid}")]
    [HttpOptions("sas-token/{blobName}")]
    public IActionResult OptionsHandler()
    {
        return Ok();
    }
}