using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Core.Interfaces.Services;

public interface IJustiaService
{
    Task<IEnumerable<JustiaSearchResult>> SearchStatutesAsync(
        string query,
        string? jurisdiction = null,
        string? code = null,
        string? category = null,
        int limit = 50,
        int offset = 0);

    Task<LegalStatute?> GetStatuteAsync(string code, string? section = null);

    Task<IEnumerable<LegalStatute>> GetRelatedStatutesAsync(string code, string section, int limit = 10);

    Task<IEnumerable<JustiaSearchResult>> SearchRegulationsAsync(
        string query,
        string? agency = null,
        string? jurisdiction = null,
        int limit = 50,
        int offset = 0);

    Task<IEnumerable<JustiaSearchResult>> UnifiedSearchAsync(
        string query,
        string? jurisdiction = null,
        string? sourceType = null,
        int limit = 50,
        int offset = 0);
}