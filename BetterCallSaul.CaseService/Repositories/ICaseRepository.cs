using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public interface ICaseRepository
{
    Task<CaseDocument?> GetByIdAsync(string id);
    Task<IEnumerable<CaseDocument>> GetByUserIdAsync(string userId);
    Task<IEnumerable<CaseDocument>> GetAllAsync();
    Task<CaseDocument> CreateAsync(CaseDocument caseDocument);
    Task<CaseDocument> UpdateAsync(string id, CaseDocument caseDocument);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}