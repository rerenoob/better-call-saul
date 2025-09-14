# Task: Set Up Google Vertex AI Service Infrastructure

## Overview
- **Parent Feature**: Phase 3 Google Cloud Implementation - Task 3.1 Google Vertex AI Service Implementation
- **Complexity**: Medium
- **Estimated Time**: 7 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/001-ai-service-interface: AI service interface completed
- [ ] 01-foundation-abstractions/005-service-factory-implementation: Service factory completed

### External Dependencies
- Google Cloud project with Vertex AI API enabled
- Google Cloud Client Libraries for .NET
- Service account with Vertex AI permissions
- Understanding of PaLM and Gemini model access

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/BetterCallSaul.Infrastructure.csproj`: Add Google Cloud SDK dependencies
- `BetterCallSaul.Core/Configuration/ProviderSettings/GoogleVertexAIOptions.cs`: Vertex AI configuration
- `BetterCallSaul.Infrastructure/Services/AI/GoogleVertexAIService.cs`: Basic service structure
- `BetterCallSaul.Tests/Services/AI/GoogleVertexAIServiceTests.cs`: Unit test setup

### Code Patterns
- Follow Google Cloud SDK authentication patterns (service accounts, ADC)
- Use Google Cloud async patterns with proper error handling
- Implement both PaLM and Gemini model support

## Acceptance Criteria
- [ ] Google Cloud AI Platform NuGet packages installed (Google.Cloud.AIPlatform.V1)
- [ ] GoogleVertexAIOptions with ProjectId, Location, ModelId, ServiceAccountPath properties
- [ ] Basic GoogleVertexAIService implementing IAIService interface
- [ ] Authentication supports both service account files and Application Default Credentials
- [ ] Support for both PaLM and Gemini model families
- [ ] Service factory creates Vertex AI service when Google provider is configured

## Testing Strategy
- Unit tests: Configuration loading, client initialization, authentication
- Integration tests: Google Cloud API connectivity and model access
- Manual validation: Service factory creates Google service correctly

## System Stability
- No impact on existing Azure or AWS functionality
- Graceful handling of missing credentials or API access
- Clear error messages for Google Cloud setup issues

### Google Cloud Configuration Structure
```json
{
  "Google": {
    "VertexAI": {
      "ProjectId": "bettercallsaul-project",
      "Location": "us-central1",
      "ModelId": "gemini-pro",
      "ServiceAccountPath": "/path/to/service-account.json",
      "MaxTokens": 2000,
      "Temperature": 0.3
    }
  }
}
```