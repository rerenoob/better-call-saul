using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.LegalResearch;

public class CourtListenerService : ICourtListenerService
{
    private readonly CourtListenerClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILegalResearchRepository _legalResearchRepository;
    private readonly ILogger<CourtListenerService> _logger;
    private const int CacheDurationMinutes = 60;

    public CourtListenerService(
        CourtListenerClient client,
        IMemoryCache cache,
        ILegalResearchRepository legalResearchRepository,
        ILogger<CourtListenerService> logger)
    {
        _client = client;
        _cache = cache;
        _legalResearchRepository = legalResearchRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<LegalCase>> SearchCasesAsync(
        string query,
        string? jurisdiction = null,
        string? court = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 50,
        int offset = 0)
    {
        var cacheKey = $"courtlistener_search_{query}_{jurisdiction}_{court}_{startDate}_{endDate}_{limit}_{offset}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<LegalCase>? cachedResults) && cachedResults != null)
        {
            return cachedResults;
        }

        // Check NoSQL first for persistent storage using existing search methods
        var searchResults = await _legalResearchRepository.SearchTextAsync(query, limit);
        
        if (searchResults.Any())
        {
            // Use cached NoSQL results if available
            var legalCases = ConvertToLegalCases(searchResults);
            _cache.Set(cacheKey, legalCases, TimeSpan.FromMinutes(CacheDurationMinutes));
            return legalCases;
        }

        try
        {
            // Build search parameters
            var searchParams = new Dictionary<string, string>
            {
                ["q"] = query,
                ["type"] = "o",
                ["order_by"] = "score desc",
                ["page_size"] = limit.ToString(),
                ["page"] = (offset / limit + 1).ToString()
            };

            if (!string.IsNullOrEmpty(jurisdiction))
                searchParams["court"] = jurisdiction;

            if (!string.IsNullOrEmpty(court))
                searchParams["court_exact"] = court;

            if (startDate.HasValue)
                searchParams["filed_after"] = startDate.Value.ToString("yyyy-MM-dd");

            if (endDate.HasValue)
                searchParams["filed_before"] = endDate.Value.ToString("yyyy-MM-dd");

            var queryString = string.Join("&", searchParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var endpoint = $"/search/?{queryString}";

            // In a real implementation, this would call the actual CourtListener API
            // For now, we'll return mock data
            var results = GenerateMockSearchResults(query, limit);

            // Store results in NoSQL for future reference
            await StoreSearchResultsInNoSQLAsync(query, results);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cases in CourtListener");
            throw;
        }
    }

    public Task<LegalCase?> GetCaseByCitationAsync(string citation)
    {
        var cacheKey = $"courtlistener_case_{citation}";

        if (_cache.TryGetValue(cacheKey, out LegalCase? cachedCase) && cachedCase != null)
        {
            return Task.FromResult<LegalCase?>(cachedCase);
        }

        try
        {
            // In a real implementation, this would call the CourtListener API
            // For now, return mock data
            var legalCase = GenerateMockCaseFromCitation(citation);

            if (legalCase != null)
            {
                _cache.Set(cacheKey, legalCase, TimeSpan.FromMinutes(CacheDurationMinutes));
            }

            return Task.FromResult<LegalCase?>(legalCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting case by citation: {Citation}", citation);
            throw;
        }
    }

    public Task<CourtOpinion?> GetOpinionAsync(string opinionId)
    {
        var cacheKey = $"courtlistener_opinion_{opinionId}";

        if (_cache.TryGetValue(cacheKey, out CourtOpinion? cachedOpinion) && cachedOpinion != null)
        {
            return Task.FromResult<CourtOpinion?>(cachedOpinion);
        }

        try
        {
            // In a real implementation, this would call the CourtListener API
            // For now, return mock data
            var opinion = GenerateMockOpinion(opinionId);

            if (opinion != null)
            {
                _cache.Set(cacheKey, opinion, TimeSpan.FromMinutes(CacheDurationMinutes));
            }

            return Task.FromResult<CourtOpinion?>(opinion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opinion: {OpinionId}", opinionId);
            throw;
        }
    }

    public Task<IEnumerable<LegalCase>> GetRelatedCasesAsync(string citation, int limit = 10)
    {
        var cacheKey = $"courtlistener_related_{citation}_{limit}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<LegalCase>? cachedResults) && cachedResults != null)
        {
            return Task.FromResult(cachedResults);
        }

        try
        {
            // In a real implementation, this would analyze citations and find related cases
            // For now, return mock data
            var results = GenerateMockRelatedCases(citation, limit);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related cases for citation: {Citation}", citation);
            throw;
        }
    }

    public Task<IEnumerable<LegalCase>> SearchByLegalTopicsAsync(IEnumerable<string> topics, int limit = 20)
    {
        var topicsKey = string.Join("_", topics.OrderBy(t => t));
        var cacheKey = $"courtlistener_topics_{topicsKey}_{limit}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<LegalCase>? cachedResults) && cachedResults != null)
        {
            return Task.FromResult(cachedResults);
        }

        try
        {
            // In a real implementation, this would search by legal topics/keywords
            // For now, return mock data
            var results = GenerateMockTopicSearchResults(topics, limit);

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheDurationMinutes));
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by legal topics");
            throw;
        }
    }

    private IEnumerable<LegalCase> GenerateMockSearchResults(string query, int limit)
    {
        var results = new List<LegalCase>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new LegalCase
            {
                Citation = $"123 U.S. {100 + i}",
                Title = $"Sample Case {i + 1} related to {query}",
                Summary = $"This is a summary of case {i + 1} related to the search query '{query}'",
                Court = "Supreme Court of the United States",
                Jurisdiction = "federal",
                DecisionDate = DateTime.Now.AddYears(-i),
                DocketNumber = $"20-{1000 + i}",
                Judge = "Justice Sample",
                RelevanceScore = 0.9m - (i * 0.1m),
                RetrievedAt = DateTime.UtcNow
            });
        }

        return results;
    }

