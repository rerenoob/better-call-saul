# Architecture Decisions - File Processing Pipeline Fix

**Created:** 2025-09-13
**Version:** 1.0

## Decision 1: Service Validation and Configuration

### Context
Production database shows empty Documents, DocumentText, and CaseAnalyses tables despite file uploads occurring, indicating potential service configuration issues.

### Options Considered
1. **Assume Mock Services**: Continue with assumption that mock services are accidentally running in production
2. **Azure Service Validation**: Systematically validate all Azure service configurations and connections
3. **Hybrid Approach**: Implement fallback mechanisms for both mock and real services

### Chosen Solution
**Azure Service Validation** with comprehensive configuration audit.

### Rationale
- Production logs show no calls to Azure OpenAI, indicating configuration issues rather than logic failures
- File processing pipeline depends on external services - configuration validation is foundational
- Systematic validation prevents recurring issues and provides clear troubleshooting path
- Azure services are cost-effective and scalable for production workloads

### Implementation Details
- Audit Azure Form Recognizer endpoint configuration and credentials
- Validate Azure OpenAI service connectivity and API key configuration
- Verify Azure storage account configuration for file persistence
- Add health check endpoints for all Azure service dependencies
- Implement circuit breaker pattern for external service calls

## Decision 2: Processing Pipeline Architecture

### Context
Current implementation suggests synchronous processing during file upload, but production failures indicate background processing may be intended.

### Options Considered
1. **Pure Synchronous Processing**: Process OCR and analysis immediately during upload request
2. **Pure Asynchronous Processing**: Queue all processing for background execution
3. **Hybrid Synchronous/Asynchronous**: Synchronous OCR, asynchronous analysis
4. **Configurable Processing Mode**: Allow runtime configuration of processing approach

### Chosen Solution
**Hybrid Synchronous/Asynchronous** with configurable fallback.

### Rationale
- OCR processing is typically fast (1-5 seconds) and users expect immediate text extraction
- AI analysis can take 10-30 seconds and should not block upload completion
- Synchronous OCR provides immediate feedback for upload validation
- Asynchronous analysis allows for retry logic and prevents timeout issues
- Hybrid approach provides best user experience while maintaining system reliability

### Implementation Details
- OCR processing runs synchronously during file upload with 30-second timeout
- AI analysis queued for background processing with SignalR progress updates
- Configurable fallback to pure asynchronous mode if synchronous OCR fails repeatedly
- Processing status tracking in Document entity (pending, processing, completed, failed)
- Retry mechanisms with exponential backoff for failed operations

## Decision 3: Error Handling and Observability

### Context
Current production failures are silent - no meaningful error logs or user feedback available for troubleshooting.

### Options Considered
1. **Basic Error Logging**: Simple try-catch blocks with generic error messages
2. **Comprehensive Monitoring**: Structured logging, metrics, alerts, and user feedback
3. **Minimal Viable Monitoring**: Essential logs and basic user notifications

### Chosen Solution
**Comprehensive Monitoring** with structured logging and user feedback.

### Rationale
- File processing is critical user journey - failures must be visible and actionable
- Structured logging enables efficient troubleshooting and monitoring
- User feedback prevents confusion and provides clear next steps
- Metrics enable proactive issue detection and capacity planning
- Investment in observability prevents recurring production issues

### Implementation Details
- Structured logging using Serilog with correlation IDs for request tracing
- Application Insights integration for metrics, alerts, and dependency tracking
- Processing status entities with detailed error information
- User-facing error messages with specific guidance (file format, size limits, retry options)
- Admin dashboard showing processing pipeline health and failure rates
- Automated alerts for high failure rates or service unavailability

## Technical Stack Decisions

### Core Technologies (Unchanged)
- **.NET 8 Web API**: Continue with existing backend framework
- **Entity Framework Core**: Maintain current ORM for database operations
- **Azure Services**: Azure Form Recognizer (OCR), Azure OpenAI (Analysis), Azure Storage (Files)
- **SignalR**: Real-time updates for processing progress
- **React Frontend**: Maintain existing UI framework

### New Dependencies Required
- **Microsoft.ApplicationInsights**: Already added for production logging
- **Polly**: Retry and circuit breaker patterns for external service calls
- **Serilog.Enrichers.CorrelationId**: Request correlation for distributed tracing
- **Azure.Storage.Blobs**: If migrating from local file storage to Azure Blob Storage

### Configuration Management
- Azure Key Vault integration for production secrets management
- Environment-specific configuration for service endpoints and credentials
- Feature flags for processing mode configuration (sync vs async)
- Health check endpoints for dependency validation

## Next Steps
1. Review implementation breakdown for detailed task planning
2. Validate current Azure service configurations in production environment
3. Implement comprehensive logging infrastructure
4. Execute processing pipeline fixes in priority order
5. Establish monitoring and alerting for ongoing operations
