using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public interface ITextExtractionService
{
    Task<TextExtractionResult> ExtractTextAsync(string filePath, string fileName);
    Task<TextExtractionResult> ExtractTextFromBytesAsync(byte[] fileContent, string fileName);
    Task<bool> SupportsFileTypeAsync(string fileName);
}