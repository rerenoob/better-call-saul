using MongoDB.Driver;
using MongoDB.Bson;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Repositories.NoSQL;

public class LegalResearchRepository : ILegalResearchRepository
{
    private readonly IMongoCollection<LegalResearchDocument> _collection;
    private readonly ILogger<LegalResearchRepository> _logger;

    public LegalResearchRepository(NoSqlContext context, ILogger<LegalResearchRepository> logger)
    {
        _collection = context.LegalResearchDocuments;
        _logger = logger;
    }

    public async Task<List<LegalResearchDocument>> SearchTextAsync(string query, int limit = 50)
    {
        try
        {
            var filter = Builders<LegalResearchDocument>.Filter.Text(query);
            var sort = Builders<LegalResearchDocument>.Sort.Descending(d => d.RelevanceScore);

            return await _collection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing text search: {Query}", query);
            throw;
        }
    }

    public async Task<List<LegalResearchDocument>> FindSimilarCasesAsync(string caseText, double threshold = 0.7)
    {
        try
        {
            // For now, use text search. In production, you'd use vector similarity or ML models
            var keywords = ExtractKeywords(caseText);
            var keywordQuery = string.Join(" ", keywords.Take(10)); // Use top 10 keywords
            
            var filter = Builders<LegalResearchDocument>.Filter.Text(keywordQuery);
            var sort = Builders<LegalResearchDocument>.Sort.Descending(d => d.RelevanceScore);

            var results = await _collection.Find(filter)
                .Sort(sort)
                .Limit(50)
                .ToListAsync();

            // Filter by threshold (in production, this would be based on actual similarity scores)
            return results.Where(r => (double)r.RelevanceScore >= threshold).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cases");
            throw;
        }
    }

