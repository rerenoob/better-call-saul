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

    public async Task<ViabilityAssessment> AssessViabilityAsync(Guid caseId, string caseFacts, string[] charges, string[] evidence, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting viability assessment for case {CaseId}", caseId);

        var assessment = new ViabilityAssessment
        {
            CaseId = caseId,
            CaseFacts = caseFacts,
            Charges = charges,
            Evidence = evidence,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Prepare viability assessment prompt
            var prompt = BuildViabilityAssessmentPrompt(caseFacts, charges, evidence);
            
            // Call OpenAI service for viability assessment
            var aiResponse = await _openAIService.GenerateLegalAnalysisAsync(prompt, "Viability Assessment", cancellationToken);
            
            if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.GeneratedText))
            {
                ParseViabilityResults(assessment, aiResponse.GeneratedText);
                assessment.ConfidenceLevel = aiResponse.ConfidenceScore > 80 ? "High" : 
                                           aiResponse.ConfidenceScore > 60 ? "Medium" : "Low";
            }
            else
            {
                _logger.LogWarning("AI viability assessment failed for case {CaseId}: {Error}", caseId, aiResponse.ErrorMessage);
                assessment.ViabilityScore = 50.0; // Default neutral score
                assessment.ConfidenceLevel = "Low";
                assessment.Reasoning = "Unable to complete AI assessment. Manual review recommended.";
            }

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during viability assessment for case {CaseId}", caseId);
            assessment.ViabilityScore = 0;
            assessment.ConfidenceLevel = "Low";
            assessment.Reasoning = "Assessment failed due to technical error. Manual review required.";
            return assessment;
        }
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

    private string BuildViabilityAssessmentPrompt(string caseFacts, string[] charges, string[] evidence)
    {
        return $@"
Please assess the viability of this criminal defense case and provide a comprehensive analysis:

CASE FACTS:
{caseFacts}

CHARGES:
{string.Join(", ", charges)}

AVAILABLE EVIDENCE:
{string.Join("\n", evidence)}

Please provide your assessment in the following format:

VIABILITY SCORE: [0-100]%

CONFIDENCE LEVEL: [High/Medium/Low]

REASONING:
[Detailed explanation of why this case has the assessed viability]

STRENGTH FACTORS:
- [List key factors that strengthen the defense]

WEAKNESS FACTORS:  
- [List key factors that weaken the defense]

RECOMMENDED STRATEGY:
[Primary recommended approach for defending this case]

Focus on legal precedents, evidence quality, procedural issues, and potential defenses.
";
    }

    private void ParseViabilityResults(ViabilityAssessment assessment, string analysisText)
    {
        try
        {
            // Extract viability score
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"VIABILITY SCORE:\s*(\d+)%?");
            if (scoreMatch.Success && double.TryParse(scoreMatch.Groups[1].Value, out var score))
            {
                assessment.ViabilityScore = score;
            }

            // Extract reasoning
            var reasoningMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"REASONING:\s*(.*?)(?=STRENGTH FACTORS:|WEAKNESS FACTORS:|RECOMMENDED STRATEGY:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (reasoningMatch.Success)
            {
                assessment.Reasoning = reasoningMatch.Groups[1].Value.Trim();
            }

            // Extract strength factors
            var strengthMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"STRENGTH FACTORS:\s*(.*?)(?=WEAKNESS FACTORS:|RECOMMENDED STRATEGY:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (strengthMatch.Success)
            {
                var strengths = strengthMatch.Groups[1].Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Trim().StartsWith("-"))
                    .Select(line => line.Trim().TrimStart('-').Trim())
                    .Where(factor => !string.IsNullOrEmpty(factor))
                    .ToArray();
                assessment.StrengthFactors = strengths;
            }

            // Extract weakness factors
            var weaknessMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"WEAKNESS FACTORS:\s*(.*?)(?=RECOMMENDED STRATEGY:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (weaknessMatch.Success)
            {
                var weaknesses = weaknessMatch.Groups[1].Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Trim().StartsWith("-"))
                    .Select(line => line.Trim().TrimStart('-').Trim())
                    .Where(factor => !string.IsNullOrEmpty(factor))
                    .ToArray();
                assessment.WeaknessFactors = weaknesses;
            }

            // Extract recommended strategy
            var strategyMatch = System.Text.RegularExpressions.Regex.Match(analysisText, @"RECOMMENDED STRATEGY:\s*(.*?)$", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (strategyMatch.Success)
            {
                assessment.RecommendedStrategy = strategyMatch.Groups[1].Value.Trim();
            }

            _logger.LogInformation("Parsed viability assessment for case {CaseId} with score {Score}%", assessment.CaseId, assessment.ViabilityScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse viability assessment results");
        }
    }
}