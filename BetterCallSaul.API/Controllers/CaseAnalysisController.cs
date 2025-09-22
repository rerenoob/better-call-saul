using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
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
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<CaseAnalysisController> _logger;

    public CaseAnalysisController(
        ICaseAnalysisService caseAnalysisService,
        ICaseDocumentRepository caseDocumentRepository,
        BetterCallSaulContext context,
        ILogger<CaseAnalysisController> logger)
    {
        _caseAnalysisService = caseAnalysisService;
        _caseDocumentRepository = caseDocumentRepository;
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
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(analysisId);
            if (caseDocument == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            var latestAnalysis = caseDocument.Analyses?.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (latestAnalysis == null)
            {
                return NotFound(new { error = "Analysis not found" });
            }

            var response = new CaseAnalysisResponse
            {
                AnalysisId = latestAnalysis.Id,
                CaseId = caseDocument.CaseId,
                Status = latestAnalysis.Status.ToString(),
                ViabilityScore = latestAnalysis.ViabilityScore > 0 ? latestAnalysis.ViabilityScore : null,
                ConfidenceScore = latestAnalysis.ConfidenceScore > 0 ? latestAnalysis.ConfidenceScore : null,
                Summary = latestAnalysis.AnalysisText,
                KeyIssues = latestAnalysis.KeyLegalIssues?.ToArray() ?? Array.Empty<string>(),
                PotentialDefenses = latestAnalysis.PotentialDefenses?.ToArray() ?? Array.Empty<string>(),
                EvidenceGaps = latestAnalysis.EvidenceEvaluation?.EvidenceGaps?.ToArray() ?? Array.Empty<string>(),
                RecommendedActions = latestAnalysis.Recommendations?.Select(r => r.Action ?? "").Where(a => !string.IsNullOrEmpty(a)).ToArray() ?? Array.Empty<string>(),
                SimilarCases = new string[0], // Placeholder for similar cases
                CreatedAt = latestAnalysis.CreatedAt,
                CompletedAt = latestAnalysis.CompletedAt
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
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(caseId);
            if (caseDocument == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            var analyses = caseDocument.Analyses;
            var responses = analyses.Select(a => new CaseAnalysisResponse
                {
                    AnalysisId = a.Id,
                    CaseId = caseId,
                    Status = a.Status.ToString(),
                    ViabilityScore = a.ViabilityScore > 0 ? a.ViabilityScore : null,
                    ConfidenceScore = a.ConfidenceScore > 0 ? a.ConfidenceScore : null,
                    Summary = a.AnalysisText,
                    KeyIssues = a.KeyLegalIssues?.ToArray() ?? Array.Empty<string>(),
                    PotentialDefenses = a.PotentialDefenses?.ToArray() ?? Array.Empty<string>(),
                    EvidenceGaps = a.EvidenceEvaluation?.EvidenceGaps?.ToArray() ?? Array.Empty<string>(),
                    RecommendedActions = (a.Recommendations?.Select(r => r.Action ?? "").Where(action => !string.IsNullOrEmpty(action)).ToArray()) ?? Array.Empty<string>(),
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
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var caseDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
            var allAnalyses = caseDocuments.SelectMany(cd => cd.Analyses).ToList();
            
            var totalAnalyses = allAnalyses.Count;
            var completedAnalyses = allAnalyses.Count(a => a.Status == AnalysisStatus.Completed);
            var processingAnalyses = allAnalyses.Count(a => a.Status == AnalysisStatus.Processing);
            var failedAnalyses = allAnalyses.Count(a => a.Status == AnalysisStatus.Failed);

            var completedAnalysesWithScores = allAnalyses.Where(a => a.Status == AnalysisStatus.Completed && a.ViabilityScore > 0).ToList();
            var averageViabilityScore = completedAnalysesWithScores.Any() ? completedAnalysesWithScores.Average(a => a.ViabilityScore) : 0;
            var averageConfidenceScore = completedAnalysesWithScores.Any() ? completedAnalysesWithScores.Average(a => a.ConfidenceScore) : 0;

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