# Task: Remove Azure NuGet Package Dependencies

## Overview
- **Parent Feature**: AZURE-05 NuGet Package Dependencies Update (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Low
- **Estimated Time**: 1 hour
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-azure-service-implementations.md: Azure services must be removed first
- [x] 002-azure-configuration-classes.md: Azure configuration classes removed

### External Dependencies
- .NET SDK for package operations
- Successful build validation capability

## Implementation Details
### Files to Modify
- `BetterCallSaul.Infrastructure/BetterCallSaul.Infrastructure.csproj`: Remove Azure package references

### Packages to Remove
- `Azure.AI.OpenAI` (Version 1.0.0-beta.17): Azure OpenAI client library
- `Azure.Storage.Blobs` (Version 12.25.0): Azure Blob Storage client library
- `Azure.AI.FormRecognizer` (Version 4.1.0): Azure Form Recognizer client library

### Packages to Retain
- `AWSSDK.BedrockRuntime` (Version 3.7.500.2): AWS Bedrock for production AI
- `AWSSDK.S3` (Version 3.7.500.2): AWS S3 for production storage
- `AWSSDK.Textract` (Version 3.7.500.2): AWS Textract for production text extraction
- `AWSSDK.Extensions.NETCore.Setup` (Version 3.7.300): AWS service integration

### Current .csproj Content (lines to remove)
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.25.0" />
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.17" />
<PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />
```

### Code Patterns
- Use dotnet remove package command for clean removal
- Verify no transitive dependencies remain
- Check for any using statements referencing Azure libraries

## Acceptance Criteria
- [ ] All 3 Azure packages removed from BetterCallSaul.Infrastructure.csproj
- [ ] dotnet restore succeeds without errors
- [ ] dotnet build succeeds without missing package errors
- [ ] No Azure using statements remain in any source files
- [ ] AWS packages remain in project file for production use
- [ ] Package lock file updated (if using package lock)
- [ ] No transitive Azure dependencies listed in dependency tree

## Testing Strategy
- Unit tests: Build validation after package removal
- Integration tests: Application startup without Azure packages
- Manual validation:
  1. Run `dotnet remove package` commands
  2. Execute `dotnet restore` and verify success
  3. Execute `dotnet build` and verify no package-related errors
  4. Check `dotnet list package` output for any remaining Azure packages

## System Stability
- How this task maintains operational state: Packages removed only after code no longer references them
- Rollback strategy if needed: Re-add packages via `dotnet add package` commands
- Impact on existing functionality: No impact if Azure code properly removed first

## Commands to Execute
```bash
cd BetterCallSaul.Infrastructure
dotnet remove package Azure.AI.OpenAI
dotnet remove package Azure.Storage.Blobs
dotnet remove package Azure.AI.FormRecognizer
dotnet restore
dotnet build
```

## Notes
- This is a low-risk task as packages are removed after code cleanup
- Verify no implicit using statements for Azure namespaces remain
- Check for any Azure-specific configuration or dependency injection that might cause runtime errors