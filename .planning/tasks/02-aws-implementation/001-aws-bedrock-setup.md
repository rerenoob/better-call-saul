# Task: Set Up AWS Bedrock Service Infrastructure and Dependencies

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.1 AWS Bedrock AI Service Implementation
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/004-cloud-provider-configuration: Configuration system completed
- [ ] 01-foundation-abstractions/005-service-factory-implementation: Service factory completed

### External Dependencies
- AWS account with Bedrock access enabled
- AWS SDK for .NET v3.x NuGet packages
- Claude model access permissions in AWS account

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/BetterCallSaul.Infrastructure.csproj`: Add AWS SDK dependencies
- `BetterCallSaul.Core/Configuration/ProviderSettings/AWSBedrockOptions.cs`: Bedrock configuration options
- `BetterCallSaul.Infrastructure/Services/AI/AWSBedrockService.cs`: Basic service structure (skeleton)
- `BetterCallSaul.Tests/Services/AI/AWSBedrockServiceTests.cs`: Unit test structure setup

### Code Patterns
- Follow existing configuration option patterns
- Use AWS SDK best practices for async operations
- Implement proper AWS credential handling (IAM roles, profiles)

## Acceptance Criteria
- [ ] AWS SDK NuGet packages installed (AWS.BedrockRuntime, AWSSDK.BedrockRuntime)
- [ ] AWSBedrockOptions configuration class with Region, ModelId, MaxTokens, Temperature properties
- [ ] Basic AWSBedrockService class structure implementing IAIService interface
- [ ] AWS credential configuration supports IAM roles, profiles, and environment variables
- [ ] Unit test project structure created with mock AWS service setup
- [ ] Service factory recognizes AWS provider and creates AWSBedrockService instance

## Testing Strategy
- Unit tests: Configuration loading, AWS client initialization
- Integration tests: AWS account connectivity and service availability
- Manual validation: Service factory creates AWS service when configured

## System Stability
- No impact on existing Azure functionality
- AWS service gracefully handles missing credentials or configuration
- Clear error messages for AWS setup issues

### NuGet Package Dependencies
```xml
<PackageReference Include="AWS.BedrockRuntime" Version="1.0.0" />
<PackageReference Include="AWSSDK.BedrockRuntime" Version="3.7.300" />
<PackageReference Include="AWSSDK.Core" Version="3.7.300" />
```