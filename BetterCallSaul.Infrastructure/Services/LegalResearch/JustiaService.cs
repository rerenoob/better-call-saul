using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.LegalResearch;

public class JustiaService : IJustiaService
{
    private readonly JustiaClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<JustiaService> _logger;
    private const int CacheDurationMinutes = 60;

    public JustiaService(
        JustiaClient client,
        IMemoryCache cache,
        ILogger<JustiaService> logger)
    {
        _client = client;
        _cache = cache;
        _logger = logger;
    }

    public Task<IEnumerable<JustiaSearchResult>> SearchStatutesAsync(
        string query,
        string? jurisdiction = null,
        string? code = null,
        string? category = null,
        int limit = 50,
        int offset = 0)
    {
        var cacheKey = $"justia_statutes_{query}_{jurisdiction}_{code}_{category}_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<JustiaSearchResult>? cachedResults) && cachedResults != null)
        {
            return Task.FromResult(cachedResults);
        }

        try
        {
            // In a real implementation, this would call the Justia API
            // For now, return mock data
            var results = GenerateMockStatuteSearchResults(query, jurisdiction, code, category, limit);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching statutes in Justia");
            throw;
        }
    }

    public Task<LegalStatute?> GetStatuteAsync(string code, string? section = null)
    {
        var cacheKey = $"justia_statute_{code}_{section}";

        if (_cache.TryGetValue(cacheKey, out LegalStatute? cachedStatute) && cachedStatute != null)
        {
            return Task.FromResult<LegalStatute?>(cachedStatute);
        }

        try
        {
            // In a real implementation, this would call the Justia API
            // For now, return mock data
            var statute = GenerateMockStatute(code, section);

            if (statute != null)
            {
                _cache.Set(cacheKey, statute, TimeSpan.FromMinutes(CacheDurationMinutes));
            }

            return Task.FromResult<LegalStatute?>(statute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statute: {Code} {Section}", code, section);
            throw;
        }
    }

    public Task<IEnumerable<LegalStatute>> GetRelatedStatutesAsync(string code, string section, int limit = 10)
    {
        var cacheKey = $"justia_related_{code}_{section}_{limit}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<LegalStatute>? cachedResults) && cachedResults != null)
        {
            return Task.FromResult(cachedResults);
        }

        try
        {
            // In a real implementation, this would find related statutes
            // For now, return mock data
            var results = GenerateMockRelatedStatutes(code, section, limit);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related statutes for: {Code} {Section}", code, section);
            throw;
        }
    }

    public Task<IEnumerable<JustiaSearchResult>> SearchRegulationsAsync(
        string query,
        string? agency = null,
        string? jurisdiction = null,
        int limit = 50,
        int offset = 0)
    {
        var cacheKey = $"justia_regulations_{query}_{agency}_{jurisdiction}_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<JustiaSearchResult>? cachedResults) && cachedResults != null)
        {
            return Task.FromResult(cachedResults);
        }

        try
        {
            // In a real implementation, this would call the Justia API
            // For now, return mock data
            var results = GenerateMockRegulationSearchResults(query, agency, jurisdiction, limit);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching regulations in Justia");
            throw;
        }
    }

    public async Task<IEnumerable<JustiaSearchResult>> UnifiedSearchAsync(
        string query,
        string? jurisdiction = null,
        string? sourceType = null,
        int limit = 50,
        int offset = 0)
    {
        var cacheKey = $"justia_unified_{query}_{jurisdiction}_{sourceType}_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<JustiaSearchResult>? cachedResults) && cachedResults != null)
        {
            return cachedResults;
        }

        try
        {
            // In a real implementation, this would search across multiple Justia sources
            // For now, return mock data combining statutes and regulations
            var statuteResults = await SearchStatutesAsync(query, jurisdiction, null, null, limit / 2, offset);
            var regulationResults = await SearchRegulationsAsync(query, null, jurisdiction, limit / 2, offset);

            var combinedResults = statuteResults
                .Cast<JustiaSearchResult>()
                .Concat(regulationResults)
                .OrderByDescending(r => r.RelevanceScore)
                .Take(limit);

            _cache.Set(cacheKey, combinedResults, TimeSpan.FromMinutes(CacheDurationMinutes));
            return combinedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing unified search in Justia");
            throw;
        }
    }

    private IEnumerable<JustiaSearchResult> GenerateMockStatuteSearchResults(
        string query, string? jurisdiction, string? code, string? category, int limit)
    {
        var results = new List<JustiaSearchResult>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new JustiaSearchResult
            {
                Title = $"Statute {i + 1} related to {query}",
                Summary = $"This statute addresses legal issues related to {query}",
                Source = $"{(code ?? "USC")} § {100 + i}",
                Jurisdiction = jurisdiction ?? "federal",
                Court = null,
                DecisionDate = DateTime.Now.AddYears(-i - 1),
                Citation = $"{(code ?? "USC")} § {100 + i}",
                Type = "Statute",
                RelevanceScore = 0.9m - (i * 0.1m),
                Database = "Justia"
            });
        }

        return results;
    }

    private LegalStatute? GenerateMockStatute(string code, string? section)
    {
        return new LegalStatute
        {
            Code = code,
            Section = section ?? "1",
            Title = $"Important {code} Statute",
            Description = $"This statute in {code} addresses important legal principles related to its subject matter",
            Jurisdiction = "federal",
            Category = "Criminal",
            EffectiveDate = DateTime.Now.AddYears(-5),
            FullText = $"Full text of {code} {section} would appear here...",
            RelevanceScore = 0.95m,
            Database = "Justia"
        };
    }

    private IEnumerable<LegalStatute> GenerateMockRelatedStatutes(string code, string section, int limit)
    {
        var results = new List<LegalStatute>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new LegalStatute
            {
                Code = code,
                Section = $"{int.Parse(section) + i + 1}",
                Title = $"Related Statute to {code} {section}",
                Description = $"This statute is related to {code} {section} and deals with similar legal concepts",
                Jurisdiction = "federal",
                Category = "Criminal",
                EffectiveDate = DateTime.Now.AddYears(-i - 2),
                RelevanceScore = 0.85m - (i * 0.05m),
                Database = "Justia"
            });
        }

        return results;
    }

    private IEnumerable<JustiaSearchResult> GenerateMockRegulationSearchResults(
        string query, string? agency, string? jurisdiction, int limit)
    {
        var results = new List<JustiaSearchResult>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new JustiaSearchResult
            {
                Title = $"Regulation {i + 1} related to {query}",
                Summary = $"This regulation from {(agency ?? "FDA")} addresses issues related to {query}",
                Source = agency ?? "Federal Regulations",
                Jurisdiction = jurisdiction ?? "federal",
                Court = null,
                DecisionDate = DateTime.Now.AddYears(-i),
                Citation = $"CFR § {200 + i}",
                Type = "Regulation",
                RelevanceScore = 0.88m - (i * 0.08m),
                Database = "Justia"
            });
        }

        return results;
    }
}