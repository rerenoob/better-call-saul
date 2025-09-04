using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaseMatchingController : ControllerBase
{
    private readonly ICaseMatchingService _caseMatchingService;
    private readonly ILogger<CaseMatchingController> _logger;

    public CaseMatchingController(
        ICaseMatchingService caseMatchingService,
        ILogger<CaseMatchingController> logger)
    {
        _caseMatchingService = caseMatchingService;
        _logger = logger;
    }

    [HttpGet("similar/{caseId}")]
    public async Task<IActionResult> FindSimilarCases(
        Guid caseId,
        [FromQuery] string? jurisdiction = null,
        [FromQuery] int limit = 10,
        [FromQuery] decimal minSimilarity = 0.6m)
    {
        try
        {
            var matches = await _caseMatchingService.FindSimilarCasesAsync(
                caseId, jurisdiction, limit, minSimilarity);

            return Ok(new
            {
                CaseId = caseId,
                Matches = matches,
                TotalCount = matches.Count(),
                MinSimilarity = minSimilarity,
                Jurisdiction = jurisdiction
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cases for case: {CaseId}", caseId);
            return StatusCode(500, "An error occurred while finding similar cases");
        }
    }

    [HttpPost("similar/text")]
    public async Task<IActionResult> FindSimilarCasesByText(
        [FromBody] CaseTextRequest request,
        [FromQuery] string? jurisdiction = null,
        [FromQuery] int limit = 10,
        [FromQuery] decimal minSimilarity = 0.6m)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Case text is required");
            }

            var matches = await _caseMatchingService.FindSimilarCasesByTextAsync(
                request.Text, jurisdiction, limit, minSimilarity);

            return Ok(new
            {
                TextLength = request.Text.Length,
                Matches = matches,
                TotalCount = matches.Count(),
                MinSimilarity = minSimilarity,
                Jurisdiction = jurisdiction
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cases by text");
            return StatusCode(500, "An error occurred while finding similar cases");
        }
    }

    [HttpGet("best/{caseId}")]
    public async Task<IActionResult> GetBestMatch(
        Guid caseId,
        [FromQuery] string? jurisdiction = null,
        [FromQuery] decimal minSimilarity = 0.7m)
    {
        try
        {
            var bestMatch = await _caseMatchingService.GetBestMatchAsync(
                caseId, jurisdiction, minSimilarity);

            if (bestMatch == null)
            {
                return NotFound("No suitable match found");
            }

            return Ok(bestMatch);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best match for case: {CaseId}", caseId);
            return StatusCode(500, "An error occurred while finding the best match");
        }
    }

    [HttpGet("precedents/{caseId}")]
    public async Task<IActionResult> FindPrecedents(
        Guid caseId,
        [FromQuery] string? jurisdiction = null,
        [FromQuery] int limit = 5,
        [FromQuery] decimal minSimilarity = 0.7m)
    {
        try
        {
            var precedents = await _caseMatchingService.FindPrecedentsAsync(
                caseId, jurisdiction, limit, minSimilarity);

            return Ok(new
            {
                CaseId = caseId,
                Precedents = precedents,
                TotalCount = precedents.Count(),
                MinSimilarity = minSimilarity,
                Jurisdiction = jurisdiction
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding precedents for case: {CaseId}", caseId);
            return StatusCode(500, "An error occurred while finding precedents");
        }
    }

    [HttpGet("similarity")]
    public async Task<IActionResult> CalculateSimilarity(
        [FromQuery] Guid caseId1,
        [FromQuery] Guid caseId2)
    {
        try
        {
            var similarity = await _caseMatchingService.CalculateSimilarityAsync(caseId1, caseId2);

            return Ok(new
            {
                CaseId1 = caseId1,
                CaseId2 = caseId2,
                SimilarityScore = similarity,
                SimilarityPercentage = similarity * 100
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating similarity between cases: {CaseId1} and {CaseId2}", caseId1, caseId2);
            return StatusCode(500, "An error occurred while calculating similarity");
        }
    }

    [HttpPost("similarity/text")]
    public async Task<IActionResult> CalculateTextSimilarity(
        [FromBody] TextSimilarityRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text1) || string.IsNullOrWhiteSpace(request.Text2))
            {
                return BadRequest("Both text fields are required");
            }

            var similarity = await _caseMatchingService.CalculateTextSimilarityAsync(
                request.Text1, request.Text2);

            return Ok(new
            {
                Text1Length = request.Text1.Length,
                Text2Length = request.Text2.Length,
                SimilarityScore = similarity,
                SimilarityPercentage = similarity * 100
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating text similarity");
            return StatusCode(500, "An error occurred while calculating text similarity");
        }
    }

    [HttpGet("history/{caseId}")]
    public async Task<IActionResult> GetMatchHistory(
        Guid caseId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        try
        {
            var history = await _caseMatchingService.GetCaseMatchHistoryAsync(caseId, limit, offset);

            return Ok(new
            {
                CaseId = caseId,
                MatchHistory = history,
                TotalCount = history.Count(),
                Limit = limit,
                Offset = offset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match history for case: {CaseId}", caseId);
            return StatusCode(500, "An error occurred while retrieving match history");
        }
    }

    [HttpGet("criteria")]
    public async Task<IActionResult> GetMatchingCriteria()
    {
        try
        {
            var criteria = await _caseMatchingService.GetMatchingCriteriaAsync();
            return Ok(criteria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matching criteria");
            return StatusCode(500, "An error occurred while retrieving matching criteria");
        }
    }

    [HttpPut("criteria")]
    public async Task<IActionResult> UpdateMatchingCriteria([FromBody] MatchingCriteria criteria)
    {
        try
        {
            await _caseMatchingService.UpdateMatchingCriteriaAsync(criteria);
            return Ok("Matching criteria updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating matching criteria");
            return StatusCode(500, "An error occurred while updating matching criteria");
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Test the service with a simple similarity calculation
            var similarity = await _caseMatchingService.CalculateTextSimilarityAsync(
                "test case text", "test case text");

            return Ok(new 
            { 
                Status = "Healthy", 
                Service = "CaseMatching", 
                SelfSimilarity = similarity 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Case matching service health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Service = "CaseMatching", Error = ex.Message });
        }
    }
}

public class CaseTextRequest
{
    public string Text { get; set; } = string.Empty;
}

public class TextSimilarityRequest
{
    public string Text1 { get; set; } = string.Empty;
    public string Text2 { get; set; } = string.Empty;
}