using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaseAnalysisController : ControllerBase
{
    private readonly ICaseAnalysisService _caseAnalysisService;
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<CaseAnalysisController> _logger;

    public CaseAnalysisController(
        ICaseAnalysisService caseAnalysisService,
        BetterCallSaulContext context,
        ILogger<CaseAnalysisController> logger)
    {
        _caseAnalysisService = caseAnalysisService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("analyze/{caseId}")]
    public async Task<ActionResult<CaseAnalysisResponse>> AnalyzeCase(Guid caseId, [FromBody] AnalyzeCaseRequest request)
    {
        try
        {
            _logger.LogInformation("Starting case analysis for case {CaseId}", caseId);

            // Verify case exists and user has access
            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            // Get document for analysis
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.CaseId == caseId);
            
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            // Start analysis
            var analysis = await _caseAnalysisService.AnalyzeCaseAsync(
                caseId, 
                request.DocumentId, 
                document.ExtractedText?.FullText ?? string.Empty);

            var response = new CaseAnalysisResponse
            {
                AnalysisId = analysis.Id,
                CaseId = caseId,
                Status = analysis.Status.ToString(),
                ViabilityScore = analysis.ViabilityScore > 0 ? analysis.ViabilityScore : null,
                ConfidenceScore = analysis.ConfidenceScore > 0 ? analysis.ConfidenceScore : null,
                Summary = analysis.AnalysisText,
                KeyIssues = analysis.KeyLegalIssues.ToArray(),
                PotentialDefenses = analysis.PotentialDefenses.ToArray(),
                EvidenceGaps = analysis.EvidenceEvaluation.EvidenceGaps.ToArray(),
                RecommendedActions = analysis.Recommendations.Select(r => r.Action ?? "").Where(a => !string.IsNullOrEmpty(a)).ToArray(),
                SimilarCases = new string[0], // Placeholder for similar cases
                CreatedAt = analysis.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case {CaseId}", caseId);
            return StatusCode(500, new { error = "Internal server error during analysis" });
        }
    }

    [HttpGet("analysis/{analysisId}")]
    public async Task<ActionResult<CaseAnalysisResponse>> GetAnalysis(Guid analysisId)
    {
        try
        {
            var analysis = await _context.CaseAnalyses
                .FirstOrDefaultAsync(a => a.Id == analysisId);

            if (analysis == null)
            {
                return NotFound(new { error = "Analysis not found" });
            }

            var response = new CaseAnalysisResponse
            {
                AnalysisId = analysis.Id,
                CaseId = analysis.CaseId,
                Status = analysis.Status.ToString(),
                ViabilityScore = analysis.ViabilityScore > 0 ? analysis.ViabilityScore : null,
                ConfidenceScore = analysis.ConfidenceScore > 0 ? analysis.ConfidenceScore : null,
                Summary = analysis.AnalysisText,
                KeyIssues = analysis.KeyLegalIssues.ToArray(),
                PotentialDefenses = analysis.PotentialDefenses.ToArray(),
                EvidenceGaps = analysis.EvidenceEvaluation.EvidenceGaps.ToArray(),
                RecommendedActions = analysis.Recommendations.Select(r => r.Action ?? "").Where(a => !string.IsNullOrEmpty(a)).ToArray(),
                SimilarCases = new string[0], // Placeholder for similar cases
                CreatedAt = analysis.CreatedAt,
                CompletedAt = analysis.CompletedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis {AnalysisId}", analysisId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("case/{caseId}/analyses")]
    public async Task<ActionResult<IEnumerable<CaseAnalysisResponse>>> GetCaseAnalyses(Guid caseId)
    {
        try
        {
            var analyses = await _context.CaseAnalyses
                .Where(a => a.CaseId == caseId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var responses = analyses.Select(a => new CaseAnalysisResponse
                {
                    AnalysisId = a.Id,
                    CaseId = a.CaseId,
                    Status = a.Status.ToString(),
                    ViabilityScore = a.ViabilityScore > 0 ? a.ViabilityScore : null,
                    ConfidenceScore = a.ConfidenceScore > 0 ? a.ConfidenceScore : null,
                    Summary = a.AnalysisText,
                    KeyIssues = a.KeyLegalIssues.ToArray(),
                    PotentialDefenses = a.PotentialDefenses.ToArray(),
                    EvidenceGaps = a.EvidenceEvaluation.EvidenceGaps.ToArray(),
                    RecommendedActions = a.Recommendations.Select(r => r.Action ?? "").Where(action => !string.IsNullOrEmpty(action)).ToArray(),
                    SimilarCases = new string[0], // Placeholder for similar cases
                    CreatedAt = a.CreatedAt,
                    CompletedAt = a.CompletedAt
                })
                .ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses for case {CaseId}", caseId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("viability/{caseId}")]
    public async Task<ActionResult<ViabilityAssessmentResponse>> AssessViability(Guid caseId, [FromBody] ViabilityAssessmentRequest request)
    {
        try
        {
            _logger.LogInformation("Assessing case viability for case {CaseId}", caseId);

            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            // Perform viability assessment based on case facts and charges
            var assessment = await _caseAnalysisService.AssessViabilityAsync(
                caseId,
                request.CaseFacts,
                request.Charges,
                request.Evidence);

            var response = new ViabilityAssessmentResponse
            {
                CaseId = caseId,
                ViabilityScore = assessment.ViabilityScore,
                ConfidenceLevel = assessment.ConfidenceLevel,
                Reasoning = assessment.Reasoning,
                StrengthFactors = assessment.StrengthFactors,
                WeaknessFactors = assessment.WeaknessFactors,
                RecommendedStrategy = assessment.RecommendedStrategy,
                AssessedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing viability for case {CaseId}", caseId);
            return StatusCode(500, new { error = "Internal server error during viability assessment" });
        }
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<AnalysisMetricsResponse>> GetAnalysisMetrics()
    {
        try
        {
            var totalAnalyses = await _context.CaseAnalyses.CountAsync();
            var completedAnalyses = await _context.CaseAnalyses
                .CountAsync(a => a.Status == AnalysisStatus.Completed);
            var processingAnalyses = await _context.CaseAnalyses
                .CountAsync(a => a.Status == AnalysisStatus.Processing);
            var failedAnalyses = await _context.CaseAnalyses
                .CountAsync(a => a.Status == AnalysisStatus.Failed);

            var averageViabilityScore = await _context.CaseAnalyses
                .Where(a => a.Status == AnalysisStatus.Completed && a.ViabilityScore > 0)
                .AverageAsync(a => a.ViabilityScore);

            var averageConfidenceScore = await _context.CaseAnalyses
                .Where(a => a.Status == AnalysisStatus.Completed && a.ConfidenceScore > 0)
                .AverageAsync(a => a.ConfidenceScore);

            var response = new AnalysisMetricsResponse
            {
                TotalAnalyses = totalAnalyses,
                CompletedAnalyses = completedAnalyses,
                ProcessingAnalyses = processingAnalyses,
                FailedAnalyses = failedAnalyses,
                AverageViabilityScore = Math.Round(averageViabilityScore, 2),
                AverageConfidenceScore = Math.Round(averageConfidenceScore, 2),
                SuccessRate = totalAnalyses > 0 ? Math.Round((double)completedAnalyses / totalAnalyses * 100, 2) : 0
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class AnalyzeCaseRequest
{
    public Guid DocumentId { get; set; }
    public string? AnalysisType { get; set; } = "comprehensive";
    public bool IncludeViabilityAssessment { get; set; } = true;
    public bool IncludeSimilarCases { get; set; } = true;
}

public class CaseAnalysisResponse
{
    public Guid AnalysisId { get; set; }
    public Guid CaseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? ViabilityScore { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? Summary { get; set; }
    public string[]? KeyIssues { get; set; }
    public string[]? PotentialDefenses { get; set; }
    public string[]? EvidenceGaps { get; set; }
    public string[]? RecommendedActions { get; set; }
    public string[]? SimilarCases { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ViabilityAssessmentRequest
{
    public string CaseFacts { get; set; } = string.Empty;
    public string[] Charges { get; set; } = Array.Empty<string>();
    public string[] Evidence { get; set; } = Array.Empty<string>();
}

public class ViabilityAssessmentResponse
{
    public Guid CaseId { get; set; }
    public double ViabilityScore { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public string[] StrengthFactors { get; set; } = Array.Empty<string>();
    public string[] WeaknessFactors { get; set; } = Array.Empty<string>();
    public string RecommendedStrategy { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; }
}

public class AnalysisMetricsResponse
{
    public int TotalAnalyses { get; set; }
    public int CompletedAnalyses { get; set; }
    public int ProcessingAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public double AverageViabilityScore { get; set; }
    public double AverageConfidenceScore { get; set; }
    public double SuccessRate { get; set; }
}