    private LegalCase? GenerateMockCaseFromCitation(string citation)
    {
        return new LegalCase
        {
            Citation = citation,
            Title = $"Important Case: {citation}",
            Summary = $"This is a detailed summary of the case with citation {citation}. It involves important legal principles and precedents.",
            Court = "Supreme Court of the United States",
            Jurisdiction = "federal",
            DecisionDate = DateTime.Now.AddYears(-2),
            DocketNumber = "20-1234",
            Judge = "Chief Justice Example",
            FullText = $"Full text of the opinion for case {citation} would appear here...",
            RelevanceScore = 0.95m,
            RetrievedAt = DateTime.UtcNow
        };
    }

    private CourtOpinion? GenerateMockOpinion(string opinionId)
    {
        return new CourtOpinion
        {
            Citation = $"123 U.S. 456",
            CaseName = $"Sample v. Example ({opinionId})",
            Court = "Supreme Court of the United States",
            DecisionDate = DateTime.Now.AddYears(-1),
            DocketNumber = "20-5678",
            Author = "Justice Writer",
            OpinionText = $"This is the full text of opinion {opinionId}. It contains the court's reasoning and decision.",
            OpinionType = "Majority",
            PageCount = 15,
            Headnotes = "Key legal points from this opinion",
            Syllabus = "Case syllabus summarizing the decision",
            Holding = "The court held that...",
            RelevanceScore = 0.92m,
            RetrievedAt = DateTime.UtcNow
        };
    }

