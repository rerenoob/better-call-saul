# Task: Create Local File Storage Service for Development

## Overview
- **Parent Feature**: AZURE-03 Mock Service Enhancement (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-di-container-refactor.md: DI container configured to use local storage in development

### External Dependencies
- Access to existing IStorageService interface
- Local filesystem permissions for file storage
- Understanding of existing file upload patterns

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/LocalFileStorageService.cs`: New local storage service
- `BetterCallSaul.Tests/Services/FileProcessing/LocalFileStorageServiceTests.cs`: Unit tests for local storage

### Code Patterns
- Follow existing patterns in `FileUploadService.cs` which already implements local storage
- Implement `IStorageService` interface completely
- Use similar file organization and access patterns as existing local storage
- Reference `AWSS3StorageService.cs` for interface implementation examples

### Interface to Implement
```csharp
public interface IStorageService
{
    Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId);
    Task<Stream> DownloadFileAsync(string fileId);
    Task<bool> DeleteFileAsync(string fileId);
    Task<string> GenerateAccessUrlAsync(string fileId, TimeSpan expiry);
}
```

### Local Storage Strategy
- Store files in organized directory structure: `uploads/{caseId}/{userId}/{fileName}`
- Generate unique file IDs that map to local file paths
- Maintain file metadata in memory or simple file-based index
- Simulate cloud storage access patterns for compatibility

### Example Implementation Structure
```csharp
public class LocalFileStorageService : IStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _baseStoragePath;

    public async Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId)
    {
        // Create directory structure if needed
        var directory = Path.Combine(_baseStoragePath, caseId.ToString(), userId.ToString());
        Directory.CreateDirectory(directory);

        // Save file and return storage result
        // ...
    }
}
```

## Acceptance Criteria
- [ ] LocalFileStorageService implements all IStorageService interface methods
- [ ] Files stored in organized directory structure under project root
- [ ] File upload preserves original filenames and metadata
- [ ] File download returns proper stream for saved files
- [ ] File deletion removes files from local filesystem
- [ ] Access URL generation provides file access mechanism
- [ ] Comprehensive unit test coverage for all storage operations
- [ ] Service registered in development environment DI container
- [ ] Error handling for filesystem permission issues and missing files

## Testing Strategy
- Unit tests: All IStorageService methods with file operations
- Integration tests: End-to-end file upload/download workflows
- Manual validation:
  1. Upload files through API in development mode
  2. Verify files saved to local directory structure
  3. Test file download and deletion operations
  4. Check file organization and naming

## System Stability
- How this task maintains operational state: Provides development replacement for Azure Blob Storage
- Rollback strategy if needed: Use existing FileUploadService implementation
- Impact on existing functionality: Enables local development without cloud storage dependencies

### Storage Configuration
- **Base Path**: `uploads/` directory in project root
- **Directory Structure**: `{caseId}/{userId}/{uniqueFileName}`
- **File ID Format**: Base64 encoded path or GUID mapping to file location
- **Metadata Storage**: Simple JSON files or in-memory cache for development

## File Management Features
- **File Organization**: Hierarchical directory structure by case and user
- **Cleanup**: Optional cleanup of old files for development environment
- **Access Control**: File access based on case and user associations
- **Logging**: Comprehensive logging of file operations for debugging

## Notes
- Local storage should mimic cloud storage behavior for API compatibility
- Consider file size limits appropriate for development environment
- Ensure proper error handling for disk space and permission issues
- File access URLs should work with existing frontend file handling code