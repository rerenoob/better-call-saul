using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using BetterCallSaul.Infrastructure.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IFileUploadService fileUploadService, ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
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
            // Check if the current service is AzureBlobStorageService
            if (_fileUploadService is not AzureBlobStorageService azureStorageService)
            {
                return BadRequest(new { error = "SAS tokens are only available when using Azure Blob Storage" });
            }

            // Validate user has access to this blob (you would implement proper authorization here)
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Generate SAS token
            var expiryTime = TimeSpan.FromMinutes(expiryMinutes);
            var sasToken = await azureStorageService.GenerateSasTokenAsync(blobName, expiryTime);

            return Ok(new 
            {
                SasToken = sasToken,
                ExpiryMinutes = expiryMinutes,
                BlobName = blobName,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Blob '{blobName}' not found" });
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
}