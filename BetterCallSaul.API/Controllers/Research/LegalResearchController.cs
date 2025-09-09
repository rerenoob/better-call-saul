using BetterCallSaul.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LegalResearchController : ControllerBase
{
    private readonly ICourtListenerService _courtListenerService;
    private readonly ICaseAnalysisService _caseAnalysisService;
    private readonly ILogger<LegalResearchController> _logger;

    public LegalResearchController(
        ICourtListenerService courtListenerService,
        ICaseAnalysisService caseAnalysisService,
        ILogger<LegalResearchController> logger)
    {
        _courtListenerService = courtListenerService;
        _caseAnalysisService = caseAnalysisService;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchCases(
        [FromQuery] string query,
        [FromQuery] string? jurisdiction = null,
        [FromQuery] string? court = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            if (limit > 100)
            {
                limit = 100;
            }

            var results = await _courtListenerService.SearchCasesAsync(
                query, jurisdiction, court, startDate, endDate, limit, offset);

            return Ok(new
            {
                Results = results,
                TotalCount = results.Count(),
                Query = query,
                Limit = limit,
                Offset = offset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cases for query: {Query}", query);
            return StatusCode(500, "An error occurred while searching for cases");
        }
    }

    [HttpGet("case/{citation}")]
    public async Task<IActionResult> GetCaseByCitation(string citation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(citation))
            {
                return BadRequest("Citation is required");
            }

            var legalCase = await _courtListenerService.GetCaseByCitationAsync(citation);

            if (legalCase == null)
            {
                return NotFound("Case not found");
            }

            return Ok(legalCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting case by citation: {Citation}", citation);
            return StatusCode(500, "An error occurred while retrieving the case");
        }
    }

    [HttpGet("opinion/{opinionId}")]
    public async Task<IActionResult> GetOpinion(string opinionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(opinionId))
            {
                return BadRequest("Opinion ID is required");
            }

            var opinion = await _courtListenerService.GetOpinionAsync(opinionId);

            if (opinion == null)
            {
                return NotFound("Opinion not found");
            }

            return Ok(opinion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opinion: {OpinionId}", opinionId);
            return StatusCode(500, "An error occurred while retrieving the opinion");
        }
    }

    [HttpGet("related/{citation}")]
    public async Task<IActionResult> GetRelatedCases(
        string citation,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(citation))
            {
                return BadRequest("Citation is required");
            }

            if (limit > 50)
            {
                limit = 50;
            }

            var relatedCases = await _courtListenerService.GetRelatedCasesAsync(citation, limit);

            return Ok(new
            {
                OriginalCitation = citation,
                RelatedCases = relatedCases,
                Count = relatedCases.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related cases for citation: {Citation}", citation);
            return StatusCode(500, "An error occurred while finding related cases");
        }
    }

    [HttpPost("topics")]
    public async Task<IActionResult> SearchByTopics(
        [FromBody] string[] topics,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (topics == null || topics.Length == 0)
            {
                return BadRequest("At least one topic is required");
            }

            if (limit > 50)
            {
                limit = 50;
            }

            var results = await _courtListenerService.SearchByLegalTopicsAsync(topics, limit);

            return Ok(new
            {
                Topics = topics,
                Results = results,
                TotalCount = results.Count(),
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by topics: {Topics}", string.Join(", ", topics));
            return StatusCode(500, "An error occurred while searching by topics");
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Test the service with a simple search
            var results = await _courtListenerService.SearchCasesAsync("test", limit: 1);
            return Ok(new { Status = "Healthy", Service = "CourtListener", ResultsCount = results.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourtListener service health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Service = "CourtListener", Error = ex.Message });
        }
    }

    [HttpPost("analyze/case/{caseId}/document/{documentId}")]
    public async Task<IActionResult> AnalyzeCase(
        Guid caseId,
        Guid documentId,
        [FromBody] CaseAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DocumentText))
            {
                return BadRequest("Document text is required for analysis");
            }

            var analysis = await _caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, request.DocumentText);
            
            return Ok(new
            {
                AnalysisId = analysis.Id,
                Status = analysis.Status.ToString(),
                CreatedAt = analysis.CreatedAt,
                EstimatedCompletionTime = TimeSpan.FromMinutes(2) // Typical AI analysis time
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case {CaseId} with document {DocumentId}", caseId, documentId);
            return StatusCode(500, "An error occurred while analyzing the case");
        }
    }

    [HttpGet("analysis/{analysisId}")]
    public async Task<IActionResult> GetAnalysis(Guid analysisId)
    {
        try
        {
            var analysis = await _caseAnalysisService.GetAnalysisAsync(analysisId);
            
            return Ok(new
            {
                AnalysisId = analysis.Id,
                CaseId = analysis.CaseId,
                DocumentId = analysis.DocumentId,
                Status = analysis.Status.ToString(),
                ViabilityScore = analysis.ViabilityScore,
                ConfidenceScore = analysis.ConfidenceScore,
                AnalysisText = analysis.AnalysisText,
                KeyLegalIssues = analysis.KeyLegalIssues,
                PotentialDefenses = analysis.PotentialDefenses,
                EvidenceStrength = analysis.EvidenceEvaluation.StrengthScore,
                CreatedAt = analysis.CreatedAt,
                CompletedAt = analysis.CompletedAt,
                ProcessingTime = analysis.ProcessingTime
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Analysis not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis {AnalysisId}", analysisId);
            return StatusCode(500, "An error occurred while retrieving the analysis");
        }
    }

    [HttpGet("case/{caseId}/analyses")]
    public async Task<IActionResult> GetCaseAnalyses(Guid caseId)
    {
        try
        {
            var analyses = await _caseAnalysisService.GetCaseAnalysesAsync(caseId);
            
            return Ok(analyses.Select(a => new
            {
                AnalysisId = a.Id,
                DocumentId = a.DocumentId,
                Status = a.Status.ToString(),
                ViabilityScore = a.ViabilityScore,
                ConfidenceScore = a.ConfidenceScore,
                CreatedAt = a.CreatedAt,
                CompletedAt = a.CompletedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses for case {CaseId}", caseId);
            return StatusCode(500, "An error occurred while retrieving case analyses");
        }
    }
}

public class CaseAnalysisRequest
{
    public string DocumentText { get; set; } = string.Empty;
    public string? CaseContext { get; set; }
}