# Task: Create Mock AI Service for Development

## Overview
- **Parent Feature**: AZURE-03 Mock Service Enhancement (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-di-container-refactor.md: DI container configured to use mock services in development

### External Dependencies
- Access to existing IAIService interface
- Understanding of expected AI response formats

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/AI/MockAIService.cs`: New mock AI service implementation
- `BetterCallSaul.Tests/Services/AI/MockAIServiceTests.cs`: Unit tests for mock service

### Code Patterns
- Follow existing service patterns in `BetterCallSaul.Infrastructure/Services/AI/` directory
- Implement `IAIService` interface completely
- Use existing AI response models from Core layer
- Reference `AWSBedrockService.cs` for interface implementation examples

### Interface to Implement
From `BetterCallSaul.Core/Interfaces/Services/IAIService.cs`:
```csharp
public interface IAIService
{
    Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default);
    Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default);
    Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default);
}
```

### Mock Response Strategy
- Generate realistic legal analysis text appropriate to request type
- Simulate processing delays (1-3 seconds) to match real AI service behavior
- Include appropriate confidence scores and metadata
- Handle edge cases and error scenarios gracefully

### Example Implementation Structure
```csharp
public class MockAIService : IAIService
{
    private readonly ILogger<MockAIService> _logger;
    private readonly Random _random;

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken)
    {
        // Simulate processing delay
        await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 4)), cancellationToken);

        // Generate realistic legal analysis response
        return new AIResponse { ... };
    }
}
```

## Acceptance Criteria
- [ ] MockAIService implements all IAIService interface methods
- [ ] Realistic legal analysis responses generated for each method
- [ ] Processing delays simulate real AI service behavior (1-3 seconds)
- [ ] Streaming analysis method yields incremental text updates
- [ ] Error handling for invalid inputs and edge cases
- [ ] Comprehensive unit test coverage for all service methods
- [ ] Mock service registered in development environment DI container
- [ ] Service provides consistent, deterministic responses for testing

## Testing Strategy
- Unit tests: All IAIService methods with various input scenarios
- Integration tests: Service resolution and basic functionality
- Manual validation:
  1. Start application in development mode
  2. Call AI analysis endpoints via API
  3. Verify realistic responses and appropriate processing delays
  4. Test streaming functionality

## System Stability
- How this task maintains operational state: Provides development replacement for Azure AI service
- Rollback strategy if needed: Temporarily use AWS Bedrock service in development
- Impact on existing functionality: Enables local development without cloud AI dependencies

## Mock Response Examples
- **Case Analysis**: Legal case summary with key issues, evidence assessment, potential outcomes
- **Legal Analysis**: Document analysis with legal implications and recommendations
- **Outcome Prediction**: Percentage confidence ratings with supporting reasoning
- **Document Summary**: Concise summary of key legal points and findings

## Notes
- Mock responses should be diverse enough for realistic testing
- Consider creating response templates for different legal document types
- Ensure mock service logs clearly indicate simulated responses
- Response quality should be sufficient for UI development and testing