    private IEnumerable<LegalCase> GenerateMockRelatedCases(string citation, int limit)
    {
        var results = new List<LegalCase>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new LegalCase
            {
                Citation = $"124 U.S. {200 + i}",
                Title = $"Related Case {i + 1} to {citation}",
                Summary = $"This case is related to {citation} and deals with similar legal issues",
                Court = "Supreme Court of the United States",
                Jurisdiction = "federal",
                DecisionDate = DateTime.Now.AddYears(-i - 1),
                DocketNumber = $"21-{2000 + i}",
                Judge = "Justice Related",
                RelevanceScore = 0.85m - (i * 0.05m),
                RetrievedAt = DateTime.UtcNow
            });
        }

        return results;
    }

    private IEnumerable<LegalCase> GenerateMockTopicSearchResults(IEnumerable<string> topics, int limit)
    {
        var topicString = string.Join(", ", topics);
        var results = new List<LegalCase>();
        
        for (int i = 0; i < limit; i++)
        {
            results.Add(new LegalCase
            {
                Citation = $"125 U.S. {300 + i}",
                Title = $"Case about {topicString} - {i + 1}",
                Summary = $"This case addresses legal issues related to {topicString}",
                Court = "Supreme Court of the United States",
                Jurisdiction = "federal",
                DecisionDate = DateTime.Now.AddYears(-i - 2),
                DocketNumber = $"22-{3000 + i}",
                Judge = "Justice Topic",
                RelevanceScore = 0.88m - (i * 0.04m),
                RetrievedAt = DateTime.UtcNow
            });
        }

        return results;
    }



    private async Task StoreSearchResultsInNoSQLAsync(string query, IEnumerable<LegalCase> results)
    {
        try
        {
            foreach (var legalCase in results)
            {
                var researchDocument = new LegalResearchDocument
                {
                    Citation = legalCase.Citation,
                    Title = legalCase.Title,
                    Summary = legalCase.Summary,
                    Court = legalCase.Court,
                    Jurisdiction = legalCase.Jurisdiction,
                    DecisionDate = legalCase.DecisionDate,
                    DocketNumber = legalCase.DocketNumber,
                    Judge = legalCase.Judge,
                    FullText = legalCase.FullText,
                    RelevanceScore = legalCase.RelevanceScore,
                    Type = LegalDocumentType.CaseOpinion,
                    Source = "CourtListener",
                    RetrievedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                // Add search query to metadata
                researchDocument.Metadata.SearchQueries.Add(query);
                researchDocument.Metadata.Keywords.AddRange(ExtractKeyTopics(legalCase.Summary));

                await _legalResearchRepository.CreateAsync(researchDocument);
            }
            
            _logger.LogInformation("Stored {Count} legal research results in NoSQL for query: {Query}", results.Count(), query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store legal research results in NoSQL for query: {Query}", query);
            // Don't throw - this is a secondary storage operation
        }
    }

    private IEnumerable<LegalCase> ConvertToLegalCases(List<LegalResearchDocument> researchDocuments)
    {
        return researchDocuments.Select(doc => new LegalCase
        {
            Citation = doc.Citation,
            Title = doc.Title,
            Summary = doc.Summary,
            Court = doc.Court,
            Jurisdiction = doc.Jurisdiction,
            DecisionDate = doc.DecisionDate,
            DocketNumber = doc.DocketNumber,
            Judge = doc.Judge,
            FullText = doc.FullText,
            RelevanceScore = doc.RelevanceScore,
            RetrievedAt = doc.RetrievedAt
        });
    }

    private List<string> ExtractKeyTopics(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        // Simple keyword extraction - in a real implementation, this would use NLP
        var commonLegalTerms = new[] { "contract", "tort", "criminal", "constitutional", "civil", "property", "criminal", "evidence", "procedure" };
        var topics = new List<string>();

        foreach (var term in commonLegalTerms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                topics.Add(term);
            }
        }

        return topics;
    }

    private List<string> ExtractCitations(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        // Simple citation extraction - in a real implementation, this would use regex patterns for legal citations
        var citations = new List<string>();

        // This is a simplified example
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length - 2; i++)
        {
            if (int.TryParse(words[i], out _) && words[i + 1].Contains("U.S.") && int.TryParse(words[i + 2], out _))
            {
                citations.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
            }
        }

        return citations;
    }
}