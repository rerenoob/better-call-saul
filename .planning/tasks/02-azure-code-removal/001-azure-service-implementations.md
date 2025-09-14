# Task: Remove Azure Service Implementations

## Overview
- **Parent Feature**: AZURE-02 Azure Service Implementation Removal (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: High
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-di-container-refactor.md: DI container refactored to not reference Azure services

### External Dependencies
- Git repository for safe file deletion
- Build validation capability

## Implementation Details
### Files to Remove
- `BetterCallSaul.Infrastructure/Services/AI/AzureOpenAIService.cs`: Azure OpenAI service implementation
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AzureBlobStorageService.cs`: Azure Blob Storage service
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AzureFormRecognizerService.cs`: Azure Form Recognizer service
- `BetterCallSaul.Core/Interfaces/Services/IAzureOpenAIService.cs`: Azure-specific AI service interface

### Files to Analyze for References
- Search all `.cs` files for imports and references to removed files
- Update any remaining references to use base interfaces (IAIService, IStorageService, etc.)

### Code Patterns
- Ensure all Azure service references are replaced with base interface usage
- Verify no direct Azure service instantiation remains

## Acceptance Criteria
- [ ] All 4 Azure service implementation files deleted from repository
- [ ] Build succeeds after file deletion
- [ ] No compilation errors related to missing Azure services
- [ ] No remaining imports of deleted Azure service classes
- [ ] Git history preserves deleted files for potential recovery
- [ ] All references updated to use base interfaces (IAIService, IStorageService, etc.)
- [ ] No Azure-specific service instantiation remains in codebase

## Testing Strategy
- Unit tests: Verify build compiles successfully
- Integration tests: Application startup without Azure service references
- Manual validation:
  1. Delete files and attempt build
  2. Search codebase for any remaining Azure service references
  3. Verify application starts without Azure dependencies

## System Stability
- How this task maintains operational state: Files are only deleted after DI container no longer references them
- Rollback strategy if needed: Git restore deleted files, revert DI container changes if needed
- Impact on existing functionality: No impact if DI properly configured, services replaced by mock/AWS alternatives

## Notes
- Ensure branch/commit before file deletion for easy rollback
- Use IDE "Find Usages" to identify all references before deletion
- This task removes implementation but preserves interface contracts
- Focus on Infrastructure layer services, Core interfaces handled separately