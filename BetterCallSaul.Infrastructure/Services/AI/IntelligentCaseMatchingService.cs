using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.AI;

public class IntelligentCaseMatchingService : ICaseMatchingService
{
    private readonly BetterCallSaulContext _context;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly LegalTextSimilarity _textSimilarity;
    private readonly ICourtListenerService _courtListenerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IntelligentCaseMatchingService> _logger;
    private const int CacheDurationMinutes = 30;

    public IntelligentCaseMatchingService(
        BetterCallSaulContext context,
        ICaseDocumentRepository caseDocumentRepository,
        LegalTextSimilarity textSimilarity,
        ICourtListenerService courtListenerService,
        IMemoryCache cache,
        ILogger<IntelligentCaseMatchingService> logger)
    {
        _context = context;
        _caseDocumentRepository = caseDocumentRepository;
        _textSimilarity = textSimilarity;
        _courtListenerService = courtListenerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<CaseMatch>> FindSimilarCasesAsync(
        Guid caseId,
        string? jurisdiction = null,
        int limit = 10,
        decimal minSimilarity = 0.6m)
    {
        var cacheKey = $"case_matches_{caseId}_{jurisdiction}_{limit}_{minSimilarity}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<CaseMatch>? cachedMatches) && cachedMatches != null)
        {
            return cachedMatches;
        }

        try
        {
            var sourceCase = await _context.Cases
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (sourceCase == null)
            {
                throw new ArgumentException("Case not found");
            }

            // Get case text from documents
            var caseText = await GetCaseTextAsync(sourceCase);

            // Search external legal databases
            var externalMatches = await FindExternalSimilarCasesAsync(caseText, jurisdiction, limit * 2, minSimilarity);

            // Filter and rank matches
            var rankedMatches = externalMatches
                .OrderByDescending(m => m.SimilarityScore)
                .Where(m => m.SimilarityScore >= minSimilarity)
                .Take(limit)
                .ToList();

            // Store successful matches in database
            foreach (var match in rankedMatches)
            {
                match.SourceCaseId = caseId;
                await _context.CaseMatches.AddAsync(match);
            }

            await _context.SaveChangesAsync();

            _cache.Set(cacheKey, rankedMatches, TimeSpan.FromMinutes(CacheDurationMinutes));
            return rankedMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cases for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<IEnumerable<CaseMatch>> FindSimilarCasesByTextAsync(
        string caseText,
        string? jurisdiction = null,
        int limit = 10,
        decimal minSimilarity = 0.6m)
    {
        var cacheKey = $"text_matches_{caseText.GetHashCode()}_{jurisdiction}_{limit}_{minSimilarity}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<CaseMatch>? cachedMatches) && cachedMatches != null)
        {
            return cachedMatches;
        }

        try
        {
            var externalMatches = await FindExternalSimilarCasesAsync(caseText, jurisdiction, limit, minSimilarity);

            var rankedMatches = externalMatches
                .OrderByDescending(m => m.SimilarityScore)
                .Where(m => m.SimilarityScore >= minSimilarity)
                .Take(limit)
                .ToList();

            _cache.Set(cacheKey, rankedMatches, TimeSpan.FromMinutes(CacheDurationMinutes));
            return rankedMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cases by text");
            throw;
        }
    }

    public async Task<CaseMatch?> GetBestMatchAsync(
        Guid caseId,
        string? jurisdiction = null,
        decimal minSimilarity = 0.7m)
    {
        try
        {
            var matches = await FindSimilarCasesAsync(caseId, jurisdiction, 1, minSimilarity);
            return matches.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best match for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<IEnumerable<CaseMatch>> FindPrecedentsAsync(
        Guid caseId,
        string? jurisdiction = null,
        int limit = 5,
        decimal minSimilarity = 0.7m)
    {
        try
        {
            var allMatches = await FindSimilarCasesAsync(caseId, jurisdiction, limit * 2, minSimilarity);
            
            // Filter for precedents (higher similarity and from higher courts)
            var precedents = allMatches
                .Where(m => m.SimilarityScore >= 0.8m && m.IsPrecedent)
                .OrderByDescending(m => m.SimilarityScore)
                .Take(limit)
                .ToList();

            return precedents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding precedents for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<decimal> CalculateSimilarityAsync(Guid caseId1, Guid caseId2)
    {
        try
        {
            var case1 = await _context.Cases
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == caseId1);

            var case2 = await _context.Cases
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == caseId2);

            if (case1 == null || case2 == null)
            {
                return 0m;
            }

            var text1 = await GetCaseTextAsync(case1);
            var text2 = await GetCaseTextAsync(case2);

            return _textSimilarity.CalculateOverallSimilarity(text1, text2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating similarity between cases: {CaseId1} and {CaseId2}", caseId1, caseId2);
            return 0m;
        }
    }

    public Task<decimal> CalculateTextSimilarityAsync(string text1, string text2)
    {
        try
        {
            return Task.FromResult(_textSimilarity.CalculateOverallSimilarity(text1, text2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating text similarity");
            return Task.FromResult(0m);
        }
    }

    public async Task<IEnumerable<CaseMatch>> GetCaseMatchHistoryAsync(Guid caseId, int limit = 20, int offset = 0)
    {
        try
        {
            return await _context.CaseMatches
                .Where(m => m.SourceCaseId == caseId)
                .OrderByDescending(m => m.SimilarityScore)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match history for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task UpdateMatchingCriteriaAsync(MatchingCriteria criteria)
    {
        try
        {
            var existing = await _context.MatchingCriteria
                .FirstOrDefaultAsync(c => c.Id == criteria.Id);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(criteria);
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.MatchingCriteria.AddAsync(criteria);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating matching criteria");
            throw;
        }
    }

    public async Task<IEnumerable<MatchingCriteria>> GetMatchingCriteriaAsync()
    {
        try
        {
            return await _context.MatchingCriteria
                .Where(c => c.IsActive)
                .OrderBy(c => c.Priority)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matching criteria");
            throw;
        }
    }

    private async Task<string> GetCaseTextAsync(Case legalCase)
    {
        // Get document texts from NoSQL
        var caseDocument = await _caseDocumentRepository.GetByIdAsync(legalCase.Id);
        var documentTexts = new List<string>();

        if (caseDocument?.Documents != null)
        {
            documentTexts = caseDocument.Documents
                .Where(d => d.IsProcessed && d.ExtractedText?.FullText != null)
                .Select(d => d.ExtractedText!.FullText!)
                .ToList();
        }

        // Combine with case description
        var combinedText = $"{legalCase.Description} {string.Join(" ", documentTexts)}";
        return combinedText;
    }

    private async Task<List<CaseMatch>> FindExternalSimilarCasesAsync(
        string caseText, string? jurisdiction, int limit, decimal minSimilarity)
    {
        var matches = new List<CaseMatch>();

        try
        {
            // Search CourtListener for similar cases
            var courtListenerResults = await _courtListenerService.SearchCasesAsync(
                ExtractKeywords(caseText), jurisdiction, null, null, null, limit * 2, 0);

            foreach (var result in courtListenerResults)
            {
                var similarity = _textSimilarity.CalculateOverallSimilarity(
                    caseText, $"{result.Title} {result.Summary} {result.FullText}");

                if (similarity >= minSimilarity)
                {
                    matches.Add(new CaseMatch
                    {
                        MatchedCaseId = result.Id,
                        MatchedCaseCitation = result.Citation,
                        MatchedCaseTitle = result.Title,
                        SimilarityScore = similarity,
                        MatchType = "Semantic",
                        Reasoning = $"Semantic similarity based on case content",
                        JurisdictionMatch = result.Jurisdiction,
                        LegalIssueMatch = ExtractLegalIssues(caseText),
                        IsPrecedent = IsPrecedent(result),
                        ConfidenceLevel = CalculateConfidenceLevel(similarity, result),
                        KeySimilarities = $"Similar legal issues and facts",
                        KeyDifferences = $"Different jurisdictions or time periods"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching CourtListener for similar cases");
        }

        return matches;
    }

    private string ExtractKeywords(string text)
    {
        // Simple keyword extraction - in real implementation, use NLP
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(word => word.Length > 4)
                       .Distinct()
                       .Take(10);

        return string.Join(" ", words);
    }

    private string ExtractLegalIssues(string text)
    {
        // Simple legal issue extraction
        var legalTerms = new[] { "negligence", "contract", "tort", "constitutional", "criminal", "civil" };
        return string.Join(", ", legalTerms.Where(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private bool IsPrecedent(LegalCase legalCase)
    {
        // Simple precedent detection - higher courts and recent decisions
        return legalCase.Jurisdiction?.Equals("federal", StringComparison.OrdinalIgnoreCase) == true ||
               legalCase.Court?.Contains("Supreme", StringComparison.OrdinalIgnoreCase) == true ||
               legalCase.DecisionDate > DateTime.Now.AddYears(-10);
    }

    private decimal CalculateConfidenceLevel(decimal similarity, LegalCase legalCase)
    {
        // Higher confidence for higher courts and more recent cases
        decimal courtWeight = legalCase.Court?.Contains("Supreme", StringComparison.OrdinalIgnoreCase) == true ? 0.2m : 0.1m;
        decimal recencyWeight = legalCase.DecisionDate > DateTime.Now.AddYears(-5) ? 0.1m : 0.05m;

        return similarity * 0.7m + courtWeight + recencyWeight;
    }
}