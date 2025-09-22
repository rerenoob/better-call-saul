using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Core.Interfaces.Repositories;

namespace BetterCallSaul.Core.Interfaces.Services;

public interface ICaseManagementService
{
    Task<Case> CreateCaseAsync(Case caseData, CancellationToken cancellationToken = default);
    Task<Case> UpdateCaseAsync(Case caseData, CancellationToken cancellationToken = default);
    Task<Case> GetCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<CaseWithDocuments> GetCaseWithDocumentsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<List<Case>> GetCasesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseDocument>> SearchCasesAsync(CaseSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task DeleteCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<CaseAnalysisStats> GetCaseAnalysisStatsAsync(Guid caseId, CancellationToken cancellationToken = default);
}

public class CaseWithDocuments
{
    public Case Case { get; set; } = null!;
    public List<DocumentInfo> Documents { get; set; } = new();
    public List<CaseAnalysisResult> Analyses { get; set; } = new();
}