# Task: Update Project Documentation

## Overview
- **Parent Feature**: AZURE-07 Documentation Updates (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] All implementation tasks completed
- [x] Testing validation tasks completed

### External Dependencies
- Access to all documentation files
- Understanding of new simplified architecture

## Implementation Details
### Files to Modify
- `CLAUDE.md`: Update project instructions and architecture description
- `README.md`: Update project overview and setup instructions
- `docs/ARCHITECTURE.md`: Update architecture diagrams and service descriptions
- `docs/DEPLOYMENT_GUIDE.md`: Simplify deployment process
- `docs/DEVELOPMENT_GUIDE.md`: Update local development setup

### Key Documentation Updates

#### CLAUDE.md Updates
- Remove Azure service references from project overview
- Update architecture section to reflect simplified service pattern
- Remove Azure-specific deployment instructions
- Update common commands to reflect simplified configuration

#### README.md Updates
- Update project description to remove dual-cloud references
- Simplify setup instructions for local development
- Update service architecture description
- Remove Azure-specific environment variable requirements

#### Architecture Documentation
- Update service layer diagrams to show environment-based selection
- Remove dual-cloud provider architecture diagrams
- Add mock service descriptions for development environment
- Update deployment architecture to show single cloud provider pattern

#### Deployment Guide Updates
- Remove Azure-specific deployment sections
- Simplify environment variable configuration
- Add AWS-only production deployment instructions
- Update troubleshooting guide to reflect new architecture

#### Development Guide Updates
- Simplify local development setup (no cloud dependencies)
- Update service development patterns
- Remove Azure service configuration instructions
- Add mock service development guidance

## Content Updates Required

### Remove Azure References
- Search and remove all mentions of Azure services
- Remove dual-cloud provider configuration examples
- Delete Azure-specific troubleshooting sections
- Remove Azure deployment workflows

### Add Simplified Architecture Content
- Document environment-based service selection
- Explain mock service behavior in development
- Update service interface documentation
- Add local development setup simplicity benefits

### Update Configuration Examples
```markdown
## Development Environment
No cloud service configuration required. Mock services are used automatically.

## Production Environment
Configure AWS services via environment variables:
- AWS_ACCESS_KEY_ID=your-access-key
- AWS_SECRET_ACCESS_KEY=your-secret-key
- AWS_REGION=us-east-1
```

## Acceptance Criteria
- [ ] All Azure references removed from documentation files
- [ ] Architecture documentation reflects simplified service pattern
- [ ] Local development setup clearly documented (no cloud dependencies)
- [ ] AWS production configuration properly documented
- [ ] Deployment guides updated to remove Azure sections
- [ ] Development guides show simplified setup process
- [ ] Configuration examples accurate for new architecture
- [ ] Troubleshooting sections updated for simplified architecture

## Testing Strategy
- Documentation review: Verify all Azure references removed
- Setup validation: Follow updated setup instructions to verify accuracy
- Manual validation:
  1. Review all documentation files for Azure references
  2. Follow updated development setup instructions
  3. Validate AWS production configuration examples

## System Stability
- How this task maintains operational state: Provides accurate documentation for new architecture
- Rollback strategy if needed: Restore documentation from git history
- Impact on existing functionality: No impact, improves developer experience

## Documentation Structure Updates

### Architecture Overview
Before:
```
Dual-cloud architecture supporting Azure and AWS providers with runtime switching
```

After:
```
Simplified architecture with environment-based service selection:
- Development: Local/mock services for rapid development
- Production: AWS services for production capabilities
```

### Service Descriptions
- **IAIService**: Mock service (dev) / AWS Bedrock (prod)
- **IStorageService**: Local filesystem (dev) / AWS S3 (prod)
- **ITextExtractionService**: Mock extraction (dev) / AWS Textract (prod)

### Benefits Documentation
- Simplified local development setup
- Reduced configuration complexity
- Single cloud provider for production
- Improved maintainability and debugging

## Quality Assurance
- Documentation should be clear and actionable
- Setup instructions should be tested by following them exactly
- Configuration examples should be copy-paste ready
- Architecture diagrams should accurately reflect implementation

## Notes
- This task completes the Azure removal project by updating all documentation
- Focus on clarity and simplicity in setup instructions
- Ensure all team members can follow updated development setup
- Consider creating quick-start guides for new developers