using MongoDB.Driver;
using BetterCallSaul.CaseService.Data;
using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public class CaseAnalysisRepository : ICaseAnalysisRepository
{
    private readonly IMongoCollection<CaseAnalysisDocument> _analyses;

    public CaseAnalysisRepository(MongoDbContext context)
    {
        _analyses = context.CaseAnalyses;
    }

    public async Task<CaseAnalysisDocument?> GetByIdAsync(string id)
    {
        return await _analyses.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CaseAnalysisDocument>> GetByCaseIdAsync(string caseId)
    {
        return await _analyses.Find(a => a.CaseId == caseId).ToListAsync();
    }

    public async Task<IEnumerable<CaseAnalysisDocument>> GetByDocumentIdAsync(string documentId)
    {
        return await _analyses.Find(a => a.DocumentId == documentId).ToListAsync();
    }

    public async Task<CaseAnalysisDocument> CreateAsync(CaseAnalysisDocument analysis)
    {
        await _analyses.InsertOneAsync(analysis);
        return analysis;
    }

    public async Task<CaseAnalysisDocument> UpdateAsync(string id, CaseAnalysisDocument analysis)
    {
        var options = new FindOneAndReplaceOptions<CaseAnalysisDocument> { ReturnDocument = ReturnDocument.After };
        return await _analyses.FindOneAndReplaceAsync<CaseAnalysisDocument>(a => a.Id == id, analysis, options);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _analyses.DeleteOneAsync(a => a.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _analyses.CountDocumentsAsync(a => a.Id == id) > 0;
    }
}