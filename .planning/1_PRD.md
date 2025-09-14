# Azure Removal & Code Simplification - Product Requirements Document

**Created:** 2025-09-14
**Version:** 1.0

## Overview

### Feature Summary
Remove all Azure-specific code from the Better Call Saul application and simplify the dual-cloud architecture to use only local/mock services or keep AWS services where beneficial. This initiative aims to reduce complexity, eliminate vendor lock-in, reduce dependencies, and streamline the development/deployment process.

### Problem Statement
The current codebase supports both Azure and AWS cloud providers through a complex configuration system, resulting in:
- High maintenance overhead with dual service implementations
- Increased deployment complexity with multiple cloud-specific configurations
- Vendor lock-in risk across two cloud providers
- Complex dependency injection logic for cloud provider selection
- Additional licensing/service costs for unused Azure services

### Goals
1. **Simplify Architecture**: Eliminate dual-cloud provider pattern
2. **Reduce Dependencies**: Remove Azure-specific NuGet packages and configurations
3. **Improve Maintainability**: Single code path for each service type
4. **Cost Optimization**: Eliminate unused cloud service costs
5. **Development Efficiency**: Simplified local development setup

### Success Metrics
- ✅ Zero Azure-specific code references remaining
- ✅ Reduced NuGet package dependencies by 4+ packages
- ✅ Single configuration path for all services
- ✅ All tests pass with simplified architecture
- ✅ Local development works without cloud dependencies

## Requirements

### Core Functional Requirements

**R1: Service Replacement Strategy**
- Replace Azure OpenAI with local mock AI service or keep AWS Bedrock
- Replace Azure Blob Storage with local file storage or keep AWS S3
- Replace Azure Form Recognizer with mock text extraction service or keep AWS Textract
- Maintain existing API contracts and interfaces

**R2: Configuration Simplification**
- Remove CloudProviderOptions dual-provider pattern
- Simplify appsettings.json configuration structure
- Remove Azure-specific environment variable requirements
- Maintain backward compatibility during transition

**R3: Dependency Management**
- Remove Azure.AI.OpenAI package dependency
- Remove Azure.Storage.Blobs package dependency
- Remove Azure.AI.FormRecognizer package dependency
- Keep or consolidate AWS packages as needed

**R4: Code Cleanup**
- Remove Azure-specific service implementations
- Remove Azure-specific interfaces (IAzureOpenAIService)
- Remove Azure-specific configuration classes
- Remove Azure-specific tests and mocks

### Constraints

**C1: API Compatibility**
- Must maintain existing REST API endpoints
- No breaking changes to frontend integration
- Preserve existing data models and responses

**C2: Data Preservation**
- No data loss during migration
- Existing file uploads must remain accessible
- Database schema remains unchanged

**C3: Testing Coverage**
- All existing tests must be updated to work with new architecture
- No reduction in test coverage percentage
- Integration tests must pass with new service implementations

**C4: Performance**
- Local services should provide reasonable response times
- Mock services should simulate realistic delays
- No significant degradation in user experience

### Dependencies

**D1: Service Selection Decision**
- Decision needed: Keep AWS services, use local/mock services, or hybrid approach
- AWS services provide production-ready functionality
- Local/mock services eliminate cloud dependencies entirely

**D2: Data Migration Planning**
- If removing Azure Blob Storage, need file migration strategy
- Local storage implementation must handle existing file references
- File access patterns must remain consistent

## User Experience

### Basic User Flow
1. **Developer Experience**: Simplified local development without cloud setup requirements
2. **Production Deployment**: Single cloud provider or fully local deployment
3. **Admin Configuration**: Reduced configuration complexity in deployment environments
4. **End User**: No visible changes to application functionality

### UI Considerations
- No UI changes required
- All existing functionality preserved
- Performance should remain consistent
- Error messages updated to reflect new service architecture

## Acceptance Criteria

### Primary Acceptance Criteria

**AC1: Azure Code Removal**
- All Azure-specific service files deleted
- All Azure NuGet packages removed from .csproj files
- All Azure configuration classes removed
- No references to "Azure", "Blob", or "FormRecognizer" in remaining code

**AC2: Service Functionality**
- AI analysis continues to work (mock or AWS Bedrock)
- File upload/storage continues to work (local or AWS S3)
- Text extraction continues to work (mock or AWS Textract)
- All existing API endpoints return expected responses

**AC3: Configuration Cleanup**
- Single service selection per feature area
- Removed dual-provider configuration logic
- Simplified appsettings.json structure
- Clear documentation of new configuration requirements

**AC4: Testing Coverage**
- All unit tests pass with updated service implementations
- Integration tests validate new service architecture
- Mock services provide realistic test scenarios
- Test coverage percentage maintained or improved

## Open Questions ⚠️

**Q1: Service Selection Strategy**
- Should we keep AWS services (Bedrock, S3, Textract) for production capability?
- Should we use only local/mock services for full cloud independence?
- Should we use a hybrid approach (local dev, AWS prod)?

**Q2: File Migration Strategy**
- Are there existing files in Azure Blob Storage that need migration?
- How do we handle file URLs that reference Azure storage?
- What's the strategy for maintaining file access during transition?

**Q3: AI Service Selection**
- Keep AWS Bedrock for production AI capabilities?
- Use local mock AI service for all environments?
- Consider alternative local AI solutions (Ollama, local models)?

**Q4: Deployment Impact**
- How does this affect existing production deployments?
- What's the rollback strategy if issues arise?
- Are there any existing Azure resources that need decommissioning?

## Next Steps

1. **Decision Phase**: Resolve open questions about service selection strategy
2. **Technical Planning**: Create detailed implementation plan based on decisions
3. **Risk Assessment**: Identify and plan mitigation for technical risks
4. **Implementation**: Execute removal and simplification in phases
5. **Testing**: Validate all functionality with new architecture
6. **Documentation**: Update all relevant documentation and deployment guides