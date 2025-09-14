# Task: Set Up AWS Textract Document Processing Service

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.3 AWS Textract Document Processing Implementation
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/003-document-processing-interface: Document processing interface completed
- [ ] 01-foundation-abstractions/005-service-factory-implementation: Service factory completed

### External Dependencies
- AWS account with Textract access enabled
- AWS SDK for .NET Textract packages
- Understanding of Textract async processing patterns

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/BetterCallSaul.Infrastructure.csproj`: Add AWS Textract SDK dependencies
- `BetterCallSaul.Core/Configuration/ProviderSettings/AWSTextractOptions.cs`: Textract configuration options
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSTextractService.cs`: Basic service structure
- `BetterCallSaul.Tests/Services/FileProcessing/AWSTextractServiceTests.cs`: Unit test setup

### Code Patterns
- Follow existing ITextExtractionService patterns from AzureFormRecognizerService
- Use AWS SDK async patterns for both sync and async document analysis
- Implement proper job polling for async operations

## Acceptance Criteria
- [ ] AWS Textract SDK NuGet packages installed (AWSSDK.Textract)
- [ ] AWSTextractOptions configuration class with Region, MaxPollingTime, PollingInterval properties
- [ ] Basic AWSTextractService class implementing IDocumentProcessingService interface
- [ ] Textract client initialization with proper credential and region configuration
- [ ] Support for both synchronous and asynchronous document analysis
- [ ] Unit test project structure with mock Textract service setup

## Testing Strategy
- Unit tests: Configuration loading, Textract client initialization
- Integration tests: Textract service connectivity and permissions validation
- Manual validation: Service factory creates Textract service correctly

## System Stability
- No impact on existing Azure Form Recognizer functionality
- Textract service gracefully handles missing credentials or permissions
- Clear error messages for Textract configuration and access issues

### Textract Configuration Structure
```json
{
  "AWS": {
    "Textract": {
      "Region": "us-east-1",
      "MaxPollingTimeMs": 300000,
      "PollingIntervalMs": 5000,
      "ConfidenceThreshold": 0.7,
      "MaxRetries": 3
    }
  }
}
```