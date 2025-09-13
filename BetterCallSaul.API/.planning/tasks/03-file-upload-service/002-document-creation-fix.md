# Task: Fix Document Entity Creation in File Upload

## Overview
- **Parent Feature**: IMPL-003 File Upload Service Enhancement
- **Complexity**: High
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 03-file-upload-service/001-fileupload-service-analysis.md: Need analysis findings
- [ ] 02-logging-monitoring/002-processing-status-tracking.md: Need status tracking

### External Dependencies
- Database write permissions
- File storage access
- Entity Framework migration capability

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/FileUploadService.cs`: Fix ProcessFileAsync method
- `BetterCallSaul.Infrastructure/Data/ApplicationDbContext.cs`: Ensure proper DbSet configuration
- `BetterCallSaul.Tests/Services/FileUploadServiceTests.cs`: Add comprehensive test coverage

### Code Patterns
- Follow existing entity persistence patterns in repository classes
- Use Entity Framework transaction scope for atomic operations
- Follow async/await patterns for database operations

## Acceptance Criteria
- [ ] Document records are created successfully for all uploaded files
- [ ] File metadata (name, size, content type, upload timestamp) properly stored
- [ ] Database transaction scope ensures atomicity of file + metadata operations
- [ ] Error handling prevents orphaned files without database records
- [ ] Processing status is set to 'Pending' immediately after document creation
- [ ] Integration with existing Case entity relationship maintained

## Testing Strategy
- Unit tests: Mock database context and verify Document entity creation
- Integration tests: Test with real database and file storage
- Manual validation: Upload files and verify Document table population

## System Stability
- File upload still works even if Document creation fails (graceful degradation)
- Database rollback cleans up any partial state
- Existing file upload functionality remains unchanged
- No breaking changes to API contracts

## Rollback Strategy
- Changes isolated to FileUploadService implementation
- Can revert to previous version if issues occur
- Database changes are additive and backward compatible
