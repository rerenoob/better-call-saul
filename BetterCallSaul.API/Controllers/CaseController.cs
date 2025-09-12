using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaseController : ControllerBase
{
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<CaseController> _logger;
    private readonly BetterCallSaulContext _context;

    public CaseController(IAzureOpenAIService aiService, ILogger<CaseController> logger, BetterCallSaulContext context)
    {
        _aiService = aiService;
        _logger = logger;
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Case>> GetCase(Guid id)
    {
        var caseItem = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == id);

        if (caseItem == null)
        {
            return NotFound();
        }

        return Ok(caseItem);
    }

    [HttpPost("{id}/chat")]
    public async Task<ActionResult<AIResponse>> ChatWithAI(Guid id, [FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing AI chat request for case {CaseId}", id);

            var aiRequest = new AIRequest
            {
                DocumentText = request.Message,
                CaseContext = $"Case ID: {id}. User is asking: {request.Message}",
                MaxTokens = 500,
                Temperature = 0.7
            };

            var response = await _aiService.AnalyzeCaseAsync(aiRequest);

            if (!response.Success)
            {
                return BadRequest(new { error = response.ErrorMessage });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI chat request for case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Case>>> GetCases()
    {
        var cases = await _context.Cases
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(cases);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var total = await _context.Cases.CountAsync();
        var active = await _context.Cases.CountAsync(c => c.Status != CaseStatus.Closed && c.Status != CaseStatus.Dismissed && c.Status != CaseStatus.Settlement);
        var completed = await _context.Cases.CountAsync(c => c.Status == CaseStatus.Closed || c.Status == CaseStatus.Settlement);
        
        // For overdue cases, you might want to check hearing dates or other deadlines
        var overdue = await _context.Cases.CountAsync(c => c.HearingDate.HasValue && c.HearingDate < DateTime.UtcNow && c.Status != CaseStatus.Closed);

        var byStatus = await _context.Cases
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

        var statistics = new
        {
            Total = total,
            Active = active,
            Completed = completed,
            Overdue = overdue,
            ByStatus = byStatus
        };

        return Ok(statistics);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<Case>>> GetRecentCases([FromQuery] int limit = 10)
    {
        var recentCases = await _context.Cases
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(recentCases);
    }

    [HttpPost("create-with-files")]
    public async Task<ActionResult<CaseCreationResponse>> CreateCaseWithFiles([FromBody] CreateCaseWithFilesRequest request)
    {
        try
        {
            _logger.LogInformation("Creating case with files: {Title}", request.Title);

            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new CaseCreationResponse
                {
                    Success = false,
                    CaseId = string.Empty,
                    Error = "User not authenticated"
                });
            }

            // Create the case
            var caseItem = new Case
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                UserId = userId,
                CaseNumber = $"CASE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = CaseStatus.New
            };

            _context.Cases.Add(caseItem);

            // If files are provided, associate them with the case
            if (request.FileIds != null && request.FileIds.Count > 0)
            {
                var documents = await _context.Documents
                    .Where(d => request.FileIds.Contains(d.Id.ToString()))
                    .ToListAsync();

                foreach (var document in documents)
                {
                    document.CaseId = caseItem.Id;
                    document.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created case {CaseId} with {FileCount} files", 
                caseItem.Id, request.FileIds?.Count ?? 0);

            return Ok(new CaseCreationResponse
            {
                Success = true,
                CaseId = caseItem.Id.ToString(),
                Message = "Case created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating case with files");
            return StatusCode(500, new CaseCreationResponse
            {
                Success = false,
                CaseId = string.Empty,
                Error = "Internal server error"
            });
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

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class CreateCaseWithFilesRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> FileIds { get; set; } = new();
}

public class CaseCreationResponse
{
    public bool Success { get; set; }
    public string CaseId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Error { get; set; }
}