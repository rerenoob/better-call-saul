using better_call_saul.Models;
using better_call_saul.Models.ViewModels;

namespace better_call_saul.Services;

public interface ICaseService
{
    Task<IEnumerable<Case>> GetUserCasesAsync(string userId);
    Task<Case?> GetCaseByIdAsync(int caseId, string userId);
    Task<Case> CreateCaseAsync(CaseViewModel model, string userId);
    Task<bool> UpdateCaseAsync(int caseId, CaseViewModel model, string userId);
    Task<bool> DeleteCaseAsync(int caseId, string userId);
    Task<CaseListViewModel> GetUserCaseListAsync(string userId);
    Task<CaseDetailViewModel> GetCaseDetailAsync(int caseId, string userId);
}