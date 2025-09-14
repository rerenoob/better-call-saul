# Task: Implement AWS S3 File Upload and Download Operations

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.2 AWS S3 Storage Service Implementation
- **Complexity**: Medium
- **Estimated Time**: 7 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 02-aws-implementation/004-aws-s3-setup: S3 infrastructure setup completed

### External Dependencies
- S3 bucket with proper read/write permissions
- Understanding of S3 multipart upload for large files

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSS3StorageService.cs`: Implement file operations
- `BetterCallSaul.Infrastructure/Helpers/S3UploadHelper.cs`: Multipart upload utility
- `BetterCallSaul.Infrastructure/Mappers/S3ResponseMapper.cs`: Response normalization
- `BetterCallSaul.Tests/Services/FileProcessing/S3FileOperationsTests.cs`: File operation tests

### Code Patterns
- Follow existing Azure Blob Storage upload patterns
- Use S3 SDK async patterns with progress tracking
- Implement proper stream handling and disposal

## Acceptance Criteria
- [ ] UploadFileAsync successfully uploads files to S3 with metadata
- [ ] DeleteFileAsync removes files from S3 bucket
- [ ] Multipart upload support for files larger than 5MB
- [ ] File upload progress tracking and cancellation support
- [ ] Response normalization converts S3 responses to standard StorageResult format
- [ ] Error handling covers S3-specific exceptions (access denied, bucket not found, etc.)

## Testing Strategy
- Unit tests: Upload/download logic, progress tracking, error handling
- Integration tests: Real S3 operations with various file sizes
- Manual validation: File operations work identically to Azure Blob Storage

## System Stability
- Proper cleanup of partial uploads on failure or cancellation
- Resource management for streams and S3 clients
- Retry logic for transient S3 errors

### S3 Upload Implementation
```csharp
public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
{
    var result = new StorageResult { UploadSessionId = uploadSessionId };

    try
    {
        var key = await GenerateUniqueFileNameAsync(file.FileName);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            Metadata = {
                ["OriginalFileName"] = file.FileName,
                ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                ["CaseId"] = caseId.ToString(),
                ["UserId"] = userId.ToString()
            }
        };

        var response = await _s3Client.PutObjectAsync(request);

        result.Success = true;
        result.StoragePath = $"s3://{_options.BucketName}/{key}";
        result.FileSize = file.Length;

        return result;
    }
    catch (AmazonS3Exception ex)
    {
        // Handle S3-specific errors
        result.Success = false;
        result.Message = $"S3 error: {ex.Message}";
        result.ErrorCode = ex.ErrorCode;
        return result;
    }
}
```