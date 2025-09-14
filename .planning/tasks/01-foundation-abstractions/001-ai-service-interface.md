# Task: Create IAIService Interface and Standard Response Models

## Overview
- **Parent Feature**: Phase 1 Foundation - Task 1.1 Create Service Abstraction Interfaces
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundation task)

### External Dependencies
- Access to existing AzureOpenAIService.cs for interface extraction

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Interfaces/Services/IAIService.cs`: New generic AI service interface
- `BetterCallSaul.Core/Models/ServiceResponses/AIResponse.cs`: Standardized AI response model
- `BetterCallSaul.Core/Models/ServiceResponses/AIRequest.cs`: Standardized AI request model
- `BetterCallSaul.Infrastructure/Services/AI/AzureOpenAIService.cs`: Modify to implement new interface

### Code Patterns
- Follow existing interface patterns in `BetterCallSaul.Core/Interfaces/Services/`
- Use standard C# XML documentation conventions
- Implement async/await patterns consistently

## Acceptance Criteria
- [ ] `IAIService` interface defines all methods currently in AzureOpenAIService
- [ ] `AIResponse` model includes Success, GeneratedText, ConfidenceScore, ProcessingTime, and Metadata properties
- [ ] `AIRequest` model supports DocumentText, CaseContext, MaxTokens, and Temperature parameters
- [ ] AzureOpenAIService successfully implements IAIService without functionality loss
- [ ] All methods include comprehensive XML documentation
- [ ] Interface supports both sync and async streaming operations

## Testing Strategy
- Unit tests: Interface contract validation, response model serialization
- Integration tests: AzureOpenAIService implementation verification
- Manual validation: Existing case analysis functionality works unchanged

## System Stability
- Existing Azure functionality remains operational throughout implementation
- No breaking changes to current API endpoints
- Backwards compatible with existing database models

### Files to Create
```csharp
// IAIService.cs interface structure
public interface IAIService
{
    Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default);
    Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default);
    Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default);
}
```