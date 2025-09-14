using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BetterCallSaul.Infrastructure.Services.AI;

/// <summary>
/// Mock AI service implementation for development and testing environments
/// </summary>
public class MockAIService : IAIService
{
    private readonly ILogger<MockAIService> _logger;
    private readonly Random _random;
    private readonly List<string> _legalCaseTypes;
    private readonly List<string> _legalIssues;
    private readonly List<string> _recommendations;

    public MockAIService(ILogger<MockAIService> logger)
    {
        _logger = logger;
        _random = new Random();
        
        _legalCaseTypes = new List<string>
        {
            "Criminal Defense", "Civil Litigation", "Contract Dispute", "Family Law", 
            "Personal Injury", "Employment Law", "Real Estate", "Intellectual Property"
        };
        
        _legalIssues = new List<string>
        {
            "Statute of limitations", "Jurisdictional issues", "Evidence admissibility",
            "Witness credibility", "Constitutional violations", "Procedural errors",
            "Contract interpretation", "Damages calculation", "Liability assessment"
        };
        
        _recommendations = new List<string>
        {
            "File motion to dismiss", "Seek additional discovery", "Consult expert witness",
            "Negotiate settlement", "Prepare for trial", "Appeal previous ruling",
            "Request continuance", "Mediate dispute", "Arbitrate claim"
        };
    }

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service analyzing case with prompt: {Prompt}",
            request.Prompt?.Substring(0, Math.Min(100, request.Prompt?.Length ?? 0)));

        // Simulate realistic processing delay (1-3 seconds)
        var delayMs = _random.Next(1000, 3001);
        await Task.Delay(delayMs, cancellationToken);

        var response = new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockAnalysis(request),
            TokensUsed = _random.Next(120, 301),
            ProcessingTime = TimeSpan.FromMilliseconds(delayMs),
            Model = "mock-ai-model-v2",
            ConfidenceScore = Math.Round(0.7 + (_random.NextDouble() * 0.25), 2), // 70-95% confidence
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["environment"] = "development",
                ["requestId"] = Guid.NewGuid().ToString(),
                ["processingDelayMs"] = delayMs,
                ["caseType"] = _legalCaseTypes[_random.Next(_legalCaseTypes.Count)]
            }
        };

        _logger.LogDebug("Mock analysis completed in {ProcessingTime}ms with confidence {Confidence}", 
            response.ProcessingTime.TotalMilliseconds, response.ConfidenceScore);

        return response;
    }

    public async Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service generating legal analysis for document of length: {Length}", documentText?.Length ?? 0);

        // Simulate realistic processing delay (0.8-2 seconds)
        var delayMs = _random.Next(800, 2001);
        await Task.Delay(delayMs, cancellationToken);

        var response = new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockLegalAnalysis(documentText, caseContext),
            TokensUsed = _random.Next(150, 401),
            ProcessingTime = TimeSpan.FromMilliseconds(delayMs),
            Model = "mock-legal-model-pro",
            ConfidenceScore = Math.Round(0.75 + (_random.NextDouble() * 0.2), 2), // 75-95% confidence
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["analysisType"] = "legal",
                ["documentLength"] = documentText?.Length ?? 0,
                ["hasContext"] = !string.IsNullOrEmpty(caseContext),
                ["processingDelayMs"] = delayMs
            }
        };

        return response;
    }

    public async Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service predicting case outcome for details: {DetailsLength}", caseDetails?.Length ?? 0);

        // Simulate realistic processing delay (0.5-1.5 seconds)
        var delayMs = _random.Next(500, 1501);
        await Task.Delay(delayMs, cancellationToken);

        var confidence = Math.Round(0.6 + (_random.NextDouble() * 0.35), 2); // 60-95% confidence
        var isFavorable = _random.NextDouble() > 0.4; // 60% chance favorable

        var response = new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockPrediction(caseDetails ?? string.Empty, isFavorable, confidence),
            TokensUsed = _random.Next(80, 201),
            ProcessingTime = TimeSpan.FromMilliseconds(delayMs),
            Model = "mock-prediction-model-ai",
            ConfidenceScore = confidence,
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["predictionType"] = "outcome",
                ["isFavorable"] = isFavorable,
                ["inputLength"] = caseDetails?.Length ?? 0,
                ["processingDelayMs"] = delayMs
            }
        };

        return response;
    }

    public async Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service summarizing document of length: {Length}", documentText?.Length ?? 0);

        // Simulate realistic processing delay (0.4-1.2 seconds)
        var delayMs = _random.Next(400, 1201);
        await Task.Delay(delayMs, cancellationToken);

        var response = new AIResponse
        {
            Success = true,
            GeneratedText = GenerateMockSummary(documentText),
            TokensUsed = _random.Next(60, 151),
            ProcessingTime = TimeSpan.FromMilliseconds(delayMs),
            Model = "mock-summary-model-xl",
            ConfidenceScore = Math.Round(0.85 + (_random.NextDouble() * 0.1), 2), // 85-95% confidence
            Metadata = new Dictionary<string, object>
            {
                ["mockService"] = true,
                ["summaryType"] = "document",
                ["originalLength"] = documentText?.Length ?? 0,
                ["compressionRatio"] = Math.Round((double)(documentText?.Length ?? 1) / 150, 2),
                ["processingDelayMs"] = delayMs
            }
        };

        return response;
    }

    public async IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock AI service streaming analysis for request");

        var analysisType = !string.IsNullOrEmpty(request.Prompt) ? "custom" : 
                          !string.IsNullOrEmpty(request.DocumentText) ? "document" : "general";
        
        var chunks = GenerateStreamingChunks(analysisType, request.DocumentText?.Length ?? 0);

        foreach (var chunk in chunks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            // Variable delay to simulate real streaming (100-400ms)
            await Task.Delay(_random.Next(100, 401), cancellationToken);
            yield return chunk;
        }
    }

    private List<string> GenerateStreamingChunks(string analysisType, int documentLength)
    {
        var chunks = new List<string>
        {
            "Initializing legal analysis engine...\n",
            "Processing document content (",
            $"Document size: {documentLength} characters analyzed...\n",
            "Identifying key legal terminology and concepts...\n",
            "Cross-referencing with legal database...\n",
            "Analyzing case precedent relevance...\n",
            "Assessing evidence strength and admissibility...\n",
            "Evaluating statutory compliance...\n",
            "Generating risk assessment matrix...\n",
            "Formulating strategic recommendations...\n",
            "Finalizing comprehensive analysis report...\n",
            "Analysis complete. Ready for review.\n"
        };

        // Add type-specific chunks
        if (analysisType == "document")
        {
            chunks.Insert(3, "Extracting document structure and headings...\n");
            chunks.Insert(5, "Identifying parties and relationships...\n");
        }
        else if (analysisType == "custom")
        {
            chunks.Insert(2, "Processing custom prompt requirements...\n");
            chunks.Insert(4, "Tailoring analysis to specific query...\n");
        }

        return chunks;
    }

    private string GenerateMockAnalysis(AIRequest request)
    {
        var caseType = _legalCaseTypes[_random.Next(_legalCaseTypes.Count)];
        var keyIssue = _legalIssues[_random.Next(_legalIssues.Count)];
        var recommendation = _recommendations[_random.Next(_recommendations.Count)];
        
        var viabilityScore = _random.Next(40, 96);
        var evidenceStrength = _random.Next(3, 6); // 3-5 stars
        var complexityLevel = _random.Next(1, 6); // 1-5 complexity

        return $"""
# AI-Powered Legal Case Analysis

## Case Overview
- **Case Type**: {caseType}
- **Document Complexity**: {complexityLevel}/5
- **Primary Legal Issue**: {keyIssue}

## Viability Assessment
- **Overall Viability Score**: {viabilityScore}%
- **Evidence Strength**: {new string('★', evidenceStrength)}{new string('☆', 5 - evidenceStrength)}
- **Procedural Soundness**: {_random.Next(60, 96)}%

## Key Legal Findings
1. **{_legalIssues[_random.Next(_legalIssues.Count)]}**: Analysis indicates this is a significant factor with {_random.Next(70, 96)}% relevance
2. **{_legalIssues[_random.Next(_legalIssues.Count)]}**: Moderate impact assessed at {_random.Next(50, 81)}% relevance
3. **{_legalIssues[_random.Next(_legalIssues.Count)]}**: Minor consideration with {_random.Next(30, 61)}% relevance

## Risk Assessment
- **High Risk Factors**: {_random.Next(0, 3)} identified
- **Medium Risk Factors**: {_random.Next(1, 4)} identified  
- **Low Risk Factors**: {_random.Next(2, 6)} identified

## Strategic Recommendations
1. **{recommendation}** - Priority: High
2. **{_recommendations[_random.Next(_recommendations.Count)]}** - Priority: Medium
3. **{_recommendations[_random.Next(_recommendations.Count)]}** - Priority: Low

## Timeline Estimate
- **Initial Preparation**: {_random.Next(2, 8)} weeks
- **Discovery Phase**: {_random.Next(4, 12)} weeks  
- **Trial Preparation**: {_random.Next(6, 16)} weeks
- **Total Estimated Duration**: {_random.Next(12, 36)} weeks

## Confidence Metrics
- **Analysis Confidence**: {Math.Round(0.7 + (_random.NextDouble() * 0.25), 2) * 100}%
- **Data Completeness**: {_random.Next(75, 96)}%
- **Precedent Relevance**: {_random.Next(70, 96)}%

*Generated by Mock AI Service for development and testing purposes. Real AI analysis would provide more detailed and accurate assessment.*
""";
    }

    private string GenerateMockLegalAnalysis(string? documentText, string? caseContext)
    {
        var docLength = documentText?.Length ?? 0;
        var hasContext = !string.IsNullOrEmpty(caseContext);
        var documentType = docLength > 1000 ? "Comprehensive Legal Brief" : 
                          docLength > 500 ? "Standard Legal Document" : "Legal Memorandum";
        
        var keyFindings = _random.Next(2, 5);
        var recommendations = _random.Next(2, 4);

        var analysis = $"""
# Comprehensive Legal Document Analysis

## Document Metadata
- **Document Type**: {documentType}
- **Length**: {docLength} characters
- **Context Provided**: {hasContext}
- **Estimated Reading Time**: {Math.Ceiling(docLength / 250.0)} minutes

## Document Quality Assessment
- **Format Compliance**: {_random.Next(75, 96)}%
- **Legal Terminology Density**: {_random.Next(20, 81)}%
- **Structural Coherence**: {_random.Next(70, 96)}%

## Key Legal Findings
""";

        for (int i = 1; i <= keyFindings; i++)
        {
            analysis += $"- **{_legalIssues[_random.Next(_legalIssues.Count)]}**: {GetFindingDescription()} (Relevance: {_random.Next(60, 96)}%)\n";
        }

        analysis += $"""

## Context Integration
{(hasContext ? $"Case context successfully integrated with {_random.Next(70, 96)}% relevance alignment" : "No case context provided - analysis based solely on document content")}

## Strategic Recommendations
""";

        for (int i = 1; i <= recommendations; i++)
        {
            analysis += $"{i}. {_recommendations[_random.Next(_recommendations.Count)]} (Priority: {GetPriorityLevel()})\n";
        }

        analysis += $"""

## Risk Profile
- **Overall Risk Level**: {GetRiskLevel()}
- **Critical Issues**: {_random.Next(0, 2)}
- **Moderate Issues**: {_random.Next(1, 4)}
- **Minor Issues**: {_random.Next(2, 6)}

*AI-powered legal analysis complete. This mock service simulates real AI document analysis for development purposes.*
""";

        return analysis;
    }

    private string GetFindingDescription()
    {
        var descriptions = new[]
        {
            "Well-supported by document evidence",
            "Requires additional verification", 
            "Strong correlation with established case law",
            "Potential jurisdictional concern",
            "Statutory compliance verified",
            "Procedural requirement identified"
        };
        return descriptions[_random.Next(descriptions.Length)];
    }

    private string GetPriorityLevel()
    {
        var levels = new[] { "High", "Medium", "Low" };
        return levels[_random.Next(levels.Length)];
    }

    private string GetRiskLevel()
    {
        var levels = new[] { "Low", "Moderate", "High", "Critical" };
        return levels[_random.Next(levels.Length)];
    }

    private string GenerateMockPrediction(string caseDetails, bool isFavorable, double confidence)
    {
        var outcome = isFavorable ? "Favorable" : "Unfavorable";
        var confidencePercent = (int)(confidence * 100);
        var caseStrength = _random.Next(3, 6); // 3-5 stars
        
        var predictionFactors = _random.Next(3, 6);
        var positiveFactors = _random.Next(1, predictionFactors);
        var negativeFactors = predictionFactors - positiveFactors;

        return $"""
# AI-Powered Case Outcome Prediction

## Prediction Summary
- **Predicted Outcome**: {outcome}
- **Confidence Level**: {confidencePercent}%
- **Case Strength**: {new string('★', caseStrength)}{new string('☆', 5 - caseStrength)}
- **Prediction Certainty**: {GetCertaintyLevel(confidence)}

## Key Influencing Factors
### Positive Factors ({positiveFactors} identified)
""" + 
string.Join("\n", Enumerable.Range(1, positiveFactors).Select(i => 
    $"- {GetPositiveFactor()} (Impact: {_random.Next(20, 81)}%)")) +
$"""

### Negative Factors ({negativeFactors} identified)
""" +
string.Join("\n", Enumerable.Range(1, negativeFactors).Select(i => 
    $"- {GetNegativeFactor()} (Impact: {_random.Next(20, 81)}%)")) +
$"""

## Risk Assessment Matrix
- **Procedural Risks**: {_random.Next(1, 6)}/10
- **Evidentiary Risks**: {_random.Next(1, 6)}/10  
- **Legal Precedent Risks**: {_random.Next(1, 6)}/10
- **Witness Reliability Risks**: {_random.Next(1, 6)}/10

## Strategic Considerations
- **Recommended Approach**: {_recommendations[_random.Next(_recommendations.Count)]}
- **Timeline Impact**: {_random.Next(-20, 21)}% vs standard cases
- **Resource Requirements**: {_random.Next(80, 121)}% of typical case

## Confidence Intervals
- **68% Confidence Range**: {Math.Max(0, confidencePercent - 10)}% to {Math.Min(100, confidencePercent + 10)}%
- **95% Confidence Range**: {Math.Max(0, confidencePercent - 20)}% to {Math.Min(100, confidencePercent + 20)}%

*This prediction is generated by Mock AI Service for development testing. Real AI predictions would incorporate more comprehensive data analysis.*
""";
    }

    private string GetPositiveFactor()
    {
        var factors = new[]
        {
            "Strong documentary evidence",
            "Favorable legal precedents",
            "Credible witness testimony", 
            "Clear statutory support",
            "Procedural advantages",
            "Jurisdictional benefits"
        };
        return factors[_random.Next(factors.Length)];
    }

    private string GetNegativeFactor()
    {
        var factors = new[]
        {
            "Limited documentary evidence",
            "Unfavorable case law",
            "Witness credibility concerns",
            "Statutory interpretation challenges", 
            "Procedural complications",
            "Jurisdictional uncertainties"
        };
        return factors[_random.Next(factors.Length)];
    }

    private string GetCertaintyLevel(double confidence)
    {
        if (confidence >= 0.8) return "High";
        if (confidence >= 0.6) return "Moderate";
        return "Low";
    }

    private string GenerateMockSummary(string? documentText)
    {
        var docLength = documentText?.Length ?? 0;
        var summaryLength = Math.Max(50, Math.Min(300, docLength / 10));
        var keyPoints = _random.Next(3, 6);

        return $"""
# AI-Generated Legal Document Summary

## Executive Summary
This {GetDocumentType(docLength)} has been analyzed and summarized to highlight the most critical legal elements and implications. The document demonstrates {GetDocumentQuality()} with clear legal reasoning and appropriate structure.

## Key Points Identified
""" + 
string.Join("\n", Enumerable.Range(1, keyPoints).Select(i => 
    $"{i}. {GetSummaryPoint()} (Significance: {_random.Next(60, 96)}%)")) +
$"""

## Critical Elements
- **Primary Legal Argument**: {GetLegalArgument()}
- **Supporting Evidence**: {_random.Next(3, 8)} key pieces identified
- **Procedural Requirements**: {_random.Next(2, 6)} critical procedures noted
- **Timeline Considerations**: {_random.Next(1, 4)} key deadlines highlighted

## Risk Assessment
- **Overall Risk Level**: {GetRiskLevel()}
- **Compliance Status**: {_random.Next(70, 96)}% compliant
- **Completion Status**: {_random.Next(80, 101)}% complete

## Summary Metrics
- **Original Document**: {docLength} characters
- **Summary Length**: {summaryLength} characters
- **Compression Ratio**: {Math.Round((double)docLength / summaryLength, 1)}:1
- **Key Information Retention**: {_random.Next(85, 96)}%

*This summary was generated by Mock AI Service for development purposes. Real AI summarization would provide more nuanced and accurate document analysis.*
""";
    }

    private string GetDocumentType(int length)
    {
        if (length > 2000) return "comprehensive legal brief";
        if (length > 1000) return "detailed legal memorandum";
        if (length > 500) return "standard legal document";
        return "legal notice or filing";
    }

    private string GetDocumentQuality()
    {
        var qualities = new[]
        {
            "good procedural compliance",
            "strong evidentiary support", 
            "clear legal reasoning",
            "thorough documentation",
            "comprehensive analysis",
            "professional formatting"
        };
        return qualities[_random.Next(qualities.Length)];
    }

    private string GetSummaryPoint()
    {
        var points = new[]
        {
            "Identification of key legal issues and applicable statutes",
            "Analysis of relevant case law and precedent applications",
            "Assessment of evidentiary strength and admissibility considerations",
            "Evaluation of procedural requirements and compliance status",
            "Identification of potential counterarguments and defensive strategies",
            "Timeline analysis with critical milestones and deadlines"
        };
        return points[_random.Next(points.Length)];
    }

    private string GetLegalArgument()
    {
        var arguments = new[]
        {
            "Contractual breach with clear liability established",
            "Statutory violation with supporting evidence",
            "Procedural error requiring judicial review",
            "Constitutional issue with significant implications",
            "Tort claim with demonstrable damages",
            "Regulatory compliance matter with documentation"
        };
        return arguments[_random.Next(arguments.Length)];
    }
}