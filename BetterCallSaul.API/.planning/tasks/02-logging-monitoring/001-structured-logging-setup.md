# Task: Structured Logging Infrastructure Setup

## Overview
- **Parent Feature**: IMPL-002 Enhanced Logging and Monitoring Infrastructure
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 01-azure-service-config/001-azure-form-recognizer-audit.md: Need to understand current logging setup
- [ ] 01-azure-service-config/002-azure-openai-audit.md: Need Azure service connectivity

### External Dependencies
- Serilog NuGet packages
- Application Insights workspace
- Production environment access for log verification

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.API/Program.cs`: Configure Serilog with structured logging
- `BetterCallSaul.Infrastructure/Logging/LoggingExtensions.cs`: Custom logging extensions
- `BetterCallSaul.Infrastructure/Middleware/CorrelationIdMiddleware.cs`: Request correlation tracking
- `appsettings.json`: Logging configuration for all environments
- `BetterCallSaul.API/BetterCallSaul.API.csproj`: Add Serilog packages

### Code Patterns
- Follow existing Serilog configuration patterns in Program.cs
- Use structured logging with consistent property names
- Implement correlation ID pattern for distributed tracing

## Acceptance Criteria
- [ ] Structured logging configured with Serilog and consistent formatting
- [ ] Correlation IDs generated and tracked across all requests
- [ ] Application Insights integration sends logs with proper context
- [ ] Log levels properly configured (Debug, Info, Warning, Error)
- [ ] File processing operations have detailed logging at each step
- [ ] Performance logging for processing duration and success rates

## Testing Strategy
- Unit tests: Mock ILogger and verify log messages are generated
- Integration tests: Verify logs appear in Application Insights
- Manual validation: Review logs during file upload and processing workflow

## System Stability
- Logging configuration doesn't impact application performance
- Log overflow protection prevents disk space issues
- Graceful degradation if Application Insights is unavailable
- Sensitive data (file content, user info) not logged

## Rollback Strategy
- Logging level can be increased to reduce verbosity if needed
- Application Insights integration can be disabled via configuration
- Fallback to console logging if structured logging fails
