# Task: Create IAIService Interface for Azure/AWS Switching

## Overview
- **Parent Feature**: AWS Migration - Foundation Layer
- **Complexity**: Low-Medium
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundation task)

### External Dependencies
- Existing AzureOpenAIService.cs for interface extraction

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Interfaces/Services/IAIService.cs`: Simple AI service interface
- `BetterCallSaul.Core/Models/ServiceResponses/AIResponse.cs`: Basic AI response model
- `BetterCallSaul.Core/Models/ServiceResponses/AIRequest.cs`: Basic AI request model
- `BetterCallSaul.Infrastructure/Services/AI/AzureOpenAIService.cs`: Update to implement interface

### Code Patterns
- Follow existing interface patterns in `BetterCallSaul.Core/Interfaces/Services/`
- Use standard C# XML documentation conventions
- Implement async/await patterns consistently

## Acceptance Criteria
- [ ] `IAIService` interface covers key methods from AzureOpenAIService
- [ ] `AIResponse` model includes Success, GeneratedText, and basic metadata
- [ ] `AIRequest` model supports essential parameters (text, context, tokens)
- [ ] AzureOpenAIService implements IAIService without breaking changes
- [ ] Interface supports async operations and streaming
- [ ] Ready for AWS Bedrock implementation

## Testing Strategy
- Unit tests: Interface contract validation
- Integration tests: Azure service still works
- Manual validation: No functionality loss

## System Stability
- Existing Azure functionality remains operational throughout implementation
- No breaking changes to current API endpoints
- Backwards compatible with existing database models

### Interface Structure
```csharp
// Simplified IAIService for Azure/AWS switching
public interface IAIService
{
    Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default);
    Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default);
    Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default);
}
```