# Task: Implement AWS Textract Document Text Extraction

## Overview
- **Parent Feature**: Phase 2 AWS Implementation - Task 2.3 AWS Textract Document Processing Implementation
- **Complexity**: High
- **Estimated Time**: 8 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 02-aws-implementation/007-aws-textract-setup: Textract infrastructure setup completed

### External Dependencies
- AWS Textract service with document analysis permissions
- Understanding of Textract response structure for legal documents

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSTextractService.cs`: Implement text extraction methods
- `BetterCallSaul.Infrastructure/Helpers/TextractResponseProcessor.cs`: Response processing utilities
- `BetterCallSaul.Infrastructure/Mappers/TextractResultMapper.cs`: Response normalization
- `BetterCallSaul.Tests/Services/FileProcessing/TextractExtractionTests.cs`: Text extraction tests

### Code Patterns
- Follow existing Azure Form Recognizer extraction patterns
- Use Textract SDK async patterns with proper polling for long operations
- Implement confidence score normalization to 0-1 scale

## Acceptance Criteria
- [ ] ExtractTextAsync successfully extracts text from various document formats (PDF, images)
- [ ] Both synchronous (DetectDocumentText) and asynchronous (StartDocumentTextDetection) processing
- [ ] Confidence score normalization from Textract to standard 0-1 scale
- [ ] Response format matches existing DocumentProcessingResult structure
- [ ] Page-level text extraction with proper ordering
- [ ] Error handling for Textract-specific exceptions and limitations

## Testing Strategy
- Unit tests: Text extraction logic, confidence scoring, response mapping
- Integration tests: Real Textract API calls with various document types
- Manual validation: Text extraction quality comparable to Azure Form Recognizer

## System Stability
- Proper handling of Textract processing limits and throttling
- Async operation polling with timeout and cancellation support
- Resource cleanup for failed or cancelled operations

### Textract Implementation Structure
```csharp
public async Task<DocumentProcessingResult> ExtractTextAsync(string filePath, string fileName)
{
    var startTime = DateTime.UtcNow;

    try
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath);

        // Use sync processing for smaller documents, async for larger ones
        if (fileBytes.Length < 5 * 1024 * 1024) // 5MB threshold
        {
            return await ProcessSynchronouslyAsync(fileBytes, fileName, startTime);
        }
        else
        {
            return await ProcessAsynchronouslyAsync(fileBytes, fileName, startTime);
        }
    }
    catch (AmazonTextractException ex)
    {
        return new DocumentProcessingResult
        {
            Success = false,
            ErrorMessage = $"Textract error: {ex.Message}",
            FileName = fileName,
            ProcessingTime = DateTime.UtcNow - startTime
        };
    }
}
```