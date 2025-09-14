# Task: Clean Up Application Configuration Files

## Overview
- **Parent Feature**: AZURE-04 Configuration Cleanup (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 2 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 002-azure-configuration-classes.md: Azure configuration classes removed
- [x] 001-di-container-refactor.md: Service registration no longer uses CloudProvider configuration

### External Dependencies
- Access to all appsettings files
- Understanding of AWS configuration requirements

## Implementation Details
### Files to Modify
- `BetterCallSaul.API/appsettings.json`: Remove Azure sections, simplify structure
- `BetterCallSaul.API/appsettings.Development.json`: Configure for local/mock services
- `BetterCallSaul.API/appsettings.Production.json`: Configure for AWS services only
- `.env.example`: Update environment variable examples

### Configuration Sections to Remove
```json
// Remove these sections from all appsettings files:
"AzureOpenAI": { ... },
"AzureBlobStorage": { ... },
"AzureFormRecognizer": { ... },
"CloudProvider": {
  "Active": "Azure",
  "Azure": { ... },
  "AWS": { ... }
}
```

### Configuration Sections to Retain/Simplify
- **AWS Configuration**: Keep for production environment
- **JWT Settings**: Unchanged
- **Database Settings**: Unchanged
- **Logging Configuration**: Unchanged

### Target Configuration Structure
**appsettings.Development.json** (local development):
```json
{
  "Logging": { ... },
  // No cloud service configuration needed for development
  // Mock services require no configuration
}
```

**appsettings.Production.json** (AWS production):
```json
{
  "Logging": { ... },
  "AWS": {
    "Bedrock": {
      "Region": "us-east-1",
      "ModelId": "anthropic.claude-v2"
    },
    "S3": {
      "BucketName": "better-call-saul",
      "Region": "us-east-1"
    },
    "Textract": {
      "Region": "us-east-1"
    }
  }
}
```

### Environment Variables to Update
Update `.env.example`:
```bash
# Remove Azure-related variables
# AZURE_OPENAI_ENDPOINT=
# AZURE_OPENAI_API_KEY=
# AZURE_BLOB_STORAGE_CONNECTION_STRING=
# CLOUD_PROVIDER=

# Keep AWS variables for production
AWS_ACCESS_KEY_ID=your-aws-access-key
AWS_SECRET_ACCESS_KEY=your-aws-secret-key
AWS_REGION=us-east-1
```

## Acceptance Criteria
- [ ] All Azure configuration sections removed from appsettings files
- [ ] CloudProvider section completely removed
- [ ] Development configuration contains no cloud service settings
- [ ] Production configuration contains only AWS settings
- [ ] Environment variable examples updated to remove Azure references
- [ ] Application starts successfully with simplified configuration
- [ ] No configuration binding errors in application logs
- [ ] AWS configuration properly structured for production use

## Testing Strategy
- Unit tests: Configuration binding validation
- Integration tests: Application startup with both Development and Production configurations
- Manual validation:
  1. Start application in Development mode with simplified config
  2. Start application in Production mode with AWS config (mock credentials)
  3. Verify no configuration warnings or errors in logs

## System Stability
- How this task maintains operational state: Removes unused configuration, retains functional settings
- Rollback strategy if needed: Restore Azure configuration sections from git history
- Impact on existing functionality: No impact if service registration properly updated

## Configuration Validation
- Ensure required JWT settings remain intact
- Verify database connection strings preserved
- Validate logging configuration still functional
- Check that CORS settings remain for frontend integration

## Notes
- This task significantly simplifies configuration management
- Production deployments will need AWS environment variables configured
- Development environment requires no cloud service configuration
- Consider adding configuration validation startup checks for production AWS settings