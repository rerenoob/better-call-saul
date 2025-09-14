# Task: Implement Google Cloud Storage File Operations

## Overview
- **Parent Feature**: Phase 3 Google Cloud Implementation - Task 3.2 Google Cloud Storage Service Implementation
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 03-google-cloud-implementation/003-google-cloud-storage-setup: GCS infrastructure setup completed

### External Dependencies
- GCS bucket with proper read/write permissions
- Understanding of GCS resumable upload for large files

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/GoogleCloudStorageService.cs`: Implement file operations
- `BetterCallSaul.Infrastructure/Helpers/GCSUploadHelper.cs`: Resumable upload utility
- `BetterCallSaul.Infrastructure/Mappers/GCSResponseMapper.cs`: Response normalization
- `BetterCallSaul.Tests/Services/FileProcessing/GCSFileOperationsTests.cs`: File operation tests

### Code Patterns
- Follow existing storage service patterns from Azure and AWS implementations
- Use GCS SDK async patterns with proper error handling
- Implement resumable uploads for reliability

## Acceptance Criteria
- [ ] UploadFileAsync successfully uploads files to GCS bucket with metadata
- [ ] DeleteFileAsync removes files from GCS bucket
- [ ] Resumable upload support for large files and network interruptions
- [ ] File upload progress tracking and cancellation support
- [ ] Response normalization converts GCS responses to standard StorageResult format
- [ ] Error handling covers GCS-specific exceptions

## Testing Strategy
- Unit tests: Upload/download logic, resumable uploads, error handling
- Integration tests: Real GCS operations with various file sizes
- Manual validation: File operations work identically across all storage providers

## System Stability
- Proper cleanup of incomplete resumable uploads
- Resource management for streams and GCS clients
- Retry logic for transient GCS errors

### GCS Upload Implementation
```csharp
public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
{
    var result = new StorageResult { UploadSessionId = uploadSessionId };

    try
    {
        var objectName = await GenerateUniqueFileNameAsync(file.FileName);
        var gcsObject = new Google.Cloud.Storage.V1.Object
        {
            Bucket = _options.BucketName,
            Name = objectName,
            ContentType = file.ContentType,
            Metadata = {
                ["OriginalFileName"] = file.FileName,
                ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                ["CaseId"] = caseId.ToString(),
                ["UserId"] = userId.ToString()
            }
        };

        using var stream = file.OpenReadStream();
        var uploadedObject = await _storageClient.UploadObjectAsync(gcsObject, stream);

        result.Success = true;
        result.StoragePath = $"gs://{_options.BucketName}/{objectName}";
        result.FileSize = file.Length;

        return result;
    }
    catch (GoogleApiException ex)
    {
        result.Success = false;
        result.Message = $"GCS error: {ex.Message}";
        result.ErrorCode = ex.Error?.Code?.ToString() ?? "UNKNOWN";
        return result;
    }
}
```