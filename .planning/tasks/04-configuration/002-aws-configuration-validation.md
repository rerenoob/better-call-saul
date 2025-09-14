# Task: AWS Configuration Validation and Documentation

## Overview
- **Parent Feature**: AZURE-04 Configuration Cleanup (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Low
- **Estimated Time**: 2 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-appsettings-cleanup.md: Configuration files cleaned up
- [x] 003-nuget-package-cleanup.md: AWS packages retained for production

### External Dependencies
- AWS service configuration examples
- Production deployment documentation requirements

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.API/Program.cs`: Add AWS configuration validation for production
- `docs/AWS_CONFIGURATION.md`: New documentation for AWS setup
- Update existing deployment documentation

### Configuration Validation Logic
Add startup validation for production environment:
```csharp
if (!builder.Environment.IsDevelopment())
{
    // Validate AWS configuration is present
    var awsConfig = builder.Configuration.GetSection("AWS");
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
    {
        throw new InvalidOperationException("AWS credentials are required for production environment");
    }
}
```

### AWS Configuration Documentation
Create comprehensive AWS setup guide:
- Required AWS services (Bedrock, S3, Textract)
- IAM permission requirements
- Environment variable configuration
- Regional considerations
- Cost estimation and service limits

### Code Patterns
- Follow existing configuration validation patterns in Program.cs
- Use IConfiguration and environment variable access
- Provide clear error messages for missing configuration

## Acceptance Criteria
- [ ] Production environment validates required AWS configuration at startup
- [ ] Clear error messages for missing AWS credentials or configuration
- [ ] Comprehensive AWS setup documentation created
- [ ] Environment variable requirements clearly documented
- [ ] IAM permission requirements specified for each AWS service
- [ ] Regional configuration options documented
- [ ] Cost implications and service limits documented
- [ ] Development environment bypasses AWS configuration validation

## Testing Strategy
- Unit tests: Configuration validation logic
- Integration tests: Startup validation with missing/present AWS configuration
- Manual validation:
  1. Start production environment without AWS config - should fail gracefully
  2. Start production environment with AWS config - should succeed
  3. Verify error messages are clear and actionable

## System Stability
- How this task maintains operational state: Prevents production deployments with incomplete configuration
- Rollback strategy if needed: Remove configuration validation if causing deployment issues
- Impact on existing functionality: Improves production deployment reliability

## AWS Services Documentation Structure

### Required AWS Services
1. **Amazon Bedrock**
   - Purpose: AI analysis and legal document processing
   - Required permissions: bedrock:InvokeModel
   - Configuration: Region, Model ID

2. **Amazon S3**
   - Purpose: Document storage and retrieval
   - Required permissions: s3:GetObject, s3:PutObject, s3:DeleteObject
   - Configuration: Bucket name, Region

3. **Amazon Textract**
   - Purpose: Document text extraction
   - Required permissions: textract:DetectDocumentText, textract:AnalyzeDocument
   - Configuration: Region

### Environment Variables
```bash
# Required for production
AWS_ACCESS_KEY_ID=your-access-key-id
AWS_SECRET_ACCESS_KEY=your-secret-access-key
AWS_REGION=us-east-1

# Optional configuration
AWS_BEDROCK_MODEL_ID=anthropic.claude-v2
AWS_S3_BUCKET_NAME=better-call-saul-prod
```

## Notes
- This task ensures production deployments have proper AWS configuration
- Configuration validation prevents runtime errors in production
- Documentation should be clear enough for deployment teams
- Consider providing terraform/CloudFormation templates for AWS resource setup