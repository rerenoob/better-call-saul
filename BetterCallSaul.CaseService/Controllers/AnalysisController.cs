using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.CaseService.Models.Entities;
using BetterCallSaul.CaseService.Services.AI;
using Microsoft.AspNetCore.Authorization;

namespace BetterCallSaul.CaseService.Controllers;

[ApiController]
[Route("api/cases/{caseId}/[controller]")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly ICaseAnalysisService _caseAnalysisService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(ICaseAnalysisService caseAnalysisService, ILogger<AnalysisController> logger)
    {
        _caseAnalysisService = caseAnalysisService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaseAnalysisDocument>>> GetAnalyses(string caseId)
    {
        try
        {
            var analyses = await _caseAnalysisService.GetCaseAnalysesAsync(caseId);
            return Ok(analyses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses for case {CaseId}", caseId);
            return StatusCode(500, "An error occurred while retrieving analyses");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CaseAnalysisDocument>> GetAnalysis(string caseId, string id)
    {
        try
        {
            var analysis = await _caseAnalysisService.GetAnalysisAsync(id);
            if (analysis == null || analysis.CaseId != caseId)
            {
                return NotFound();
            }

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis {AnalysisId} for case {CaseId}", id, caseId);
            return StatusCode(500, "An error occurred while retrieving the analysis");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CaseAnalysisDocument>> AnalyzeCase(
        string caseId,
        [FromBody] AnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.DocumentId) || string.IsNullOrEmpty(request.DocumentText))
            {
                return BadRequest("DocumentId and DocumentText are required");
            }

            var analysis = await _caseAnalysisService.AnalyzeCaseAsync(
                caseId, request.DocumentId, request.DocumentText);

            return CreatedAtAction(
                nameof(GetAnalysis), 
                new { caseId, id = analysis.Id }, 
                analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case {CaseId} with document {DocumentId}", 
                caseId, request?.DocumentId);
            return StatusCode(500, "An error occurred while analyzing the case");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateAnalysisStatus(string caseId, string id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            await _caseAnalysisService.UpdateAnalysisStatusAsync(id, request.Status, request.Message);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for analysis {AnalysisId}", id);
            return StatusCode(500, "An error occurred while updating the analysis status");
        }
    }
}

public class AnalysisRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentText { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}