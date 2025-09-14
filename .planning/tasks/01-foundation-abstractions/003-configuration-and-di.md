# Task: AWS Configuration and Dependency Injection Setup

## Overview
- **Parent Feature**: AWS Migration - Foundation Layer
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

**Note**: This task combines the original configuration and service factory tasks for simplicity.

## Dependencies
### Required Tasks
- [ ] 001-ai-service-interface: IAIService interface complete
- [ ] 002-storage-service-interface: IStorageService interface complete

### External Dependencies
- AWS SDK NuGet packages

## Implementation Details
### Files to Create/Modify
- `appsettings.json`: Add AWS configuration sections
- `BetterCallSaul.API/Program.cs`: Add AWS service registration
- `BetterCallSaul.Core/Configuration/CloudProviderOptions.cs`: Provider selection
- Install AWS SDK packages in Infrastructure project

### Configuration Structure
```json
{
  "CloudProvider": {
    "Active": "Azure", // "Azure" or "AWS"
    "Azure": {
      "OpenAI": { "Endpoint": "...", "DeploymentName": "..." },
      "BlobStorage": { "ConnectionString": "..." },
      "FormRecognizer": { "Endpoint": "..." }
    },
    "AWS": {
      "Bedrock": { "Region": "us-east-1", "ModelId": "anthropic.claude-v2" },
      "S3": { "BucketName": "better-call-saul", "Region": "us-east-1" },
      "Textract": { "Region": "us-east-1" }
    }
  }
}
```

## Acceptance Criteria
- [ ] Configuration loads AWS settings correctly
- [ ] Provider selection works via CloudProvider.Active setting
- [ ] Environment variable CLOUD_PROVIDER overrides config file
- [ ] Service registration creates appropriate provider services
- [ ] Azure services continue to work when Active="Azure"
- [ ] AWS SDK packages installed and ready

## Testing Strategy
- Unit tests: Configuration loading and validation
- Integration tests: Service factory creates correct implementations
- Manual validation: Provider switching via configuration

## System Stability
- Azure functionality remains unchanged
- Graceful handling of missing AWS configuration
- Clear error messages for setup issues