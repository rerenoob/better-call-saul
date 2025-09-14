# Task: Implement AWS Bedrock Case Analysis Functionality

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.1 AWS Bedrock AI Service Implementation
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 02-aws-implementation/001-aws-bedrock-setup: AWS Bedrock infrastructure setup completed

### External Dependencies
- AWS Bedrock Claude model access
- Understanding of Claude API prompt formatting
- Legal domain knowledge for prompt optimization

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/AI/AWSBedrockService.cs`: Implement AnalyzeCaseAsync method
- `BetterCallSaul.Infrastructure/Helpers/BedrockPromptBuilder.cs`: Claude-specific prompt formatting
- `BetterCallSaul.Infrastructure/Mappers/BedrockResponseMapper.cs`: Response normalization
- `BetterCallSaul.Tests/Services/AI/AWSBedrockServiceTests.cs`: Case analysis tests

### Code Patterns
- Follow existing AzureOpenAIService prompt patterns
- Use AWS SDK async patterns with CancellationToken support
- Implement proper error handling for AWS service exceptions

## Acceptance Criteria
- [ ] AnalyzeCaseAsync method produces case analysis comparable to Azure OpenAI
- [ ] Claude prompt formatting optimized for legal case analysis
- [ ] Response normalization converts Bedrock output to standard AIResponse format
- [ ] Token counting and usage tracking implemented
- [ ] Error handling covers AWS-specific exceptions (throttling, access denied, model errors)
- [ ] Performance metrics (processing time, confidence scores) properly calculated

## Testing Strategy
- Unit tests: Prompt building, response mapping, error handling
- Integration tests: Real AWS Bedrock API calls with test cases
- Manual validation: Compare analysis quality with existing Azure implementation

## System Stability
- Graceful handling of AWS service outages or throttling
- Proper timeout handling for long-running analysis requests
- Retry logic with exponential backoff for transient errors

### Claude Prompt Structure
```csharp
private string BuildClaudePrompt(string documentText, string caseContext)
{
    return $@"Human: You are an AI legal assistant specializing in case analysis for public defenders.

Document: {documentText}
Context: {caseContext}

Provide comprehensive case analysis including:
1. Case Viability Assessment (0-100%)
2. Key Legal Issues
3. Potential Defenses
4. Evidence Strength
5. Recommended Next Steps