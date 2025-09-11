using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class AzureBlobStorageService : IFileUploadService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private BlobContainerClient? _containerClient;
    private const long MaxUserUploadSizePerHour = 500 * 1024 * 1024; // 500MB per hour

    public AzureBlobStorageService(
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Only initialize Azure clients if connection string is provided
        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        }
    }

    public async Task<UploadResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var result = new UploadResult { UploadSessionId = uploadSessionId };

        try
        {
            // Check if Azure client is initialized
            if (_blobServiceClient == null)
            {
                result.Success = false;
                result.Message = "Azure Blob Storage is not configured";
                result.ErrorCode = "AZURE_NOT_CONFIGURED";
                return result;
            }

            // Ensure container exists
            await EnsureContainerExistsAsync();

            // Generate unique filename
            var uniqueFileName = await GenerateUniqueFileNameAsync(file.FileName);
            
            // Upload to Azure Blob Storage
            var blobPath = await StoreFileAsync(file, uniqueFileName);

            result.Success = true;
            result.FileName = uniqueFileName;
            result.FileSize = file.Length;
            result.FileType = Path.GetExtension(file.FileName).ToLowerInvariant();
            result.Message = "File uploaded successfully to Azure Blob Storage";
            result.StoragePath = blobPath;

            _logger.LogInformation("File uploaded to Azure Blob Storage: {FileName} (Path: {BlobPath})", 
                file.FileName, blobPath);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Blob already exists during upload: {FileName}", file.FileName);
            result.Success = false;
            result.Message = "File already exists";
            result.ErrorCode = "BLOB_CONFLICT";
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage error uploading file: {FileName}", file.FileName);
            result.Success = false;
            result.Message = $"Azure storage error: {ex.Message}";
            result.ErrorCode = $"AZURE_ERROR_{ex.Status}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure Blob Storage: {FileName}", file.FileName);
            result.Success = false;
            result.Message = $"Error uploading file: {ex.Message}";
            result.ErrorCode = "UPLOAD_ERROR";
        }

        return result;
    }

    public Task<bool> ValidateFileAsync(IFormFile file)
    {
        // Basic validation - file size, extension, etc.
        // More comprehensive validation should be done by FileValidationService
        if (file == null || file.Length == 0)
            return Task.FromResult(false);

        if (file.Length > 100 * 1024 * 1024) // 100MB limit
            return Task.FromResult(false);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt", ".jpg", ".jpeg", ".png", ".gif" };
        
        return Task.FromResult(allowedExtensions.Contains(extension));
    }

    public Task<string> GenerateUniqueFileNameAsync(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        return Task.FromResult($"{baseName}_{timestamp}_{random}{extension}");
    }

    public async Task<string> StoreFileAsync(IFormFile file, string fileName)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        
        // Set metadata
        var metadata = new Dictionary<string, string>
        {
            ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
            ["OriginalFileName"] = file.FileName,
            ["ContentType"] = file.ContentType
        };

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType
        };

        // Upload with retry policy
        var retryPolicy = new RetryPolicy(_options.MaxRetries, _options.RetryDelayMilliseconds, _logger);
        
        await retryPolicy.ExecuteAsync(async () =>
        {
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    Metadata = metadata,
                    HttpHeaders = blobHttpHeaders
                });
            }
        });

        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteFileAsync(string blobUrl)
    {
        try
        {
            if (_containerClient == null)
            {
                return false;
            }

            var blobUri = new Uri(blobUrl);
            var blobName = blobUri.Segments.Last();
            var blobClient = _containerClient.GetBlobClient(blobName);

            var retryPolicy = new RetryPolicy(_options.MaxRetries, _options.RetryDelayMilliseconds, _logger);
            
            return await retryPolicy.ExecuteAsync(async () =>
            {
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob: {BlobUrl}", blobUrl);
            return false;
        }
    }

    public Task<long> GetTotalUploadSizeForUserAsync(Guid userId, TimeSpan timeWindow)
    {
        // This method would typically query a database for user upload statistics
        // For Azure Blob Storage, we'd need to implement blob metadata tracking
        // or use Azure Storage Analytics for this information
        
        // For now, return 0 as this is handled by the database in the main flow
        return Task.FromResult(0L);
    }

    public async Task<string> GenerateSasTokenAsync(string blobName, TimeSpan expiryTime)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        
        // Check if blob exists
        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException("Blob not found", blobName);
        }

        // Create SAS token
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiryTime)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);
        return sasToken.ToString();
    }

    private async Task EnsureContainerExistsAsync()
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        try
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to create blob container: {ContainerName}", _options.ContainerName);
            throw;
        }
    }

    // Simple retry policy implementation
    private class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _delayMs;
        private readonly ILogger _logger;

        public RetryPolicy(int maxRetries, int delayMs, ILogger logger)
        {
            _maxRetries = maxRetries;
            _delayMs = delayMs;
            _logger = logger;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (RequestFailedException ex) when (IsTransientError(ex) && attempt < _maxRetries)
                {
                    _logger.LogWarning(ex, "Transient error on attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms", 
                        attempt, _maxRetries, _delayMs);
                    await Task.Delay(_delayMs);
                }
            }

            // If we get here, all retries failed
            throw new InvalidOperationException($"Operation failed after {_maxRetries} attempts");
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (RequestFailedException ex) when (IsTransientError(ex) && attempt < _maxRetries)
                {
                    _logger.LogWarning(ex, "Transient error on attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms", 
                        attempt, _maxRetries, _delayMs);
                    await Task.Delay(_delayMs);
                }
            }

            throw new InvalidOperationException($"Operation failed after {_maxRetries} attempts");
        }

        private bool IsTransientError(RequestFailedException ex)
        {
            return ex.Status == 429 || // Too Many Requests
                   ex.Status == 503 || // Service Unavailable
                   ex.Status == 500 || // Internal Server Error
                   (ex.Status >= 300 && ex.Status < 500 && ex.Status != 404); // Other transient errors
        }
    }
}