using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces.Services;
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
    private readonly ILogger<FileUploadController> _logger;
    private readonly BetterCallSaulContext _context;
    private readonly IServiceProvider _serviceProvider;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ICaseAnalysisService caseAnalysisService,
        ILogger<FileUploadController> logger,
        BetterCallSaulContext context,
        IServiceProvider serviceProvider)
    {
        _fileUploadService = fileUploadService;
        _caseAnalysisService = caseAnalysisService;
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<UploadResult>> UploadFile(
        IFormFile file,
        [FromForm] Guid caseId,
        [FromForm] string uploadSessionId)
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

            // Handle temporary case uploads - create a temporary case if needed
            if (caseId == Guid.Empty)
            {
                caseId = await GetOrCreateTemporaryCaseAsync(userId);
            }

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

            // Upload file
            var result = await _fileUploadService.UploadFileAsync(file, caseId, userId, uploadSessionId);

            if (result.Success)
            {
                // Trigger automatic case analysis if we have a document ID and it's not a temporary case
                // Analysis will check if text is available and proceed accordingly
                if (result.FileId != Guid.Empty && !await IsTemporaryCaseAsync(caseId))
                {
                    _ = Task.Run(async () => await TriggerCaseAnalysisAsync(caseId, result.FileId, file.FileName));
                }

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

    private async Task<Guid> GetOrCreateTemporaryCaseAsync(Guid userId)
    {
        // Try to find existing temporary case for this user created in the last hour
        var temporaryCase = await _context.Cases
            .Where(c => c.UserId == userId &&
                       c.Title == "TEMPORARY_UPLOAD_CASE" &&
                       c.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .FirstOrDefaultAsync();

        if (temporaryCase != null)
        {
            return temporaryCase.Id;
        }

        // Create new temporary case
        var newTempCase = new Case
        {
            Id = Guid.NewGuid(),
            Title = "TEMPORARY_UPLOAD_CASE",
            Description = "Temporary case for file uploads before case creation",
            UserId = userId,
            CaseNumber = $"TEMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Status = CaseStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Cases.Add(newTempCase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created temporary case {CaseId} for user {UserId}", newTempCase.Id, userId);

        return newTempCase.Id;
    }

    private async Task<bool> IsTemporaryCaseAsync(Guid caseId)
    {
        var caseItem = await _context.Cases.FirstOrDefaultAsync(c => c.Id == caseId);
        return caseItem?.Title == "TEMPORARY_UPLOAD_CASE";
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

            // With database transactions, text extraction is guaranteed to be complete
            // Get the document with extracted text directly
            var document = await context.Documents
                .Include(d => d.ExtractedText)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document?.ExtractedText?.FullText == null)
            {
                _logger.LogWarning("No extracted text available for document {DocumentId}, skipping analysis", documentId);
                return;
            }

            // Trigger the case analysis
            await caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, document.ExtractedText.FullText);

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