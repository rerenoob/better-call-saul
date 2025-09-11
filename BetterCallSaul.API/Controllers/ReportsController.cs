using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(BetterCallSaulContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("case/{caseId}/analysis")]
    public async Task<ActionResult<CaseAnalysisReportData>> GetCaseAnalysisReport(Guid caseId)
    {
        try
        {
            var caseItem = await _context.Cases
                .Include(c => c.Documents)
                .ThenInclude(d => d.ExtractedText)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseItem == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            var analyses = await _context.CaseAnalyses
                .Where(a => a.CaseId == caseId && a.Status == AnalysisStatus.Completed)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var reportData = new CaseAnalysisReportData
            {
                CaseId = caseId,
                CaseNumber = caseItem.CaseNumber,
                ClientName = caseItem.Title, // Using Title as client name placeholder
                DefenseAttorney = "Public Defender", // Placeholder
                Charges = new string[] { caseItem.Description ?? "No charges specified" },
                GeneratedAt = DateTime.UtcNow,
                Analyses = analyses.Select(a => new AnalysisReportSection
                {
                    AnalysisId = a.Id,
                    CreatedAt = a.CreatedAt,
                    ViabilityScore = a.ViabilityScore,
                    ConfidenceScore = a.ConfidenceScore,
                    Summary = a.AnalysisText ?? "No analysis summary available",
                    KeyIssues = a.KeyLegalIssues.ToArray(),
                    PotentialDefenses = a.PotentialDefenses.ToArray(),
                    EvidenceGaps = a.EvidenceEvaluation.EvidenceGaps.ToArray(),
                    Recommendations = a.Recommendations.Select(r => new RecommendationItem
                    {
                        Action = r.Action ?? "",
                        Rationale = r.Rationale ?? "",
                        Priority = r.Priority.ToString(),
                        ImpactScore = r.ImpactScore
                    }).ToArray()
                }).ToArray(),
                DocumentSummary = new DocumentSummarySection
                {
                    TotalDocuments = caseItem.Documents?.Count ?? 0,
                    ProcessedDocuments = caseItem.Documents?.Count(d => d.IsProcessed) ?? 0,
                    DocumentTypes = caseItem.Documents?.GroupBy(d => d.Type.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<string, int>()
                }
            };

            return Ok(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating case analysis report for case {CaseId}", caseId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("case/{caseId}/legal-research")]
    public async Task<ActionResult<LegalResearchReportData>> GetLegalResearchReport(Guid caseId)
    {
        try
        {
            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            // Get related legal research (this would be expanded based on actual research data structure)
            var reportData = new LegalResearchReportData
            {
                CaseId = caseId,
                CaseNumber = caseItem.CaseNumber,
                GeneratedAt = DateTime.UtcNow,
                ResearchSections = new List<ResearchSection>
                {
                    new ResearchSection
                    {
                        Title = "Relevant Case Law",
                        Content = "Placeholder for case law research results",
                        Citations = new string[] { "Sample citation 1", "Sample citation 2" }
                    },
                    new ResearchSection
                    {
                        Title = "Applicable Statutes",
                        Content = "Placeholder for statute research results",
                        Citations = new string[] { "Sample statute citation 1" }
                    }
                }
            };

            return Ok(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating legal research report for case {CaseId}", caseId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ReportGenerationResponse>> GenerateReport([FromBody] ReportGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating report for case {CaseId} with template {Template}", request.CaseId, request.Template);

            var caseItem = await _context.Cases.FindAsync(request.CaseId);
            if (caseItem == null)
            {
                return NotFound(new { error = "Case not found" });
            }

            // Generate report content based on template and sections
            var reportContent = await GenerateReportContent(request);

            var response = new ReportGenerationResponse
            {
                ReportId = Guid.NewGuid(),
                CaseId = request.CaseId,
                Template = request.Template,
                Format = request.Format,
                Content = reportContent,
                GeneratedAt = DateTime.UtcNow,
                DownloadUrl = $"/api/reports/download/{Guid.NewGuid()}" // Placeholder URL
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for case {CaseId}", request.CaseId);
            return StatusCode(500, new { error = "Internal server error during report generation" });
        }
    }

    [HttpGet("templates")]
    public ActionResult<IEnumerable<ReportTemplate>> GetReportTemplates()
    {
        var templates = new List<ReportTemplate>
        {
            new ReportTemplate
            {
                Id = "case-analysis",
                Name = "Case Analysis Report",
                Description = "Comprehensive analysis of case viability and strategy",
                Sections = new[] { "Executive Summary", "Key Issues", "Evidence Analysis", "Recommendations" }
            },
            new ReportTemplate
            {
                Id = "legal-research",
                Name = "Legal Research Report",
                Description = "Research findings with citations and precedents",
                Sections = new[] { "Case Law", "Statutes", "Legal Arguments", "Citations" }
            },
            new ReportTemplate
            {
                Id = "case-summary",
                Name = "Case Summary",
                Description = "Brief overview of case facts and status",
                Sections = new[] { "Case Facts", "Current Status", "Next Steps" }
            },
            new ReportTemplate
            {
                Id = "court-filing",
                Name = "Court Filing Preparation",
                Description = "Template for preparing court documents",
                Sections = new[] { "Facts", "Legal Arguments", "Relief Sought", "Exhibits" }
            }
        };

        return Ok(templates);
    }

    private async Task<string> GenerateReportContent(ReportGenerationRequest request)
    {
        var content = new StringBuilder();
        
        content.AppendLine($"# {request.Template.Replace("-", " ").ToUpperInvariant()} REPORT");
        content.AppendLine($"Case ID: {request.CaseId}");
        content.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        content.AppendLine();

        // Get case data
        var caseItem = await _context.Cases.FindAsync(request.CaseId);
        if (caseItem != null)
        {
            content.AppendLine($"Case Number: {caseItem.CaseNumber}");
            content.AppendLine($"Client: {caseItem.Title}");
            content.AppendLine($"Defense Attorney: Public Defender");
            content.AppendLine();
        }

        foreach (var section in request.Sections)
        {
            content.AppendLine($"## {section}");
            content.AppendLine();
            
            switch (section.ToLower())
            {
                case "executive summary":
                    content.AppendLine("This case has been analyzed using AI-powered legal analysis tools.");
                    break;
                case "key issues":
                    var analyses = await _context.CaseAnalyses
                        .Where(a => a.CaseId == request.CaseId && a.Status == AnalysisStatus.Completed)
                        .ToListAsync();
                    
                    foreach (var analysis in analyses)
                    {
                        foreach (var issue in analysis.KeyLegalIssues)
                        {
                            content.AppendLine($"- {issue}");
                        }
                    }
                    break;
                case "recommendations":
                    var analysesWithRecs = await _context.CaseAnalyses
                        .Where(a => a.CaseId == request.CaseId && a.Status == AnalysisStatus.Completed)
                        .ToListAsync();
                    
                    foreach (var analysis in analysesWithRecs)
                    {
                        foreach (var rec in analysis.Recommendations)
                        {
                            content.AppendLine($"- **{rec.Action}**: {rec.Rationale} (Priority: {rec.Priority})");
                        }
                    }
                    break;
                default:
                    content.AppendLine($"Content for {section} section would be generated here.");
                    break;
            }
            
            content.AppendLine();
        }

        return content.ToString();
    }
}

// DTOs for report generation
public class CaseAnalysisReportData
{
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string DefenseAttorney { get; set; } = string.Empty;
    public string[] Charges { get; set; } = Array.Empty<string>();
    public DateTime GeneratedAt { get; set; }
    public AnalysisReportSection[] Analyses { get; set; } = Array.Empty<AnalysisReportSection>();
    public DocumentSummarySection DocumentSummary { get; set; } = new();
}

public class AnalysisReportSection
{
    public Guid AnalysisId { get; set; }
    public DateTime CreatedAt { get; set; }
    public double ViabilityScore { get; set; }
    public double ConfidenceScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string[] KeyIssues { get; set; } = Array.Empty<string>();
    public string[] PotentialDefenses { get; set; } = Array.Empty<string>();
    public string[] EvidenceGaps { get; set; } = Array.Empty<string>();
    public RecommendationItem[] Recommendations { get; set; } = Array.Empty<RecommendationItem>();
}

public class RecommendationItem
{
    public string Action { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
}

public class DocumentSummarySection
{
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public Dictionary<string, int> DocumentTypes { get; set; } = new();
}

public class LegalResearchReportData
{
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ResearchSection> ResearchSections { get; set; } = new();
}

public class ResearchSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string[] Citations { get; set; } = Array.Empty<string>();
}

public class ReportGenerationRequest
{
    public Guid CaseId { get; set; }
    public string Template { get; set; } = string.Empty;
    public string Format { get; set; } = "pdf";
    public string[] Sections { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? CustomFields { get; set; }
}

public class ReportGenerationResponse
{
    public Guid ReportId { get; set; }
    public Guid CaseId { get; set; }
    public string Template { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class ReportTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Sections { get; set; } = Array.Empty<string>();
}