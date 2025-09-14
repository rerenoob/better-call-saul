# Task: Implement AWS S3 Presigned URL Generation

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.2 AWS S3 Storage Service Implementation
- **Complexity**: Low
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 02-aws-implementation/005-s3-file-operations: S3 file operations completed

### External Dependencies
- S3 bucket with proper access policies for presigned URLs

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSS3StorageService.cs`: Implement GenerateSecureUrlAsync
- `BetterCallSaul.Infrastructure/Helpers/S3UrlHelper.cs`: URL generation utilities
- `BetterCallSaul.Tests/Services/FileProcessing/S3PresignedUrlTests.cs`: URL generation tests

### Code Patterns
- Follow existing Azure SAS token generation patterns
- Use S3 SDK GetPreSignedURL functionality
- Implement configurable expiry times

## Acceptance Criteria
- [ ] GenerateSecureUrlAsync creates valid presigned URLs for S3 objects
- [ ] Configurable expiry time matching Azure SAS token functionality
- [ ] URL validation and error handling for non-existent objects
- [ ] Support for both read and read-write presigned URLs
- [ ] Response format compatible with existing frontend expectations
- [ ] Performance comparable to Azure SAS token generation

## Testing Strategy
- Unit tests: URL generation logic, expiry time configuration
- Integration tests: Generated URLs provide proper file access
- Manual validation: Frontend can access files using generated URLs

## System Stability
- Proper validation of file existence before URL generation
- Clear error messages for invalid requests
- No security leaks through URL generation process

### Presigned URL Implementation
```csharp
public async Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime)
{
    try
    {
        var key = ExtractKeyFromStoragePath(storagePath);

        // Verify object exists
        var headRequest = new GetObjectMetadataRequest
        {
            BucketName = _options.BucketName,
            Key = key
        };

        await _s3Client.GetObjectMetadataAsync(headRequest);

        // Generate presigned URL
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiryTime)
        };

        return _s3Client.GetPreSignedURL(request);
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        throw new FileNotFoundException($"File not found in S3: {storagePath}");
    }
}
```