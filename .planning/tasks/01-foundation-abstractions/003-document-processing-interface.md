# Task: Create IDocumentProcessingService Interface and Response Models

## Overview
- **Parent Feature**: Phase 1 Foundation - Task 1.1 Create Service Abstraction Interfaces
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- None (foundation task, can run parallel with 001, 002)

### External Dependencies
- Access to existing AzureFormRecognizerService.cs and MockTextExtractionService.cs

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Interfaces/Services/IDocumentProcessingService.cs`: New generic document processing interface
- `BetterCallSaul.Core/Models/ServiceResponses/DocumentProcessingResult.cs`: Standardized text extraction result
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AzureFormRecognizerService.cs`: Modify to implement new interface
- `BetterCallSaul.Infrastructure/Services/FileProcessing/MockTextExtractionService.cs`: Modify to implement new interface

### Code Patterns
- Follow existing ITextExtractionService patterns
- Use standard confidence scoring (0-1 scale) across all providers
- Implement proper error handling for various document formats

## Acceptance Criteria
- [ ] `IDocumentProcessingService` interface includes ExtractTextAsync, ProcessDocumentAsync methods
- [ ] `DocumentProcessingResult` model includes Success, ExtractedText, ConfidenceScore, Pages, and Metadata
- [ ] Both AzureFormRecognizerService and MockTextExtractionService implement new interface
- [ ] Interface supports various document formats (PDF, DOC, images)
- [ ] Confidence score normalization across different providers
- [ ] Page-level text extraction and metadata support

## Testing Strategy
- Unit tests: Interface contract validation, confidence score normalization
- Integration tests: Document processing with various file formats
- Manual validation: Existing text extraction functionality works unchanged

## System Stability
- Current document processing workflows remain operational
- No changes to existing DocumentText database models
- Maintains backward compatibility with existing document processing APIs

### Interface Structure
```csharp
public interface IDocumentProcessingService
{
    Task<DocumentProcessingResult> ExtractTextAsync(string filePath, string fileName);
    Task<DocumentProcessingResult> ExtractTextFromBytesAsync(byte[] fileContent, string fileName);
    Task<bool> SupportsFileTypeAsync(string fileName);
    Task<DocumentText> ProcessDocumentAsync(string filePath, Guid documentId);
}
```