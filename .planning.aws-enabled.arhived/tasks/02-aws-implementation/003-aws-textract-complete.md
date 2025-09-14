# Task: Complete AWS Textract Document Processing Implementation

## Overview
- **Parent Feature**: AWS Migration - Document Processing
- **Complexity**: High
- **Estimated Time**: 14 hours
- **Status**: Not Started

**Note**: This combines Textract setup and document analysis tasks.

## Dependencies
### Required Tasks
- [ ] 01-foundation-abstractions/001-ai-service-interface: Basic interfaces complete
- [ ] 01-foundation-abstractions/003-configuration-and-di: AWS configuration ready

### External Dependencies
- AWS Textract service access
- IAM permissions for Textract operations

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Interfaces/Services/IDocumentProcessingService.cs`: Document processing interface
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSTextractService.cs`: Complete Textract implementation
- Add AWS Textract SDK packages
- Update existing Azure Form Recognizer service to implement interface

### AWS Services Used
- **Amazon Textract**: OCR and document analysis
- **Textract Analyze Document**: Synchronous text extraction
- **Textract Start/Get Document Analysis**: Asynchronous processing for large documents

## Acceptance Criteria
- [ ] `IDocumentProcessingService` interface covers essential OCR operations
- [ ] `AWSTextractService` implements all interface methods
- [ ] Text extraction accuracy comparable to Azure Form Recognizer
- [ ] Confidence scores normalized to 0-1 scale
- [ ] Support for PDF and image document types
- [ ] Async processing for large documents

## Key Implementation Points
### Interface Design
```csharp
public interface IDocumentProcessingService
{
    Task<TextExtractionResult> ExtractTextAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
    Task<TextExtractionResult> AnalyzeDocumentAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> IsDocumentProcessingCompleteAsync(string jobId, CancellationToken cancellationToken = default);
}

public class TextExtractionResult
{
    public bool Success { get; set; }
    public string ExtractedText { get; set; }
    public double ConfidenceScore { get; set; } // Normalized 0-1
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Textract Implementation Strategy
- **Small documents** (< 5MB, < 3000 pages): Synchronous processing
- **Large documents**: Asynchronous processing with job polling
- **Confidence normalization**: Convert Textract confidence (0-100) to standard scale (0-1)

### Document Type Support
- **PDF files**: Multi-page document analysis
- **Image files**: JPG, PNG document processing
- **Form documents**: Maintain compatibility with existing form processing workflows

## Testing Strategy
- Unit tests: Textract response parsing, confidence normalization
- Integration tests: Real document processing with various file types
- Performance tests: Processing time comparison with Azure Form Recognizer
- Quality tests: Text extraction accuracy validation

## System Stability
- Graceful handling of unsupported document formats
- Timeout and retry logic for long-running jobs
- Fallback to Azure Form Recognizer if Textract unavailable
- Clear error messages for processing failures