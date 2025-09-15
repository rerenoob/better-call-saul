using MongoDB.Driver;
using BetterCallSaul.CaseService.Data;
using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public class CaseRepository : ICaseRepository
{
    private readonly IMongoCollection<CaseDocument> _cases;

    public CaseRepository(MongoDbContext context)
    {
        _cases = context.Cases;
    }

    public async Task<CaseDocument?> GetByIdAsync(string id)
    {
        return await _cases.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CaseDocument>> GetByUserIdAsync(string userId)
    {
        return await _cases.Find(c => c.UserId == userId && !c.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<CaseDocument>> GetAllAsync()
    {
        return await _cases.Find(c => !c.IsDeleted).ToListAsync();
    }

    public async Task<CaseDocument> CreateAsync(CaseDocument caseDocument)
    {
        await _cases.InsertOneAsync(caseDocument);
        return caseDocument;
    }

    public async Task<CaseDocument> UpdateAsync(string id, CaseDocument caseDocument)
    {
        caseDocument.UpdatedAt = DateTime.UtcNow;
        var options = new FindOneAndReplaceOptions<CaseDocument> { ReturnDocument = ReturnDocument.After };
        return await _cases.FindOneAndReplaceAsync<CaseDocument>(c => c.Id == id, caseDocument, options);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<CaseDocument>.Update
            .Set(c => c.IsDeleted, true)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await _cases.UpdateOneAsync(c => c.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _cases.CountDocumentsAsync(c => c.Id == id && !c.IsDeleted) > 0;
    }
}