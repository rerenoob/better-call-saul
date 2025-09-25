using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Enums;
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
    private readonly ILogger<FileUploadController> _logger;
    private readonly BetterCallSaulContext _context;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger,
        BetterCallSaulContext context)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
        _context = context;
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
    public async Task<ActionResult<object>> GenerateSasToken(string blobName, [FromQuery] int expiryMinutes = 60)
    {
        try
        {
            // SAS tokens were only available with cloud blob storage
            // This feature is not available in the current AWS/Local configuration
            return BadRequest(new { error = "SAS tokens are not available in the current storage configuration." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS token for blob: {BlobName}", blobName);
            return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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