using better_call_saul.Models;

namespace better_call_saul.Services;

public interface ITextExtractionService
{
    Task<DocumentContent> ExtractTextAsync(string filePath, string fileName);
    bool IsSupported(string fileName);
}