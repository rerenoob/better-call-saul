using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Core.Services;

public interface ICourtListenerService
{
    Task<IEnumerable<LegalCase>> SearchCasesAsync(
        string query,
        string? jurisdiction = null,
        string? court = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 50,
        int offset = 0);

    Task<LegalCase?> GetCaseByCitationAsync(string citation);

    Task<CourtOpinion?> GetOpinionAsync(string opinionId);

    Task<IEnumerable<LegalCase>> GetRelatedCasesAsync(string citation, int limit = 10);

    Task<IEnumerable<LegalCase>> SearchByLegalTopicsAsync(IEnumerable<string> topics, int limit = 20);
}