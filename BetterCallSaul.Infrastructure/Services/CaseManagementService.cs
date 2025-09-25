using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Infrastructure.Services;

public class CaseManagementService : ICaseManagementService
{
    private readonly BetterCallSaulContext _sqlContext;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly ILogger<CaseManagementService> _logger;

    public CaseManagementService(
        BetterCallSaulContext sqlContext,
        ICaseDocumentRepository caseDocumentRepository,
        ILogger<CaseManagementService> logger)
    {
        _sqlContext = sqlContext;
        _caseDocumentRepository = caseDocumentRepository;
        _logger = logger;
    }

    public async Task<Case> CreateCaseAsync(Case caseData, CancellationToken cancellationToken = default)
    {
        // Write to SQL only - NoSQL is for document/analysis data
        _sqlContext.Cases.Add(caseData);
        await _sqlContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created case {CaseId}", caseData.Id);
        return caseData;
    }

    public async Task<Case> UpdateCaseAsync(Case caseData, CancellationToken cancellationToken = default)
    {
        // Update SQL only - NoSQL is for document/analysis data
        _sqlContext.Cases.Update(caseData);
        await _sqlContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated case {CaseId}", caseData.Id);
        return caseData;
    }

    public async Task<Case> GetCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseData = await _sqlContext.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (caseData == null)
        {
            throw new KeyNotFoundException($"Case {caseId} not found");
        }

        return caseData;
    }

    public async Task<CaseWithDocuments> GetCaseWithDocumentsAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseData = await GetCaseAsync(caseId, cancellationToken);
        
        // Get detailed documents and analyses from NoSQL
        var caseDocument = await _caseDocumentRepository.GetByIdAsync(caseId);
        
        return new CaseWithDocuments
        {
            Case = caseData,
            Documents = caseDocument?.Documents ?? new List<DocumentInfo>(),
            Analyses = caseDocument?.Analyses ?? new List<CaseAnalysisResult>()
        };
    }

    public async Task<List<Case>> GetCasesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _sqlContext.Cases
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CaseDocument>> SearchCasesAsync(CaseSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        // Use NoSQL for complex searches (full-text, tags, etc.)
        return await _caseDocumentRepository.SearchAsync(criteria);
    }

    public async Task DeleteCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        // Delete from SQL
        var caseData = await _sqlContext.Cases.FindAsync(new object[] { caseId }, cancellationToken);
        if (caseData != null)
        {
            _sqlContext.Cases.Remove(caseData);
            await _sqlContext.SaveChangesAsync(cancellationToken);
        }

        // Delete from NoSQL (optional - NoSQL stores document/analysis data)
        try
        {
            await _caseDocumentRepository.DeleteAsync(caseId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete NoSQL document for case {CaseId}", caseId);
        }
        
        _logger.LogInformation("Deleted case {CaseId}", caseId);
    }

    public async Task<CaseAnalysisStats> GetCaseAnalysisStatsAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        // Use NoSQL for analysis stats (more efficient for complex aggregations)
        return await _caseDocumentRepository.GetAnalysisStatsAsync(caseId);
    }
}