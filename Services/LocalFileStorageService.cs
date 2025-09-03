using Microsoft.Extensions.Options;

namespace better_call_saul.Services;

public class FileStorageOptions
{
    public string BasePath { get; set; } = "wwwroot/uploads";
    public long MaxFileSize { get; set; } = 10_000_000; // 10MB
    public string[] AllowedExtensions { get; set; } = { ".pdf", ".docx", ".doc", ".txt", ".jpg", ".jpeg", ".png" };
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILoggerService _logger;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, ILoggerService logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string directory)
    {
        ValidateFile(file);
        
        var sanitizedDirectory = SanitizePath(directory);
        var fileName = SanitizeFileName(file.FileName);
        var filePath = Path.Combine(_options.BasePath, sanitizedDirectory, fileName);
        
        EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation($"File saved successfully: {filePath}");
        return filePath;
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        var sanitizedPath = SanitizePath(filePath);
        
        if (!FileExists(sanitizedPath))
        {
            _logger.LogWarning($"File not found for deletion: {sanitizedPath}");
            return Task.FromResult(false);
        }

        File.Delete(sanitizedPath);
        _logger.LogInformation($"File deleted: {sanitizedPath}");
        return Task.FromResult(true);
    }

    public Task<Stream> GetFileStreamAsync(string filePath)
    {
        var sanitizedPath = SanitizePath(filePath);
        
        if (!FileExists(sanitizedPath))
        {
            _logger.LogWarning($"File not found: {sanitizedPath}");
            throw new FileNotFoundException("File not found", sanitizedPath);
        }

        return Task.FromResult<Stream>(new FileStream(sanitizedPath, FileMode.Open, FileAccess.Read));
    }

    public bool FileExists(string filePath)
    {
        var sanitizedPath = SanitizePath(filePath);
        return File.Exists(sanitizedPath);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        if (file.Length > _options.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_options.MaxFileSize} bytes");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}");
        }
    }

    private string SanitizePath(string path)
    {
        // Prevent directory traversal attacks
        var basePath = Path.GetFullPath(_options.BasePath);
        var fullPath = Path.GetFullPath(Path.Combine(basePath, path));

        // Ensure the path stays within the base directory
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid file path - attempted directory traversal");
        }

        return fullPath;
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        return string.IsNullOrEmpty(sanitized) ? Guid.NewGuid().ToString() : sanitized;
    }

    private void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            _logger.LogInformation($"Created directory: {directoryPath}");
        }
    }
}