using MongoDB.Driver;
using BetterCallSaul.CaseService.Data;
using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly IMongoCollection<DocumentDocument> _documents;

    public DocumentRepository(MongoDbContext context)
    {
        _documents = context.Documents;
    }

    public async Task<DocumentDocument?> GetByIdAsync(string id)
    {
        return await _documents.Find(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentDocument>> GetByCaseIdAsync(string caseId)
    {
        return await _documents.Find(d => d.CaseId == caseId && !d.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<DocumentDocument>> GetByUploaderAsync(string uploadedBy)
    {
        return await _documents.Find(d => d.UploadedBy == uploadedBy && !d.IsDeleted).ToListAsync();
    }

    public async Task<DocumentDocument> CreateAsync(DocumentDocument document)
    {
        await _documents.InsertOneAsync(document);
        return document;
    }

    public async Task<DocumentDocument> UpdateAsync(string id, DocumentDocument document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        var options = new FindOneAndReplaceOptions<DocumentDocument> { ReturnDocument = ReturnDocument.After };
        return await _documents.FindOneAndReplaceAsync<DocumentDocument>(d => d.Id == id, document, options);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<DocumentDocument>.Update
            .Set(d => d.IsDeleted, true)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        var result = await _documents.UpdateOneAsync(d => d.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _documents.CountDocumentsAsync(d => d.Id == id && !d.IsDeleted) > 0;
    }
}