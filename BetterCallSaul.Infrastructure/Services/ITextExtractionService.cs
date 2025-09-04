using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Infrastructure.Services;

public interface ITextExtractionService
{
    Task<TextExtractionResult> ExtractTextAsync(string filePath, string fileName);
    Task<TextExtractionResult> ExtractTextFromBytesAsync(byte[] fileContent, string fileName);
    Task<bool> SupportsFileTypeAsync(string fileName);
    Task<DocumentText> ProcessDocumentAsync(string filePath, Guid documentId);
}