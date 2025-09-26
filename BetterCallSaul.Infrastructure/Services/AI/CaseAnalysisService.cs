using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Infrastructure.Services.AI;

public class CaseAnalysisService : ICaseAnalysisService
{
    private readonly BetterCallSaulContext _context;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IAIService _openAIService;
    private readonly ILogger<CaseAnalysisService> _logger;

    public event EventHandler<AnalysisProgressEventArgs>? AnalysisProgress;

    public CaseAnalysisService(
        BetterCallSaulContext context,
        ICaseDocumentRepository caseDocumentRepository,
        IAIService openAIService,
        ILogger<CaseAnalysisService> logger)
    {
        _context = context;
        _caseDocumentRepository = caseDocumentRepository;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<CaseAnalysis> AnalyzeCaseAsync(Guid caseId, Guid documentId, string documentText, CancellationToken cancellationToken = default)
    {
        // Validate that we have document text to analyze
        if (string.IsNullOrWhiteSpace(documentText))
        {
            _logger.LogError("Cannot analyze case {CaseId} - document {DocumentId} has no extracted text", caseId, documentId);

            var failedAnalysis = new CaseAnalysis
            {
                CaseId = caseId,
                DocumentId = documentId,
                Status = AnalysisStatus.Failed,
                AnalysisText = "Analysis failed: No text content available from document. Text extraction may have failed.",
                ViabilityScore = 0,
                ConfidenceScore = 0,
                Metadata = new Dictionary<string, object> { ["error"] = "Text extraction failed - no content available for analysis" },
                CompletedAt = DateTime.UtcNow
            };

            _context.CaseAnalyses.Add(failedAnalysis);
            await _context.SaveChangesAsync(cancellationToken);

            return failedAnalysis;
        }

        // Create SQL record for tracking
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

            // Call AI service for legal analysis
            var aiResponse = await _openAIService.GenerateLegalAnalysisAsync(documentText, "Case analysis", cancellationToken);

            if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.GeneratedText))
            {
                // Update SQL record with basic info
                analysis.AnalysisText = aiResponse.GeneratedText;
                analysis.ConfidenceScore = aiResponse.ConfidenceScore;
                analysis.Status = AnalysisStatus.Completed;
                analysis.CompletedAt = DateTime.UtcNow;
                analysis.ProcessingTime = aiResponse.ProcessingTime;

                // Parse AI response to extract structured data
                ParseAnalysisResults(analysis, aiResponse.GeneratedText);

                // Store detailed results in NoSQL
                await StoreAnalysisInNoSQLAsync(analysis, aiResponse.GeneratedText, cancellationToken);

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

                // Store failed analysis in NoSQL
                await StoreFailedAnalysisInNoSQLAsync(analysis, aiResponse.ErrorMessage, cancellationToken);

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

            // Store failed analysis in NoSQL
            await StoreFailedAnalysisInNoSQLAsync(analysis, ex.Message, cancellationToken);

            OnAnalysisProgress(new AnalysisProgressEventArgs
            {
                AnalysisId = analysis.Id,
                CaseId = caseId,
                Status = AnalysisStatus.Failed,
                ProgressPercentage = 0,
                Message = $"Analysis failure: {ex.Message}"
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
        // Get basic analysis records from SQL
        return await _context.CaseAnalyses
            .Where(a => a.CaseId == caseId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
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
                _logger.LogWarning("AI viability assessment failed for case {CaseId}: {Error}. Confidence: {Confidence}", 
                    caseId, aiResponse.ErrorMessage, aiResponse.ConfidenceScore);
                assessment.ViabilityScore = 50.0; // Default neutral score
                assessment.ConfidenceLevel = "Low";
                assessment.Reasoning = $"Unable to complete AI assessment: {aiResponse.ErrorMessage}";
                assessment.Metadata = new Dictionary<string, object>
                {
                    ["ai_error"] = aiResponse.ErrorMessage ?? "Unknown error",
                    ["ai_confidence"] = aiResponse.ConfidenceScore,
                    ["assessment_failed"] = true
                };

                // Create audit log for AI assessment failure
                await CreateAuditLogAsync(
                    "AI_ASSESSMENT_FAILURE",
                    $"AI viability assessment failed for case {caseId}: {aiResponse.ErrorMessage}",
                    "ViabilityAssessment",
                    Guid.NewGuid(), // No assessment ID yet
                    AuditLogLevel.Warning
                );
            }

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during viability assessment for case {CaseId}. Exception type: {ExceptionType}", 
                caseId, ex.GetType().Name);
            assessment.ViabilityScore = 0;
            assessment.ConfidenceLevel = "Low";
            assessment.Reasoning = $"Assessment failed due to technical error: {ex.Message}";
            assessment.Metadata = new Dictionary<string, object>
            {
                ["error_type"] = ex.GetType().Name,
                ["error_stack_trace"] = ex.StackTrace ?? "No stack trace",
                ["assessment_failed"] = true
            };

            // Create audit log for critical assessment failure
            await CreateAuditLogAsync(
                "VIABILITY_ASSESSMENT_CRITICAL_FAILURE",
                $"Critical viability assessment error for case {caseId}: {ex.Message}",
                "ViabilityAssessment",
                Guid.NewGuid(), // No assessment ID yet
                AuditLogLevel.Critical
            );

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

    private async Task StoreAnalysisInNoSQLAsync(CaseAnalysis analysis, string analysisText, CancellationToken cancellationToken)
    {
        try
        {
            // Get or create case document
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(analysis.CaseId);
            if (caseDocument == null)
            {
                // Get user ID from case (cross-database query)
                var caseEntity = await _context.Cases.FirstOrDefaultAsync(c => c.Id == analysis.CaseId, cancellationToken);
                var userId = caseEntity?.UserId ?? Guid.Empty;

                caseDocument = new CaseDocument
                {
                    CaseId = analysis.CaseId,
                    UserId = userId
                };
            }

            // Create detailed analysis result for NoSQL
            var analysisResult = new CaseAnalysisResult
            {
                Id = analysis.Id,
                DocumentId = analysis.DocumentId,
                AnalysisText = analysisText,
                ViabilityScore = analysis.ViabilityScore,
                ConfidenceScore = analysis.ConfidenceScore,
                KeyLegalIssues = analysis.KeyLegalIssues,
                PotentialDefenses = analysis.PotentialDefenses,
                EvidenceEvaluation = new EvidenceEvaluationInfo
                {
                    StrengthScore = analysis.EvidenceEvaluation.StrengthScore,
                    StrongEvidence = analysis.EvidenceEvaluation.StrongEvidence,
                    WeakEvidence = analysis.EvidenceEvaluation.WeakEvidence,
                    EvidenceGaps = analysis.EvidenceEvaluation.EvidenceGaps,
                    AdditionalEvidenceNeeded = analysis.EvidenceEvaluation.AdditionalEvidenceNeeded
                },
                TimelineAnalysis = new TimelineAnalysisInfo
                {
                    Events = analysis.TimelineAnalysis.Events.Select(e => new TimelineEventInfo
                    {
                        Date = e.Date,
                        Description = e.Description,
                        Significance = e.Significance,
                        Confidence = e.Confidence
                    }).ToList(),
                    ChronologicalIssues = analysis.TimelineAnalysis.ChronologicalIssues,
                    CriticalTimePoints = analysis.TimelineAnalysis.CriticalTimePoints
                },
                Recommendations = analysis.Recommendations.Select(r => new RecommendationInfo
                {
                    Action = r.Action,
                    Rationale = r.Rationale,
                    Priority = r.Priority,
                    ImpactScore = r.ImpactScore
                }).ToList(),
                Status = analysis.Status,
                CreatedAt = analysis.CreatedAt,
                CompletedAt = analysis.CompletedAt,
                ProcessingTime = analysis.ProcessingTime,
                Metadata = analysis.Metadata
            };

            // Add or update analysis in the case document
            var existingAnalysisIndex = caseDocument.Analyses.FindIndex(a => a.Id == analysis.Id);
            if (existingAnalysisIndex >= 0)
            {
                caseDocument.Analyses[existingAnalysisIndex] = analysisResult;
            }
            else
            {
                caseDocument.Analyses.Add(analysisResult);
            }

            caseDocument.UpdatedAt = DateTime.UtcNow;
            caseDocument.Metadata.LastAnalyzedAt = DateTime.UtcNow;
            caseDocument.Metadata.TotalAnalyses = caseDocument.Analyses.Count;

            // Save to NoSQL
            if (caseDocument.Id == default)
            {
                await _caseDocumentRepository.CreateAsync(caseDocument);
            }
            else
            {
                await _caseDocumentRepository.UpdateAsync(caseDocument);
            }

            _logger.LogInformation("Stored analysis {AnalysisId} in NoSQL for case {CaseId}", analysis.Id, analysis.CaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store analysis {AnalysisId} in NoSQL", analysis.Id);
            // Don't throw - this is a secondary storage operation
        }
    }

    private async Task StoreFailedAnalysisInNoSQLAsync(CaseAnalysis analysis, string? errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Store minimal failed analysis record in NoSQL for consistency
            var caseDocument = await _caseDocumentRepository.GetByIdAsync(analysis.CaseId);
            if (caseDocument != null)
            {
                var failedAnalysis = new CaseAnalysisResult
                {
                    Id = analysis.Id,
                    DocumentId = analysis.DocumentId,
                    AnalysisText = errorMessage ?? "Analysis failed",
                    Status = AnalysisStatus.Failed,
                    CreatedAt = analysis.CreatedAt,
                    Metadata = analysis.Metadata
                };

                caseDocument.Analyses.Add(failedAnalysis);
                caseDocument.UpdatedAt = DateTime.UtcNow;
                await _caseDocumentRepository.UpdateAsync(caseDocument);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store failed analysis {AnalysisId} in NoSQL", analysis.Id);
        }
    }

    private async Task CreateAuditLogAsync(string action, string description, string entityType, Guid entityId, AuditLogLevel level)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                Level = level,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {Action}", action);
        }
    }
}