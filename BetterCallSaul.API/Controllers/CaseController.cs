using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaseController : ControllerBase
{
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<CaseController> _logger;

    public CaseController(IAzureOpenAIService aiService, ILogger<CaseController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Case>> GetCase(Guid id)
    {
        // Mock data for now - in real implementation, this would fetch from database
        var mockCase = new Case
        {
            Id = id,
            CaseNumber = "2024-CR-001",
            Title = "State v. Johnson",
            Description = "Client: Michael Johnson. The case against Michael Johnson relies heavily on circumstantial evidence and a single, uncorroborated witness testimony.",
            Status = CaseStatus.PreTrial,
            Type = CaseType.Criminal,
            Court = "Superior Court",
            Judge = "Hon. Smith",
            FiledDate = DateTime.UtcNow.AddDays(-30),
            HearingDate = DateTime.UtcNow.AddDays(15),
            TrialDate = DateTime.UtcNow.AddDays(60),
            SuccessProbability = 0.82m,
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        return Ok(mockCase);
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
        // Mock data for now
        var mockCases = new[]
        {
            new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = "2024-CR-001",
                Title = "State v. Johnson",
                Description = "Client: Michael Johnson",
                Status = CaseStatus.PreTrial,
                Type = CaseType.Criminal,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = "2024-CR-002", 
                Title = "State v. Chen",
                Description = "Client: Wei Chen",
                Status = CaseStatus.Investigation,
                Type = CaseType.Criminal,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        return Ok(mockCases);
    }

    [HttpGet("statistics")]
    public ActionResult GetStatistics()
    {
        var statistics = new
        {
            Total = 15,
            Active = 8,
            Completed = 5,
            Overdue = 2,
            ByStatus = new Dictionary<string, int>
            {
                { "New", 2 },
                { "Investigation", 3 },
                { "PreTrial", 5 },
                { "Trial", 2 },
                { "Closed", 3 }
            }
        };

        return Ok(statistics);
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}