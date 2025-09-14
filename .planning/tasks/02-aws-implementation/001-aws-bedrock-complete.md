# Task: Complete AWS Bedrock AI Service Implementation

## Overview
- **Parent Feature**: AWS Migration - AI Service
- **Complexity**: High
- **Estimated Time**: 16 hours
- **Status**: Not Started

**Note**: This combines the original Bedrock setup, case analysis, and streaming tasks.

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/001-ai-service-interface: IAIService complete
- [ ] 01-foundation-abstractions/003-configuration-and-di: AWS configuration ready

### External Dependencies
- AWS account with Bedrock access
- Claude model permissions

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/AI/AWSBedrockService.cs`: Complete implementation
- Add AWS Bedrock SDK packages
- Update existing `AzureOpenAIService.cs` to implement `IAIService`

### AWS Services Used
- **AWS Bedrock Runtime**: For Claude model access
- **Models**: Claude 3 Haiku/Sonnet for cost-effective analysis
- **Features**: Text generation, streaming responses

## Acceptance Criteria
- [ ] `AWSBedrockService` implements all `IAIService` methods
- [ ] Case analysis produces results comparable to Azure OpenAI
- [ ] Streaming analysis works for real-time responses
- [ ] Proper error handling and retry logic
- [ ] Token usage tracking and limits respected
- [ ] Performance within 15% of Azure implementation

## Key Implementation Points
### Model Selection
- **Claude 3 Haiku**: Fast, cost-effective for basic analysis
- **Claude 3 Sonnet**: Higher quality for complex legal analysis

### Prompt Engineering
- Reuse existing Azure prompts with Bedrock format adaptation
- Maintain consistent analysis quality across providers

### Streaming Support
```csharp
public async IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, CancellationToken cancellationToken = default)
{
    // AWS Bedrock streaming implementation
    var streamRequest = new InvokeModelWithResponseStreamRequest
    {
        ModelId = _options.ModelId,
        Body = CreateBedrockPayload(request)
    };

    await foreach (var chunk in _bedrockClient.InvokeModelWithResponseStreamAsync(streamRequest, cancellationToken))
    {
        yield return ProcessChunk(chunk);
    }
}
```

## Testing Strategy
- Unit tests: Bedrock client mocking, response parsing
- Integration tests: Real AWS Bedrock calls with test cases
- Performance tests: Compare response times with Azure
- Manual validation: Legal analysis quality verification

## System Stability
- Fallback to Azure if AWS services unavailable
- Graceful degradation with clear error messages
- Configuration-driven provider selection