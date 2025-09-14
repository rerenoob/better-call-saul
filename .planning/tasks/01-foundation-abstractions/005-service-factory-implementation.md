# Task: Create Service Factory with Dependency Injection Integration

## Overview
- **Parent Feature**: Phase 1 Foundation - Task 1.3 Service Factory Implementation
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-ai-service-interface: AI service interface completed
- [ ] 002-storage-service-interface: Storage service interface completed
- [ ] 003-document-processing-interface: Document processing interface completed
- [ ] 004-cloud-provider-configuration: Configuration system completed

### External Dependencies
- ASP.NET Core dependency injection container
- Existing service registration in Program.cs

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Factories/ServiceProviderFactory.cs`: Main service factory
- `BetterCallSaul.Infrastructure/Extensions/ServiceCollectionExtensions.cs`: DI registration extensions
- `BetterCallSaul.Infrastructure/HealthChecks/ProviderHealthCheck.cs`: Health check implementation
- `BetterCallSaul.API/Program.cs`: Updated service registration

### Code Patterns
- Follow existing dependency injection patterns in Program.cs
- Use factory pattern for provider selection
- Implement health check interfaces for monitoring

## Acceptance Criteria
- [ ] ServiceProviderFactory creates appropriate service implementations based on configuration
- [ ] Dependency injection container resolves IAIService, IStorageService, IDocumentProcessingService correctly
- [ ] Health checks validate service availability and configuration for active provider
- [ ] Graceful degradation when services are unavailable (fallback to mock implementations)
- [ ] Support for runtime provider switching through configuration reload
- [ ] Factory supports both singleton and scoped service lifetimes appropriately

## Testing Strategy
- Unit tests: Factory logic, service creation, configuration validation
- Integration tests: DI container resolution, health check functionality
- Manual validation: Application starts successfully with different provider configurations

## System Stability
- Existing Azure service registration continues to work during migration
- Application startup fails fast with clear error messages for invalid configurations
- Health checks provide real-time service availability monitoring

### Factory Implementation Pattern
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudProviderOptions = configuration.GetSection("CloudProvider").Get<CloudProviderOptions>();

        services.AddScoped<IAIService>(provider =>
            cloudProviderOptions.Active switch
            {
                "Azure" => new AzureOpenAIService(...),
                "AWS" => new AWSBedrockService(...),
                "Google" => new GoogleVertexAIService(...),
                _ => throw new NotSupportedException($"Provider {cloudProviderOptions.Active} not supported")
            });

        return services;
    }
}
```