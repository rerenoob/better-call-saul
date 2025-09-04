using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Core.Services;

public interface ICaseMatchingService
{
    Task<IEnumerable<CaseMatch>> FindSimilarCasesAsync(
        Guid caseId,
        string? jurisdiction = null,
        int limit = 10,
        decimal minSimilarity = 0.6m);

    Task<IEnumerable<CaseMatch>> FindSimilarCasesByTextAsync(
        string caseText,
        string? jurisdiction = null,
        int limit = 10,
        decimal minSimilarity = 0.6m);

    Task<CaseMatch?> GetBestMatchAsync(
        Guid caseId,
        string? jurisdiction = null,
        decimal minSimilarity = 0.7m);

    Task<IEnumerable<CaseMatch>> FindPrecedentsAsync(
        Guid caseId,
        string? jurisdiction = null,
        int limit = 5,
        decimal minSimilarity = 0.7m);

    Task<decimal> CalculateSimilarityAsync(
        Guid caseId1,
        Guid caseId2);

    Task<decimal> CalculateTextSimilarityAsync(
        string text1,
        string text2);

    Task<IEnumerable<CaseMatch>> GetCaseMatchHistoryAsync(
        Guid caseId,
        int limit = 20,
        int offset = 0);

    Task UpdateMatchingCriteriaAsync(MatchingCriteria criteria);

    Task<IEnumerable<MatchingCriteria>> GetMatchingCriteriaAsync();
}