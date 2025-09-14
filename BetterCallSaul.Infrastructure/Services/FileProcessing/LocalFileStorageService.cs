using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.ServiceResponses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BetterCallSaul.Core.Configuration;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class LocalFileStorageService : IStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _baseStoragePath;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB limit

    public LocalFileStorageService(
        IOptions<LocalStorageOptions> localStorageOptions,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        
        // Use configured base path or default to "uploads" directory
        var localStorageConfig = localStorageOptions.Value;
        _baseStoragePath = !string.IsNullOrEmpty(localStorageConfig?.BasePath) 
            ? localStorageConfig.BasePath 
            : Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        // Ensure base directory exists
        Directory.CreateDirectory(_baseStoragePath);
        
        _logger.LogInformation("LocalFileStorageService initialized with base path: {BasePath}", _baseStoragePath);
    }

    public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var result = new StorageResult { UploadSessionId = uploadSessionId };

        try
        {
            // Validate file
            if (!await ValidateFileAsync(file))
            {
                result.Success = false;
                result.Message = "File validation failed";
                result.ErrorCode = "VALIDATION_FAILED";
                return result;
            }

            // Generate unique filename
            var uniqueFileName = await GenerateUniqueFileNameAsync(file.FileName);
            var storagePath = GenerateLocalFilePath(caseId, userId, uniqueFileName);

            // Create directory structure if needed
            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file to local filesystem
            await using (var fileStream = new FileStream(storagePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            result.Success = true;
            result.FileName = uniqueFileName;
            result.FileSize = file.Length;
            result.FileType = Path.GetExtension(file.FileName).ToLowerInvariant();
            result.Message = "File uploaded successfully to local storage";
            result.StoragePath = storagePath;

            // Add metadata for tracking
            result.Metadata = new Dictionary<string, string>
            {
                ["CaseId"] = caseId.ToString(),
                ["UserId"] = userId.ToString(),
                ["OriginalFileName"] = file.FileName,
                ["UploadedAt"] = DateTime.UtcNow.ToString("O")
            };

            _logger.LogInformation("File uploaded to local storage: {FileName} (Path: {StoragePath}, Size: {FileSize} bytes)", 
                file.FileName, storagePath, file.Length);
        }
        catch (IOException ex) when (ex.Message.Contains("disk space", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Insufficient disk space when uploading file: {FileName}", file.FileName);
            result.Success = false;
            result.Message = "Insufficient disk space";
            result.ErrorCode = "DISK_SPACE_ERROR";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied when uploading file: {FileName}", file.FileName);
            result.Success = false;
            result.Message = "Permission denied for file operations";
            result.ErrorCode = "PERMISSION_DENIED";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to local storage: {FileName}", file.FileName);
            result.Success = false;
            result.Message = $"Error uploading file: {ex.Message}";
            result.ErrorCode = "UPLOAD_ERROR";
        }

        return result;
    }

    public async Task<bool> DeleteFileAsync(string storagePath)
    {
        try
        {
            // Security check: ensure the file is within the base storage path
            if (!IsPathWithinBaseDirectory(storagePath, _baseStoragePath))
            {
                _logger.LogWarning("Attempted to delete file outside base storage directory: {StoragePath}", storagePath);
                return false;
            }

            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
                
                // Clean up empty directories
                await CleanupEmptyDirectoriesAsync(Path.GetDirectoryName(storagePath));
                
                _logger.LogInformation("File deleted from local storage: {StoragePath}", storagePath);
                return true;
            }
            
            _logger.LogWarning("File not found for deletion: {StoragePath}", storagePath);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied when deleting file: {StoragePath}", storagePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from local storage: {StoragePath}", storagePath);
            return false;
        }
    }

    public async Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
    {
        try
        {
            // Security check: ensure the file is within the base storage path
            if (!IsPathWithinBaseDirectory(storagePath, _baseStoragePath))
            {
                _logger.LogWarning("Attempted to generate URL for file outside base storage directory: {StoragePath}", storagePath);
                throw new FileNotFoundException("File not found", storagePath);
            }

            // Check if file exists
            if (!File.Exists(storagePath))
            {
                _logger.LogWarning("File not found when generating URL: {StoragePath}", storagePath);
                throw new FileNotFoundException("File not found", storagePath);
            }

            // For local development, return the file path directly
            // In a real scenario, this would generate a secure access URL
            _logger.LogInformation("Generated local file access URL for: {StoragePath}", storagePath);
            
            return storagePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure URL for: {StoragePath}", storagePath);
            throw;
        }
    }

    public async Task<bool> ValidateFileAsync(IFormFile file)
    {
        // Basic validation - file size, extension, etc.
        if (file == null || file.Length == 0)
            return await Task.FromResult(false);

        if (file.Length > MaxFileSize)
            return await Task.FromResult(false);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt", 
                                       ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
        
        return await Task.FromResult(allowedExtensions.Contains(extension));
    }

    public async Task<string> GenerateUniqueFileNameAsync(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        return await Task.FromResult($"{baseName}_{timestamp}_{random}{extension}");
    }

    private string GenerateLocalFilePath(Guid caseId, Guid userId, string fileName)
    {
        // Local file path format: {basePath}/cases/{caseId}/documents/{userId}/{fileName}
        return Path.Combine(_baseStoragePath, "cases", caseId.ToString(), "documents", userId.ToString(), fileName);
    }

    private bool IsPathWithinBaseDirectory(string filePath, string baseDirectory)
    {
        var fullFilePath = Path.GetFullPath(filePath);
        var fullBaseDirectory = Path.GetFullPath(baseDirectory);
        
        return fullFilePath.StartsWith(fullBaseDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private async Task CleanupEmptyDirectoriesAsync(string directoryPath)
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return;

            // Don't cleanup the base directory or case directories
            if (Path.GetFullPath(directoryPath) == Path.GetFullPath(_baseStoragePath))
                return;

            // Don't cleanup case directories (they should remain even if empty)
            var relativePath = Path.GetRelativePath(_baseStoragePath, directoryPath);
            if (relativePath.StartsWith("cases") && relativePath.Split(Path.DirectorySeparatorChar).Length <= 2)
                return;

            var directory = new DirectoryInfo(directoryPath);
            
            // Check if directory is empty
            if (!directory.EnumerateFiles().Any() && !directory.EnumerateDirectories().Any())
            {
                Directory.Delete(directoryPath);
                _logger.LogInformation("Cleaned up empty directory: {DirectoryPath}", directoryPath);
                
                // Recursively cleanup parent directories
                var parentDirectory = Path.GetDirectoryName(directoryPath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    await CleanupEmptyDirectoriesAsync(parentDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up empty directories for: {DirectoryPath}", directoryPath);
        }
    }

    // Additional helper method for development: get all files for a case
    public async Task<IEnumerable<string>> GetFilesForCaseAsync(Guid caseId)
    {
        var caseDirectory = Path.Combine(_baseStoragePath, "cases", caseId.ToString(), "documents");
        
        if (!Directory.Exists(caseDirectory))
            return Enumerable.Empty<string>();

        return await Task.FromResult(Directory.GetFiles(caseDirectory, "*.*", SearchOption.AllDirectories));
    }

    // Additional helper method for development: get storage statistics
    public async Task<StorageStatistics> GetStorageStatisticsAsync()
    {
        var stats = new StorageStatistics();
        
        if (!Directory.Exists(_baseStoragePath))
            return stats;

        var allFiles = Directory.GetFiles(_baseStoragePath, "*.*", SearchOption.AllDirectories);
        
        stats.TotalFiles = allFiles.Length;
        stats.TotalSize = allFiles.Sum(file => new FileInfo(file).Length);
        stats.LastCleaned = DateTime.UtcNow;
        
        return await Task.FromResult(stats);
    }

    public class StorageStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public DateTime LastCleaned { get; set; }
    }
}