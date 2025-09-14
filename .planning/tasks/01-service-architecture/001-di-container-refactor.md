# Task: Dependency Injection Container Refactoring

## Overview
- **Parent Feature**: AZURE-01 Service Architecture Simplification (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundational task)

### External Dependencies
- Access to `BetterCallSaul.API/Program.cs`
- Understanding of current service registration patterns

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.API/Program.cs`: Replace cloud provider switching with environment-based registration
- No new files created (refactoring existing code)

### Code Patterns
- Follow existing service registration patterns in `Program.cs`
- Use `IWebHostEnvironment` for environment detection
- Maintain existing service interface contracts

### Current Code Analysis
```csharp
// Current pattern (lines 132-166 in Program.cs)
builder.Services.AddScoped<IStorageService>(serviceProvider =>
{
    var cloudProviderOptions = serviceProvider.GetRequiredService<IOptions<CloudProviderOptions>>().Value;
    if (cloudProviderOptions.Active == "AWS") { ... }
    else { ... }
});
```

### Target Pattern
```csharp
// New environment-based pattern
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<IAIService, MockAIService>();
    builder.Services.AddScoped<ITextExtractionService, MockTextExtractionService>();
}
else
{
    builder.Services.AddScoped<IStorageService, AWSS3StorageService>();
    builder.Services.AddScoped<IAIService, AWSBedrockService>();
    builder.Services.AddScoped<ITextExtractionService, AWSTextractService>();
}
```

## Acceptance Criteria
- [ ] Removed factory-based service registration for IStorageService, IAIService, ITextExtractionService
- [ ] Implemented environment-based conditional service registration
- [ ] Development environment automatically resolves to mock services
- [ ] Production environment automatically resolves to AWS services
- [ ] Application builds successfully with refactored DI container
- [ ] No runtime provider switching logic remains in Program.cs
- [ ] All existing service interfaces preserved (no breaking changes)

## Testing Strategy
- Unit tests: Verify correct service resolution in each environment
- Integration tests: Application startup validation for both environments
- Manual validation:
  1. Set ASPNETCORE_ENVIRONMENT=Development, verify mock services resolve
  2. Set ASPNETCORE_ENVIRONMENT=Production, verify AWS services resolve

## System Stability
- How this task maintains operational state: Preserves all service interfaces, only changes registration method
- Rollback strategy if needed: Revert Program.cs changes to factory pattern
- Impact on existing functionality: No functional impact, only changes service selection mechanism

## Notes
- This task prepares for Azure service removal by simplifying service resolution
- CloudProviderOptions configuration will be removed in subsequent tasks
- AWS service registration logic should remain functional for production environments