namespace better_call_saul.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string directory);
    Task<bool> DeleteFileAsync(string filePath);
    Task<Stream> GetFileStreamAsync(string filePath);
    bool FileExists(string filePath);
}