    public async Task<LegalResearchDocument?> GetByCitationAsync(string citation)
    {
        try
        {
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Citation, citation);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by citation: {Citation}", citation);
            throw;
        }
    }

    public async Task<LegalResearchDocument?> GetByIdAsync(string id)
    {
        try
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Id, objectId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by ID: {Id}", id);
            throw;
        }
    }

    public async Task<LegalResearchDocument> CreateAsync(LegalResearchDocument document)
    {
        try
        {
            document.IndexedAt = DateTime.UtcNow;
            document.RetrievedAt = DateTime.UtcNow;
            document.LastUpdated = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(document);
            _logger.LogInformation("Created legal research document: {Citation}", document.Citation);
            
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating legal research document: {Citation}", document.Citation);
            throw;
        }
    }

    public async Task<LegalResearchDocument> UpdateAsync(LegalResearchDocument document)
    {
        try
        {
            document.LastUpdated = DateTime.UtcNow;
            
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Id, document.Id);
            var result = await _collection.ReplaceOneAsync(filter, document);
            
            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Legal research document not found: {document.Id}");
            }
            
            _logger.LogInformation("Updated legal research document: {Citation}", document.Citation);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating legal research document: {Citation}", document.Citation);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Id, objectId);
            var result = await _collection.DeleteOneAsync(filter);
            
            if (result.DeletedCount == 0)
            {
                throw new InvalidOperationException($"Legal research document not found: {id}");
            }
            
            _logger.LogInformation("Deleted legal research document: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting legal research document: {Id}", id);
            throw;
        }
    }

    public async Task BulkIndexAsync(List<LegalResearchDocument> documents)
    {
        try
        {
            if (!documents.Any()) return;
            
            foreach (var doc in documents)
            {
                doc.IndexedAt = DateTime.UtcNow;
                doc.LastUpdated = DateTime.UtcNow;
            }
            
            await _collection.InsertManyAsync(documents);
            _logger.LogInformation("Bulk indexed {Count} legal research documents", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing legal research documents");
            throw;
        }
    }

    public async Task<List<LegalResearchDocument>> GetByJurisdictionAsync(string jurisdiction, int limit = 100)
    {
        try
        {
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Jurisdiction, jurisdiction);
            var sort = Builders<LegalResearchDocument>.Sort.Descending(d => d.DecisionDate);
            
            return await _collection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by jurisdiction: {Jurisdiction}", jurisdiction);
            throw;
        }
    }

    public async Task<List<LegalResearchDocument>> GetByCourtAsync(string court, int limit = 100)
    {
        try
        {
            var filter = Builders<LegalResearchDocument>.Filter.Eq(d => d.Court, court);
            var sort = Builders<LegalResearchDocument>.Sort.Descending(d => d.DecisionDate);
            
            return await _collection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by court: {Court}", court);
            throw;
        }
    }

    public async Task<List<LegalResearchDocument>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 100)
    {
        try
        {
            var filter = Builders<LegalResearchDocument>.Filter.And(
                Builders<LegalResearchDocument>.Filter.Gte(d => d.DecisionDate, startDate),
                Builders<LegalResearchDocument>.Filter.Lte(d => d.DecisionDate, endDate)
            );
            var sort = Builders<LegalResearchDocument>.Sort.Descending(d => d.DecisionDate);
            
            return await _collection.Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by date range: {StartDate} - {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<List<LegalResearchDocument>> SearchAdvancedAsync(LegalSearchQuery query)
    {
        try
        {
            var filterBuilder = Builders<LegalResearchDocument>.Filter;
            var filters = new List<FilterDefinition<LegalResearchDocument>>();

            // Full text search
            if (!string.IsNullOrEmpty(query.FullTextQuery))
            {
                filters.Add(filterBuilder.Text(query.FullTextQuery));
            }

            // Jurisdiction filter
            if (!string.IsNullOrEmpty(query.Jurisdiction))
            {
                filters.Add(filterBuilder.Eq(d => d.Jurisdiction, query.Jurisdiction));
            }

            // Court filter
            if (!string.IsNullOrEmpty(query.Court))
            {
                filters.Add(filterBuilder.Eq(d => d.Court, query.Court));
            }

            // Date range
            if (query.StartDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(d => d.DecisionDate, query.StartDate.Value));
            }
            if (query.EndDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(d => d.DecisionDate, query.EndDate.Value));
            }

            // Document type
            if (query.DocumentType.HasValue)
            {
                filters.Add(filterBuilder.Eq(d => d.Type, query.DocumentType.Value));
            }

            // Relevance score
            if (query.MinRelevanceScore.HasValue)
            {
                filters.Add(filterBuilder.Gte(d => d.RelevanceScore, (decimal)query.MinRelevanceScore.Value));
            }

            // Keywords
            if (query.Keywords?.Any() == true)
            {
                filters.Add(filterBuilder.AnyIn(d => d.Metadata.Keywords, query.Keywords));
            }

            // Topics
            if (query.Topics?.Any() == true)
            {
                filters.Add(filterBuilder.AnyIn(d => d.Metadata.Topics, query.Topics));
            }

            // Practice areas
            if (query.PracticeAreas?.Any() == true)
            {
                filters.Add(filterBuilder.AnyIn(d => d.Metadata.PracticeAreas, query.PracticeAreas));
            }

            // Precedential value
            if (query.PrecedentialValue.HasValue)
            {
                filters.Add(filterBuilder.Eq(d => d.Metadata.PrecedentialValue, query.PrecedentialValue.Value));
            }

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

            // Sorting
            var sort = query.SortDescending
                ? Builders<LegalResearchDocument>.Sort.Descending(query.SortBy ?? "relevanceScore")
                : Builders<LegalResearchDocument>.Sort.Ascending(query.SortBy ?? "relevanceScore");

            return await _collection.Find(finalFilter)
                .Sort(sort)
                .Skip(query.Skip)
                .Limit(query.Take)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced search");
            throw;
        }
    }

    public async Task<long> CountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(Builders<LegalResearchDocument>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting legal research documents");
            throw;
        }
    }

    public async Task<Dictionary<string, long>> GetStatsAsync()
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$type" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            var stats = new Dictionary<string, long>();
            foreach (var result in results)
            {
                var type = result["_id"].ToString() ?? "Unknown";
                var count = result["count"].ToInt64();
                stats[type] = count;
            }

            // Add total count
            stats["Total"] = await CountAsync();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting legal research statistics");
            throw;
        }
    }

    private List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction - in production, use NLP libraries
        var words = text.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        return words
            .Where(w => w.Length > 3) // Filter short words
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .OrderBy(w => w)
            .ToList();
    }
}