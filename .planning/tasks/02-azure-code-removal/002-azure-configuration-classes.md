# Task: Remove Azure Configuration Classes

## Overview
- **Parent Feature**: AZURE-02 Azure Service Implementation Removal (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-azure-service-implementations.md: Azure services removed first to avoid compilation errors

### External Dependencies
- Configuration files need to be updated before removing classes
- Program.cs service registration must be updated

## Implementation Details
### Files to Remove
- `BetterCallSaul.Core/Configuration/AzureBlobStorageOptions.cs`: Azure Blob Storage configuration
- `BetterCallSaul.Core/Configuration/FormRecognizerOptions.cs`: Azure Form Recognizer configuration
- `BetterCallSaul.Core/Configuration/OpenAIOptions.cs`: Azure OpenAI configuration
- `BetterCallSaul.Core/Configuration/CloudProviderOptions.cs`: Dual-provider configuration system

### Files to Analyze for References
- `BetterCallSaul.API/Program.cs`: Remove configuration binding for deleted classes
- All appsettings files: Remove configuration sections (handled in separate task)
- Test files: Remove configuration-related tests

### Code References to Update
```csharp
// Remove these configuration bindings from Program.cs:
builder.Services.Configure<AzureBlobStorageOptions>(...)
builder.Services.Configure<FormRecognizerOptions>(...)
builder.Services.Configure<OpenAIOptions>(...)
builder.Services.Configure<CloudProviderOptions>(...)
```

### Code Patterns
- Remove IOptions<AzureXOptions> dependencies throughout codebase
- Update service constructors that used Azure configuration classes
- Ensure AWS configuration classes remain for production environment

## Acceptance Criteria
- [ ] All 4 Azure configuration class files deleted from repository
- [ ] No compilation errors after configuration class removal
- [ ] Program.cs no longer configures deleted configuration classes
- [ ] No remaining IOptions<AzureXOptions> dependencies in service constructors
- [ ] Build succeeds with simplified configuration structure
- [ ] Git history preserves deleted configuration classes
- [ ] AWS configuration classes remain intact for production use

## Testing Strategy
- Unit tests: Verify application builds and starts without Azure configuration
- Integration tests: Service resolution works without Azure configuration dependencies
- Manual validation:
  1. Remove configuration classes and verify build
  2. Search for remaining IOptions<AzureXOptions> references
  3. Start application and verify no configuration binding errors

## System Stability
- How this task maintains operational state: Configuration removal after services no longer need them
- Rollback strategy if needed: Git restore deleted configuration files, restore Program.cs bindings
- Impact on existing functionality: No impact if services properly updated to not use Azure config

## Notes
- This task completes the Azure configuration cleanup
- Focus on Core/Configuration classes, appsettings files handled separately
- Ensure AWS configuration patterns remain for production environment
- Remove any validation or extension methods specific to Azure configuration