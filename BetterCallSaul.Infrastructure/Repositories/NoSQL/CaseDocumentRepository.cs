using MongoDB.Driver;
using MongoDB.Bson;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Repositories.NoSQL;

public class CaseDocumentRepository : ICaseDocumentRepository
{
    private readonly IMongoCollection<CaseDocument> _collection;
    private readonly ILogger<CaseDocumentRepository> _logger;

    public CaseDocumentRepository(NoSqlContext context, ILogger<CaseDocumentRepository> logger)
    {
        _collection = context.CaseDocuments;
        _logger = logger;
    }

    public async Task<CaseDocument?> GetByIdAsync(Guid caseId)
    {
        try
        {
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.CaseId, caseId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting case document by ID: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<CaseDocument> CreateAsync(CaseDocument document)
    {
        try
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(document);
            _logger.LogInformation("Created case document for case: {CaseId}", document.CaseId);
            
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating case document for case: {CaseId}", document.CaseId);
            throw;
        }
    }

    public async Task<CaseDocument> UpdateAsync(CaseDocument document)
    {
        try
        {
            document.UpdatedAt = DateTime.UtcNow;
            document.Version++;
            
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.CaseId, document.CaseId);
            var result = await _collection.ReplaceOneAsync(filter, document);
            
            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Case document not found: {document.CaseId}");
            }
            
            _logger.LogInformation("Updated case document for case: {CaseId}", document.CaseId);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating case document for case: {CaseId}", document.CaseId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid caseId)
    {
        try
        {
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.CaseId, caseId);
            var result = await _collection.DeleteOneAsync(filter);
            
            if (result.DeletedCount == 0)
            {
                throw new InvalidOperationException($"Case document not found: {caseId}");
            }
            
            _logger.LogInformation("Deleted case document for case: {CaseId}", caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting case document for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<List<CaseDocument>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.UserId, userId);
            var sort = Builders<CaseDocument>.Sort.Descending(d => d.UpdatedAt);
            
            return await _collection.Find(filter).Sort(sort).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting case documents by user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<CaseDocument>> SearchAsync(CaseSearchCriteria criteria)
    {
        try
        {
            var filterBuilder = Builders<CaseDocument>.Filter;
            var filters = new List<FilterDefinition<CaseDocument>>();

            // User filter
            if (criteria.UserId.HasValue)
            {
                filters.Add(filterBuilder.Eq(d => d.UserId, criteria.UserId.Value));
            }

            // Text search
            if (!string.IsNullOrEmpty(criteria.SearchText))
            {
                filters.Add(filterBuilder.Text(criteria.SearchText));
            }

            // Date filters
            if (criteria.CreatedAfter.HasValue)
            {
                filters.Add(filterBuilder.Gte(d => d.CreatedAt, criteria.CreatedAfter.Value));
            }

            if (criteria.CreatedBefore.HasValue)
            {
                filters.Add(filterBuilder.Lte(d => d.CreatedAt, criteria.CreatedBefore.Value));
            }

            // Analysis filter
            if (criteria.HasAnalysis.HasValue)
            {
                if (criteria.HasAnalysis.Value)
                {
                    filters.Add(filterBuilder.SizeGt(d => d.Analyses, 0));
                }
                else
                {
                    filters.Add(filterBuilder.Size(d => d.Analyses, 0));
                }
            }

            // Viability score filter
            if (criteria.MinViabilityScore.HasValue)
            {
                filters.Add(filterBuilder.ElemMatch(d => d.Analyses, 
                    a => a.ViabilityScore >= criteria.MinViabilityScore.Value));
            }

            // Tags filter
            if (criteria.Tags?.Any() == true)
            {
                filters.Add(filterBuilder.AnyIn(d => d.Metadata.Tags, criteria.Tags));
            }

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

            // Sorting
            var sort = criteria.SortDescending
                ? Builders<CaseDocument>.Sort.Descending(criteria.SortBy ?? "updatedAt")
                : Builders<CaseDocument>.Sort.Ascending(criteria.SortBy ?? "updatedAt");

            return await _collection.Find(finalFilter)
                .Sort(sort)
                .Skip(criteria.Skip)
                .Limit(criteria.Take)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching case documents");
            throw;
        }
    }

    public async Task<CaseAnalysisStats> GetAnalysisStatsAsync(Guid caseId)
    {
        try
        {
            var document = await GetByIdAsync(caseId);
            if (document == null)
            {
                return new CaseAnalysisStats();
            }

            var stats = new CaseAnalysisStats
            {
                TotalAnalyses = document.Analyses.Count,
                CompletedAnalyses = document.Analyses.Count(a => a.Status == Core.Enums.AnalysisStatus.Completed),
                PendingAnalyses = document.Analyses.Count(a => a.Status == Core.Enums.AnalysisStatus.Pending || a.Status == Core.Enums.AnalysisStatus.Processing),
                FailedAnalyses = document.Analyses.Count(a => a.Status == Core.Enums.AnalysisStatus.Failed),
                LastAnalyzedAt = document.Metadata.LastAnalyzedAt
            };

            var completedAnalyses = document.Analyses.Where(a => a.Status == Core.Enums.AnalysisStatus.Completed).ToList();
            if (completedAnalyses.Any())
            {
                stats.AverageViabilityScore = completedAnalyses.Average(a => a.ViabilityScore);
                stats.AverageConfidenceScore = completedAnalyses.Average(a => a.ConfidenceScore);
                
                // Top legal issues (most frequently mentioned)
                stats.TopLegalIssues = completedAnalyses
                    .SelectMany(a => a.KeyLegalIssues)
                    .GroupBy(issue => issue)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();
                
                // Top recommendations
                stats.TopRecommendations = completedAnalyses
                    .SelectMany(a => a.Recommendations)
                    .Where(r => !string.IsNullOrEmpty(r.Action))
                    .GroupBy(r => r.Action!)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis stats for case: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid caseId)
    {
        try
        {
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.CaseId, caseId);
            return await _collection.Find(filter).AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if case document exists: {CaseId}", caseId);
            throw;
        }
    }

    public async Task<List<CaseDocument>> GetPagedAsync(int page, int pageSize)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var sort = Builders<CaseDocument>.Sort.Descending(d => d.UpdatedAt);
            
            return await _collection.Find(Builders<CaseDocument>.Filter.Empty)
                .Sort(sort)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged case documents");
            throw;
        }
    }

    public async Task<long> CountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(Builders<CaseDocument>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting case documents");
            throw;
        }
    }

    public async Task<long> CountByUserAsync(Guid userId)
    {
        try
        {
            var filter = Builders<CaseDocument>.Filter.Eq(d => d.UserId, userId);
            return await _collection.CountDocumentsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting case documents by user: {UserId}", userId);
            throw;
        }
    }
}