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
}