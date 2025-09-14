# Task: Complete AWS S3 Storage Service Implementation

## Overview
- **Parent Feature**: AWS Migration - File Storage
- **Complexity**: Medium
- **Estimated Time**: 12 hours
- **Status**: Not Started

**Note**: This combines S3 setup, file operations, and presigned URL tasks.

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/002-storage-service-interface: IStorageService complete
- [ ] 01-foundation-abstractions/003-configuration-and-di: AWS configuration ready

### External Dependencies
- AWS S3 bucket created and configured
- IAM permissions for S3 operations

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSS3StorageService.cs`: Complete S3 implementation
- Add AWS S3 SDK packages
- Update existing Azure storage services to implement `IStorageService`

### AWS Services Used
- **Amazon S3**: Object storage for documents
- **S3 Presigned URLs**: Secure temporary access to files
- **S3 Transfer Utility**: Efficient uploads/downloads

## Acceptance Criteria
- [ ] `AWSS3StorageService` implements all `IStorageService` methods
- [ ] File upload/download works with existing frontend
- [ ] Presigned URLs provide secure temporary access
- [ ] File validation and size limits enforced
- [ ] Proper error handling for S3 operations
- [ ] Performance comparable to Azure Blob Storage

## Key Implementation Points
### File Upload Process
```csharp
public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
{
    var keyName = GenerateS3KeyName(caseId, userId, file.FileName);

    var request = new PutObjectRequest
    {
        BucketName = _options.BucketName,
        Key = keyName,
        InputStream = file.OpenReadStream(),
        ContentType = file.ContentType,
        Metadata = { ["UploadSession"] = uploadSessionId }
    };

    var response = await _s3Client.PutObjectAsync(request);

    return new StorageResult
    {
        Success = true,
        StoragePath = keyName,
        FileSize = file.Length
    };
}
```

### Presigned URL Generation
- Generate secure URLs with configurable expiration (default: 1 hour)
- Support both download and upload presigned URLs
- Maintain compatibility with existing frontend expectations

## Testing Strategy
- Unit tests: S3 client mocking, URL generation
- Integration tests: Real S3 operations with test bucket
- File validation: Upload various document types
- Manual validation: Frontend file upload/download workflows

## System Stability
- Fallback to Azure storage if S3 unavailable
- Proper cleanup of failed uploads
- S3-specific error handling and retry logic