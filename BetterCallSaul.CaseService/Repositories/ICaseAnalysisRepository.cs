using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public interface ICaseAnalysisRepository
{
    Task<CaseAnalysisDocument?> GetByIdAsync(string id);
    Task<IEnumerable<CaseAnalysisDocument>> GetByCaseIdAsync(string caseId);
    Task<IEnumerable<CaseAnalysisDocument>> GetByDocumentIdAsync(string documentId);
    Task<CaseAnalysisDocument> CreateAsync(CaseAnalysisDocument analysis);
    Task<CaseAnalysisDocument> UpdateAsync(string id, CaseAnalysisDocument analysis);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}