using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.AI;

public class CaseAnalysisService : ICaseAnalysisService
{
    private readonly BetterCallSaulContext _context;
    private readonly IAzureOpenAIService _openAIService;
    private readonly ILogger<CaseAnalysisService> _logger;

    public event EventHandler<AnalysisProgressEventArgs>? AnalysisProgress;

    public CaseAnalysisService(BetterCallSaulContext context, IAzureOpenAIService openAIService, ILogger<CaseAnalysisService> logger)
    {
        _context = context;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<CaseAnalysis> AnalyzeCaseAsync(Guid caseId, Guid documentId, string documentText, CancellationToken cancellationToken = default)
    {
        var analysis = new CaseAnalysis
        {
            CaseId = caseId,
            DocumentId = documentId,
            Status = AnalysisStatus.Processing
        };

        _context.CaseAnalyses.Add(analysis);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            OnAnalysisProgress(new AnalysisProgressEventArgs
            {
                AnalysisId = analysis.Id,
                CaseId = caseId,
                Status = AnalysisStatus.Processing,
                ProgressPercentage = 10,
                Message = "Starting AI analysis"
            });

            // Call OpenAI service for legal analysis
            var aiResponse = await _openAIService.GenerateLegalAnalysisAsync(documentText, "Case analysis", cancellationToken);
            
            if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.GeneratedText))
            {
                analysis.AnalysisText = aiResponse.GeneratedText;
                analysis.ConfidenceScore = aiResponse.ConfidenceScore;
                analysis.Status = AnalysisStatus.Completed;
                analysis.CompletedAt = DateTime.UtcNow;
                analysis.ProcessingTime = aiResponse.ProcessingTime;

                // Parse AI response to extract structured data
                ParseAnalysisResults(analysis, aiResponse.GeneratedText);

                OnAnalysisProgress(new AnalysisProgressEventArgs
                {
                    AnalysisId = analysis.Id,
                    CaseId = caseId,
                    Status = AnalysisStatus.Completed,
                    ProgressPercentage = 100,
                    Message = "Analysis completed successfully"
                });
            }
            else
            {
                analysis.Status = AnalysisStatus.Failed;
                analysis.AnalysisText = aiResponse.ErrorMessage;
                
                OnAnalysisProgress(new AnalysisProgressEventArgs
                {
                    AnalysisId = analysis.Id,
                    CaseId = caseId,
                    Status = AnalysisStatus.Failed,
                    ProgressPercentage = 0,
                    Message = aiResponse.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case {CaseId} with document {DocumentId}", caseId, documentId);
            analysis.Status = AnalysisStatus.Failed;
            analysis.AnalysisText = $"Analysis failed: {ex.Message}";
            
            OnAnalysisProgress(new AnalysisProgressEventArgs
            {
                AnalysisId = analysis.Id,
                CaseId = caseId,
                Status = AnalysisStatus.Failed,
                ProgressPercentage = 0,
                Message = ex.Message
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return analysis;
    }

    public async Task<CaseAnalysis> GetAnalysisAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        return await _context.CaseAnalyses.FirstOrDefaultAsync(a => a.Id == analysisId, cancellationToken) ?? throw new KeyNotFoundException("Analysis not found");
    }

    public async Task<List<CaseAnalysis>> GetCaseAnalysesAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _context.CaseAnalyses.Where(a => a.CaseId == caseId).OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task UpdateAnalysisStatusAsync(Guid analysisId, AnalysisStatus status, string? message = null, CancellationToken cancellationToken = default)
    {
        var analysis = await GetAnalysisAsync(analysisId, cancellationToken);
        analysis.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private void OnAnalysisProgress(AnalysisProgressEventArgs e)
    {
        AnalysisProgress?.Invoke(this, e);
    }

    private void ParseAnalysisResults(CaseAnalysis analysis, string analysisText)
    {
        try
        {
            // Extract viability score (0-100%)
            var viabilityMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"Case Viability Assessment.*?(\d+)%");
            if (viabilityMatch.Success && double.TryParse(viabilityMatch.Groups[1].Value, out var viabilityScore))
            {
                analysis.ViabilityScore = viabilityScore;
            }

            // Extract key legal issues
            var issuesMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"Key Legal Issues Identified.*?([\s\S]*?)(?=Potential Defenses|Evidence Strength|Timeline|Recommended Next Steps|$)");
            if (issuesMatch.Success)
            {
                var issues = issuesMatch.Groups[1].Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Trim().StartsWith("-") || line.Trim().StartsWith("•") || char.IsDigit(line.Trim()[0]))
                    .Select(line => line.Trim().TrimStart('-', '•', ' ', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0').Trim())
                    .Where(issue => !string.IsNullOrEmpty(issue))
                    .ToList();
                
                analysis.KeyLegalIssues = issues;
            }

            // Extract potential defenses
            var defensesMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"Potential Defenses and Arguments.*?([\s\S]*?)(?=Evidence Strength|Timeline|Recommended Next Steps|$)");
            if (defensesMatch.Success)
            {
                var defenses = defensesMatch.Groups[1].Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Trim().StartsWith("-") || line.Trim().StartsWith("•") || char.IsDigit(line.Trim()[0]))
                    .Select(line => line.Trim().TrimStart('-', '•', ' ', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0').Trim())
                    .Where(defense => !string.IsNullOrEmpty(defense))
                    .ToList();
                
                analysis.PotentialDefenses = defenses;
            }

            // Simple evidence strength parsing
            var evidenceMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"Evidence Strength.*?(strong|weak|moderate|excellent|poor)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (evidenceMatch.Success)
            {
                var strength = evidenceMatch.Groups[1].Value.ToLower();
                analysis.EvidenceEvaluation.StrengthScore = strength switch
                {
                    "excellent" or "strong" => 0.8,
                    "moderate" => 0.5,
                    "weak" or "poor" => 0.2,
                    _ => 0.5
                };
            }

            _logger.LogInformation("Parsed analysis results for case {CaseId}", analysis.CaseId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse structured data from AI analysis text");
        }
    }
}