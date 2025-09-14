# Task: Create IStorageService Interface and Response Models

## Overview
- **Parent Feature**: Phase 1 Foundation - Task 1.1 Create Service Abstraction Interfaces
- **Complexity**: Medium
- **Estimated Time**: 5 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundation task, can run parallel with 001)

### External Dependencies
- Access to existing AzureBlobStorageService.cs and FileUploadService.cs for interface extraction

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
- [ ] `IStorageService` interface includes UploadFileAsync, DeleteFileAsync, GenerateSecureUrlAsync methods
- [ ] `StorageResult` model includes Success, Message, StoragePath, FileSize, and Metadata properties
- [ ] Both AzureBlobStorageService and FileUploadService implement IStorageService
- [ ] Interface supports file upload with progress tracking capability
- [ ] Secure URL generation with configurable expiry time
- [ ] File validation and virus scanning integration points defined

## Testing Strategy
- Unit tests: Interface implementation validation, result model serialization
- Integration tests: File upload/download workflows with both Azure and local storage
- Manual validation: Existing file upload functionality works unchanged

## System Stability
- Current file upload and storage functionality remains operational
- No disruption to existing document storage workflows
- Gradual migration path from IFileUploadService to IStorageService

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