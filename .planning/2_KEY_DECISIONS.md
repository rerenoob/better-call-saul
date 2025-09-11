# Azure Blob Storage Integration - Architecture Decisions

**Date**: September 11, 2025  
**Version**: 1.0  
**Status**: Draft

## Decision 1: Storage Provider Implementation Strategy

### Context
Need to integrate cloud storage while maintaining existing local storage for development and fallback scenarios.

### Options Considered
1. **Replace Local Storage Completely**: Migrate entirely to Azure Blob Storage
2. **Dual Implementation**: Support both local and Azure storage with configuration switching
3. **Abstract Storage Layer**: Create storage provider interface with multiple implementations

### Chosen Solution
**Dual Implementation with Configuration Switching**
- Keep existing `FileUploadService` for local storage
- Create new `AzureBlobStorageService` implementing same interface
- Use dependency injection to switch between implementations based on configuration

### Rationale
- **Low Risk**: Existing functionality preserved during transition
- **Flexibility**: Development can use local storage, production uses Azure
- **Progressive Rollout**: Can migrate gradually without breaking changes
- **Fallback**: Azure outages won't completely break the application

## Decision 2: Authentication and Access Control

### Context
Legal documents require secure access control with proper authentication and authorization.

### Options Considered
1. **Public Containers**: Make blobs publicly accessible (insecure)
2. **Private Containers with SAS Tokens**: Generate time-limited access tokens
3. **Azure AD Authentication**: Use Azure Active Directory for fine-grained access control

### Chosen Solution
**Private Containers with SAS Tokens**
- All blobs stored in private containers
- Generate SAS tokens with 1-hour expiration for file access
- Tokens include read-only permissions for security
- Frontend requests SAS tokens from backend when needed

### Rationale
- **Security**: Private containers prevent unauthorized access
- **Simplicity**: SAS tokens are easy to implement and manage
- **Cost-effective**: No additional Azure AD licensing required
- **Performance**: Minimal overhead for token generation

## Decision 3: File Naming and Organization

### Context
Need consistent blob naming and organization for manageability and performance.

### Options Considered
1. **Flat Structure**: All files in single container with unique names
2. **Hierarchical Structure**: Organize by user/case/year in blob paths
3. **Partitioned Structure**: Use Azure Storage partitioning best practices

### Chosen Solution
**Hierarchical Structure with User/Case Organization**
- Blob path format: `users/{userId}/cases/{caseId}/{uniqueFileName}`
- Maintains existing unique filename generation logic
- Enables efficient querying and management by user/case
- Follows Azure Storage performance best practices

### Rationale
- **Organization**: Logical grouping of related documents
- **Performance**: Optimized for common access patterns
- **Management**: Easy to implement retention policies by user/case
- **Scalability**: Distributed across multiple partitions naturally

## Technical Stack & Dependencies

### Core Dependencies
- **Azure.Storage.Blobs** (v12.x): Official Azure Storage SDK
- **Microsoft.Extensions.Configuration**: For configuration management
- **Microsoft.Extensions.DependencyInjection**: For service registration

### Configuration Structure
```json
"AzureBlobStorage": {
  "ConnectionString": "",
  "ContainerName": "documents",
  "TempContainerName": "temp-documents",
  "SasTokenExpiryMinutes": 60,
  "UseAzureStorage": false
}
```

### Standard Patterns Used
- **Dependency Injection**: Standard .NET DI pattern for service resolution
- **Interface-based Design**: `IFileUploadService` abstraction maintained
- **Configuration-driven**: Storage provider selection via app settings
- **Error Handling**: Standard .NET exception handling with proper logging

---

**Next Steps**:
1. Create implementation breakdown with specific tasks and dependencies
2. Identify risks and mitigation strategies for Azure integration
3. Develop testing strategy for both local and Azure storage scenarios