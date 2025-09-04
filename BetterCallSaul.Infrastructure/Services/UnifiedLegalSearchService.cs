using BetterCallSaul.Core.Models;
using BetterCallSaul.Core.Services;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services;

public class UnifiedLegalSearchService
{
    private readonly ICourtListenerService _courtListenerService;
    private readonly IJustiaService _justiaService;
    private readonly ILogger<UnifiedLegalSearchService> _logger;

    public UnifiedLegalSearchService(
        ICourtListenerService courtListenerService,
        IJustiaService justiaService,
        ILogger<UnifiedLegalSearchService> logger)
    {
        _courtListenerService = courtListenerService;
        _justiaService = justiaService;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> SearchAllSourcesAsync(
        string query,
        string? jurisdiction = null,
        int limit = 50,
        int offset = 0)
    {
        try
        {
            // Search both CourtListener and Justia in parallel
            var courtListenerTask = _courtListenerService.SearchCasesAsync(
                query, jurisdiction, null, null, null, limit / 2, offset);

            var justiaTask = _justiaService.UnifiedSearchAsync(
                query, jurisdiction, null, limit / 2, offset);

            await Task.WhenAll(courtListenerTask, justiaTask);

            var courtListenerResults = await courtListenerTask;
            var justiaResults = await justiaTask;

            // Combine and order by relevance score
            var combinedResults = courtListenerResults
                .Cast<object>()
                .Concat(justiaResults)
                .OrderByDescending(result => GetRelevanceScore(result))
                .Take(limit);

            return combinedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing unified search across all legal sources");
            
            // Fallback: try individual services
            try
            {
                var courtListenerResults = await _courtListenerService.SearchCasesAsync(
                    query, jurisdiction, null, null, null, limit, offset);
                return courtListenerResults.Cast<object>();
            }
            catch
            {
                try
                {
                    var justiaResults = await _justiaService.UnifiedSearchAsync(
                        query, jurisdiction, null, limit, offset);
                    return justiaResults.Cast<object>();
                }
                catch
                {
                    throw new Exception("All legal search services are unavailable", ex);
                }
            }
        }
    }

    public async Task<IEnumerable<object>> SearchByTopicAcrossSourcesAsync(
        IEnumerable<string> topics,
        string? jurisdiction = null,
        int limit = 20,
        int offset = 0)
    {
        try
        {
            var courtListenerTask = _courtListenerService.SearchByLegalTopicsAsync(
                topics, limit / 2);

            var justiaTask = _justiaService.UnifiedSearchAsync(
                string.Join(" ", topics), jurisdiction, null, limit / 2, offset);

            await Task.WhenAll(courtListenerTask, justiaTask);

            var courtListenerResults = await courtListenerTask;
            var justiaResults = await justiaTask;

            // Combine and order by relevance score
            var combinedResults = courtListenerResults
                .Cast<object>()
                .Concat(justiaResults)
                .OrderByDescending(result => GetRelevanceScore(result))
                .Take(limit);

            return combinedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by topics across legal sources");
            throw;
        }
    }

    public async Task<object?> GetLegalResourceAsync(string sourceType, string identifier)
    {
        try
        {
            return sourceType.ToLower() switch
            {
                "case" or "courtlistener" => await _courtListenerService.GetCaseByCitationAsync(identifier),
                "statute" or "justia" => await _justiaService.GetStatuteAsync(identifier),
                "opinion" => await _courtListenerService.GetOpinionAsync(identifier),
                _ => throw new ArgumentException("Invalid source type")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting legal resource: {SourceType} {Identifier}", sourceType, identifier);
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetCrossReferencesAsync(string citation, string sourceType, int limit = 10)
    {
        try
        {
            if (sourceType.Equals("case", StringComparison.OrdinalIgnoreCase) ||
                sourceType.Equals("courtlistener", StringComparison.OrdinalIgnoreCase))
            {
                var relatedCases = await _courtListenerService.GetRelatedCasesAsync(citation, limit);
                return relatedCases.Cast<object>();
            }
            else if (sourceType.Equals("statute", StringComparison.OrdinalIgnoreCase) ||
                     sourceType.Equals("justia", StringComparison.OrdinalIgnoreCase))
            {
                // Parse statute code and section from citation
                var parts = citation.Split('§');
                if (parts.Length == 2)
                {
                    var code = parts[0].Trim();
                    var section = parts[1].Trim();
                    var relatedStatutes = await _justiaService.GetRelatedStatutesAsync(code, section, limit);
                    return relatedStatutes.Cast<object>();
                }
            }

            throw new ArgumentException("Invalid source type or citation format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cross references for: {Citation} ({SourceType})", citation, sourceType);
            throw;
        }
    }

    private decimal GetRelevanceScore(object result)
    {
        return result switch
        {
            LegalCase legalCase => legalCase.RelevanceScore,
            JustiaSearchResult justiaResult => justiaResult.RelevanceScore,
            LegalStatute statute => statute.RelevanceScore,
            _ => 0.5m
        };
    }
}