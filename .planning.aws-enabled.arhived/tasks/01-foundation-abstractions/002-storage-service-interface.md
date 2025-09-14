# Task: Create IStorageService Interface for Azure/AWS Switching

## Overview
- **Parent Feature**: AWS Migration - Foundation Layer
- **Complexity**: Low-Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundation task, can run parallel with 001)

### External Dependencies
- Existing file upload services for interface extraction

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Interfaces/Services/IStorageService.cs`: New generic storage service interface
- `BetterCallSaul.Core/Models/ServiceResponses/StorageResult.cs`: Standardized storage operation result
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AzureBlobStorageService.cs`: Modify to implement new interface
- `BetterCallSaul.Infrastructure/Services/FileProcessing/FileUploadService.cs`: Modify to implement new interface

### Code Patterns
- Follow existing IFileUploadService patterns
- Use standard async/await patterns for I/O operations
- Implement proper disposal patterns for stream handling

## Acceptance Criteria
- [ ] `IStorageService` interface covers essential file operations
- [ ] `StorageResult` model includes Success, StoragePath, and basic metadata
- [ ] Existing Azure storage services implement IStorageService
- [ ] Interface supports file upload and secure URL generation
- [ ] Ready for AWS S3 implementation
- [ ] No breaking changes to existing functionality

## Testing Strategy
- Unit tests: Interface implementation validation, result model serialization
- Integration tests: File upload/download workflows with both Azure and local storage
- Manual validation: Existing file upload functionality works unchanged

## System Stability
- Existing file upload functionality continues to work
- No disruption to document storage workflows
- Simple interface layer for Azure/AWS switching

### Interface Structure
```csharp
public interface IStorageService
{
    Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId);
    Task<bool> DeleteFileAsync(string storagePath);
    Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime);
    Task<bool> ValidateFileAsync(IFormFile file);
    Task<string> GenerateUniqueFileNameAsync(string originalFileName);
}
```