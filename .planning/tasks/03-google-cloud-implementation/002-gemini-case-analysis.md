# Task: Implement Google Gemini Case Analysis Functionality

## Overview
- **Parent Feature**: Phase 3 Google Cloud Implementation - Task 3.1 Google Vertex AI Service Implementation
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 03-google-cloud-implementation/001-vertex-ai-setup: Vertex AI infrastructure setup completed

### External Dependencies
- Google Cloud Gemini model access and quota allocation
- Understanding of Gemini API prompt formatting and capabilities

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/AI/GoogleVertexAIService.cs`: Implement AnalyzeCaseAsync method
- `BetterCallSaul.Infrastructure/Helpers/GeminiPromptBuilder.cs`: Gemini-specific prompt formatting
- `BetterCallSaul.Infrastructure/Mappers/GeminiResponseMapper.cs`: Response normalization
- `BetterCallSaul.Tests/Services/AI/GeminiCaseAnalysisTests.cs`: Case analysis tests

### Code Patterns
- Follow existing AI service prompt patterns
- Use Google Cloud SDK async patterns with proper error handling
- Implement Gemini-specific content formatting and safety settings

## Acceptance Criteria
- [ ] AnalyzeCaseAsync produces case analysis comparable to Azure OpenAI and AWS Bedrock
- [ ] Gemini prompt formatting optimized for legal case analysis
- [ ] Response normalization converts Gemini output to standard AIResponse format
- [ ] Proper handling of Gemini safety filters and content policies
- [ ] Token counting and usage tracking implemented
- [ ] Error handling covers Google Cloud specific exceptions

## Testing Strategy
- Unit tests: Prompt building, response mapping, safety filter handling
- Integration tests: Real Vertex AI API calls with legal document test cases
- Manual validation: Analysis quality comparison across all three providers

## System Stability
- Graceful handling of Google Cloud service outages or quota limits
- Proper timeout handling for Gemini processing requests
- Retry logic with exponential backoff for transient errors

### Gemini Prompt Implementation
```csharp
private GenerateContentRequest BuildGeminiRequest(string documentText, string caseContext)
{
    var content = new Content
    {
        Parts = {
            new Part
            {
                Text = $@"You are an AI legal assistant specializing in case analysis for public defenders.

Document Text:
{documentText}

Case Context:
{caseContext}

Please provide comprehensive legal case analysis including:
1. Case Viability Assessment (0-100%)
2. Key Legal Issues Identified
3. Potential Defenses and Arguments
4. Evidence Strength Evaluation
5. Recommended Next Steps

Provide objective, thorough analysis with confidence scores where appropriate."
            }
        },
        Role = "user"
    };

    return new GenerateContentRequest
    {
        Model = $"projects/{_options.ProjectId}/locations/{_options.Location}/publishers/google/models/{_options.ModelId}",
        Contents = { content },
        GenerationConfig = new GenerationConfig
        {
            MaxOutputTokens = _options.MaxTokens,
            Temperature = (float)_options.Temperature
        }
    };
}
```