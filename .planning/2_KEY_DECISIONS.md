# Azure Removal & Code Simplification - Key Architectural Decisions

**Created:** 2025-09-14
**Version:** 1.0

## Decision 1: Service Replacement Strategy

### Context
The current system supports both Azure and AWS cloud providers for AI services, storage, and text extraction. We need to decide how to replace Azure services while maintaining functionality.

### Options Considered
1. **Full AWS Migration**: Replace Azure services with AWS equivalents
2. **Full Local/Mock Services**: Use only local file storage and mock services
3. **Hybrid Approach**: Local/mock for development, AWS for production
4. **Selective Replacement**: Mix of local and cloud services based on criticality

### Chosen Solution: Hybrid Approach (Local Dev, AWS Prod)
**Rationale:**
- **Development Simplicity**: Local/mock services eliminate cloud setup requirements for developers
- **Production Capability**: AWS services provide production-ready AI and document processing
- **Cost Optimization**: No Azure licensing costs, AWS services only used in production
- **Flexibility**: Easy to adjust service selection per environment

**Implementation Details:**
- **AI Service**: Mock AI service for development, AWS Bedrock for production
- **Storage Service**: Local file storage for development, AWS S3 for production
- **Text Extraction**: Mock extraction for development, AWS Textract for production
- **Environment Detection**: Use `IWebHostEnvironment` to select service implementations

---

## Decision 2: Configuration Architecture Simplification

### Context
The current `CloudProviderOptions` class supports dual Azure/AWS configuration with runtime provider selection. This adds complexity for single-provider usage.

### Options Considered
1. **Remove CloudProviderOptions Entirely**: Direct service configuration
2. **Simplify CloudProviderOptions**: Remove dual-provider pattern
3. **Environment-Based Selection**: Different configs per environment
4. **Feature Flags**: Toggle individual services independently

### Chosen Solution: Environment-Based Direct Configuration
**Rationale:**
- **Clarity**: No runtime provider switching logic needed
- **Simplicity**: Direct service configuration per environment
- **Maintainability**: Easier to understand and configure
- **Environment Isolation**: Clear separation between dev/prod configurations

**Implementation Details:**
- Remove `CloudProviderOptions` class entirely
- Configure services directly based on `IWebHostEnvironment`
- Use existing service-specific configuration classes (OpenAI, S3, etc.)
- Environment variables override configuration file settings

---

## Decision 3: Dependency Management Strategy

### Context
Current project includes both Azure and AWS NuGet packages. We need to determine which packages to keep, remove, or replace.

### Options Considered
1. **Remove All Cloud Dependencies**: Pure local implementation
2. **Keep Only AWS Dependencies**: Remove Azure packages entirely
3. **Conditional Dependencies**: Different packages per build configuration
4. **Abstraction Layer**: Common interfaces with swappable implementations

### Chosen Solution: Keep Only AWS Dependencies
**Rationale:**
- **Production Capability**: AWS services provide robust production functionality
- **Reduced Complexity**: Single cloud provider reduces maintenance overhead
- **Cost Efficiency**: AWS services used only in production environments
- **Proven Integration**: Existing AWS implementations are tested and working

**Packages to Remove:**
- `Azure.AI.OpenAI` (Version 1.0.0-beta.17)
- `Azure.Storage.Blobs` (Version 12.25.0)
- `Azure.AI.FormRecognizer` (Version 4.1.0)

**Packages to Keep:**
- `AWSSDK.BedrockRuntime` (Version 3.7.500.2)
- `AWSSDK.S3` (Version 3.7.500.2)
- `AWSSDK.Textract` (Version 3.7.500.2)
- `AWSSDK.Extensions.NETCore.Setup` (Version 3.7.300)

---

## Decision 4: Service Implementation Pattern

### Context
The current dependency injection uses runtime provider selection with factory patterns. We need a simpler approach for single-provider usage.

### Options Considered
1. **Direct Registration**: Register services directly without factories
2. **Environment Factories**: Factory methods based on environment
3. **Conditional Registration**: Different services per environment
4. **Configuration-Driven**: Service selection via configuration flags

### Chosen Solution: Environment-Based Conditional Registration
**Rationale:**
- **Simplicity**: No complex factory logic needed
- **Performance**: Direct service resolution without runtime switching
- **Clarity**: Clear service registration per environment
- **Testing**: Easy to mock services for testing

**Implementation Pattern:**
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IAIService, MockAIService>();
    builder.Services.AddScoped<IStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<ITextExtractionService, MockTextExtractionService>();
}
else
{
    builder.Services.AddScoped<IAIService, AWSBedrockService>();
    builder.Services.AddScoped<IStorageService, AWSS3StorageService>();
    builder.Services.AddScoped<ITextExtractionService, AWSTextractService>();
}
```

---

## Decision 5: Data Migration Strategy

### Context
Existing installations may have files stored in Azure Blob Storage that need to remain accessible after Azure service removal.

### Options Considered
1. **Full Migration**: Move all Azure files to AWS S3 or local storage
2. **Dual Access**: Maintain Azure access for existing files, new files use new storage
3. **No Migration**: Document that existing Azure files will become inaccessible
4. **Migration Tool**: Provide optional migration utility for administrators

### Chosen Solution: No Immediate Migration, Document Impact
**Rationale:**
- **Scope Control**: Migration is complex and outside core simplification goals
- **Risk Reduction**: Avoid data loss risks during primary refactoring
- **Flexibility**: Allows deployment teams to plan migration separately
- **Documentation**: Clear communication about impact and options

**Implementation Details:**
- Document that existing Azure Blob Storage files will become inaccessible
- Provide guidance for manual file migration if needed
- Ensure new file uploads work with new storage system
- Consider future migration tool as separate project phase

---

## Technical Standards

### Code Quality
- Maintain existing interface contracts for backward compatibility
- Remove all Azure-specific code references and dependencies
- Ensure mock services provide realistic behavior for development
- Update all tests to work with simplified architecture

### Performance Standards
- Local services should respond within 100ms for typical operations
- Mock AI responses should simulate realistic processing delays (1-3 seconds)
- File operations should maintain current performance characteristics
- No degradation in production API response times

### Security Standards
- Local file storage must maintain proper access controls
- AWS services must use secure credential management
- Mock services must not expose sensitive test data
- All service interfaces must maintain existing security patterns

## Validation Criteria

Each architectural decision will be validated by:
1. **Functionality**: All existing features continue to work as expected
2. **Performance**: No significant degradation in response times
3. **Maintainability**: Simplified codebase with clear service patterns
4. **Testing**: All tests pass with updated service implementations
5. **Documentation**: Clear setup instructions for new architecture

## Next Steps

1. Implement environment-based service registration in `Program.cs`
2. Create mock service implementations for development environment
3. Remove Azure-specific configuration classes and options
4. Update NuGet package references to remove Azure dependencies
5. Update all tests to work with new service architecture