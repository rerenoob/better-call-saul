using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Repositories;

public interface IDocumentRepository
{
    Task<DocumentDocument?> GetByIdAsync(string id);
    Task<IEnumerable<DocumentDocument>> GetByCaseIdAsync(string caseId);
    Task<IEnumerable<DocumentDocument>> GetByUploaderAsync(string uploadedBy);
    Task<DocumentDocument> CreateAsync(DocumentDocument document);
    Task<DocumentDocument> UpdateAsync(string id, DocumentDocument document);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}