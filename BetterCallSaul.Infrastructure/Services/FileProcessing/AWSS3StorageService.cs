using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.ServiceResponses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class AWSS3StorageService : IStorageService
{
    private readonly IAmazonS3? _s3Client;
    private readonly S3Options _options;
    private readonly ILogger<AWSS3StorageService> _logger;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB limit

    public AWSS3StorageService(
        IOptions<CloudProviderOptions> cloudProviderOptions,
        ILogger<AWSS3StorageService> logger)
    {
        _options = cloudProviderOptions.Value.AWS.S3;
        _logger = logger;
        
        // Only initialize S3 client if AWS is configured
        if (!string.IsNullOrEmpty(_options.BucketName) && !string.IsNullOrEmpty(_options.Region))
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
            _s3Client = new AmazonS3Client(region);
        }
    }

    public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        var result = new StorageResult { UploadSessionId = uploadSessionId };

        try
        {
            // Check if S3 client is initialized
            if (_s3Client == null)
            {
                result.Success = false;
                result.Message = "AWS S3 is not configured";
                result.ErrorCode = "AWS_NOT_CONFIGURED";
                return result;
            }

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
            var keyName = GenerateS3KeyName(caseId, userId, uniqueFileName);

            // Upload to S3
            var uploadRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = keyName,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                Metadata =
                {
                    ["UploadSessionId"] = uploadSessionId,
                    ["CaseId"] = caseId.ToString(),
                    ["UserId"] = userId.ToString(),
                    ["OriginalFileName"] = file.FileName,
                    ["UploadedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            var response = await _s3Client.PutObjectAsync(uploadRequest);

            result.Success = true;
            result.FileName = uniqueFileName;
            result.FileSize = file.Length;
            result.FileType = Path.GetExtension(file.FileName).ToLowerInvariant();
            result.Message = "File uploaded successfully to AWS S3";
            result.StoragePath = keyName;

            _logger.LogInformation("File uploaded to AWS S3: {FileName} (Key: {KeyName}, Size: {FileSize} bytes)", 
                file.FileName, keyName, file.Length);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "S3 object already exists during upload: {FileName}", file.FileName);
            result.Success = false;
            result.Message = "File already exists";
            result.ErrorCode = "S3_CONFLICT";
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error uploading file: {FileName}", file.FileName);
            result.Success = false;
            result.Message = $"AWS S3 error: {ex.Message}";
            result.ErrorCode = $"S3_ERROR_{(int)ex.StatusCode}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to AWS S3: {FileName}", file.FileName);
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
            if (_s3Client == null)
            {
                _logger.LogWarning("AWS S3 is not configured, cannot delete file: {StoragePath}", storagePath);
                return false;
            }

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storagePath
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest);
            
            _logger.LogInformation("File deleted from AWS S3: {StoragePath}", storagePath);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error deleting file: {StoragePath}", storagePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from AWS S3: {StoragePath}", storagePath);
            return false;
        }
    }

    public async Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
    {
        try
        {
            if (_s3Client == null)
            {
                throw new InvalidOperationException("AWS S3 is not configured");
            }

            // Check if object exists
            var headRequest = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = storagePath
            };

            await _s3Client.GetObjectMetadataAsync(headRequest);

            // Generate presigned URL
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = storagePath,
                Expires = DateTime.UtcNow.Add(expiryTime),
                Verb = HttpVerb.GET
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            
            _logger.LogInformation("Generated presigned URL for: {StoragePath}, expires in {ExpiryTime}", 
                storagePath, expiryTime);
            
            return presignedUrl;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("S3 object not found when generating URL: {StoragePath}", storagePath);
            throw new FileNotFoundException("S3 object not found", storagePath);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating presigned URL for: {StoragePath}", storagePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for: {StoragePath}", storagePath);
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

    private string GenerateS3KeyName(Guid caseId, Guid userId, string fileName)
    {
        // S3 key format: cases/{caseId}/documents/{userId}/{fileName}
        return $"cases/{caseId}/documents/{userId}/{fileName}";
    }

    // Additional method for generating upload presigned URLs
    public async Task<string> GenerateUploadPresignedUrlAsync(string fileName, Guid caseId, Guid userId, TimeSpan expiryTime)
    {
        try
        {
            if (_s3Client == null)
            {
                throw new InvalidOperationException("AWS S3 is not configured");
            }

            var keyName = GenerateS3KeyName(caseId, userId, fileName);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = keyName,
                Expires = DateTime.UtcNow.Add(expiryTime),
                Verb = HttpVerb.PUT,
                ContentType = GetContentTypeFromExtension(fileName)
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            
            _logger.LogInformation("Generated upload presigned URL for: {FileName}, expires in {ExpiryTime}", 
                fileName, expiryTime);
            
            return presignedUrl;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating upload presigned URL for: {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload presigned URL for: {FileName}", fileName);
            throw;
        }
    }

    private string GetContentTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".rtf" => "application/rtf",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}