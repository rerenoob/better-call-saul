using System.Text.Json;
using better_call_saul.Data;
using better_call_saul.Models;
using Microsoft.EntityFrameworkCore;

namespace better_call_saul.Services;

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly IAzureOpenAIService _aiService;
    private readonly ICaseService _caseService;
    private readonly ILoggerService _logger;

    public DocumentAnalysisService(
        ApplicationDbContext context,
        IAzureOpenAIService aiService,
        ICaseService caseService,
        ILoggerService logger)
    {
        _context = context;
        _aiService = aiService;
        _caseService = caseService;
        _logger = logger;
    }

    public async Task<CaseAnalysis> AnalyzeCaseDocumentsAsync(int caseId, string userId)
    {
        // Verify user has access to the case
        var caseEntity = await _caseService.GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            throw new UnauthorizedAccessException("User does not have access to this case");
        }

        // Get all documents for the case
        var documents = await _context.Documents
            .Where(d => d.CaseId.HasValue && d.CaseId.Value == caseId && !d.IsDeleted)
            .ToListAsync();

        if (documents.Count == 0)
        {
            throw new InvalidOperationException("No documents found for analysis");
        }

        // Combine all document texts
        var combinedText = CombineDocumentTexts(documents);

        // Analyze with AI service
        var analysisResult = await _aiService.AnalyzeDocumentAsync(combinedText, "legal_case");

        if (!analysisResult.Success)
        {
            throw new Exception($"AI analysis failed: {analysisResult.ErrorMessage}");
        }

        // Create and save analysis result
        var caseAnalysis = new CaseAnalysis
        {
            CaseId = caseId,
            Summary = analysisResult.Summary,
            Recommendation = analysisResult.Recommendation,
            KeyPoints = JsonSerializer.Serialize(analysisResult.KeyPoints),
            ConfidenceScore = analysisResult.ConfidenceScore,
            AnalyzedAt = DateTime.UtcNow,
            AnalysisVersion = "1.0"
        };

        _context.CaseAnalyses.Add(caseAnalysis);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Case analysis completed for case {caseId} by user {userId}");

        return caseAnalysis;
    }

    public async Task<CaseAnalysis?> GetLatestAnalysisAsync(int caseId, string userId)
    {
        // Verify user has access to the case
        var caseEntity = await _caseService.GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            throw new UnauthorizedAccessException("User does not have access to this case");
        }

        return await _context.CaseAnalyses
            .Where(ca => ca.CaseId == caseId && !ca.IsDeleted)
            .OrderByDescending(ca => ca.AnalyzedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasAnalysisAsync(int caseId, string userId)
    {
        // Verify user has access to the case
        var caseEntity = await _caseService.GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            throw new UnauthorizedAccessException("User does not have access to this case");
        }

        return await _context.CaseAnalyses
            .AnyAsync(ca => ca.CaseId == caseId && !ca.IsDeleted);
    }

    private string CombineDocumentTexts(List<Document> documents)
    {
        var combined = new System.Text.StringBuilder();
        combined.AppendLine($"Case Analysis - {documents.Count} documents");
        combined.AppendLine("============================================");

        for (int i = 0; i < documents.Count; i++)
        {
            var doc = documents[i];
            combined.AppendLine($"\nDocument {i + 1}: {doc.FileName}");
            combined.AppendLine($"File Type: {doc.FileType}, Pages: {doc.PageCount}");
            combined.AppendLine("--------------------------------------------");
            combined.AppendLine(doc.ExtractedText);
            combined.AppendLine("\n");
        }

        return combined.ToString();
    }
}