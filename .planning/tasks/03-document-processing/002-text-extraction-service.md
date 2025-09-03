# Task: Document Text Extraction Service

## Overview
- **Parent Feature**: Document Processing (DOC-003 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-file-upload-component.md: File upload system required
- [ ] 003-file-storage-setup.md: File storage service required

### External Dependencies
- iTextSharp NuGet package for PDF processing
- DocumentFormat.OpenXml NuGet package for DOCX processing

## Implementation Details
### Files to Create/Modify
- `Services/ITextExtractionService.cs`: Text extraction interface
- `Services/TextExtractionService.cs`: Implementation using iTextSharp and OpenXml
- `Models/DocumentContent.cs`: Extracted content model
- `Models/Document.cs`: Document entity for database
- Update `Data/ApplicationDbContext.cs`: Add Documents DbSet

### Code Patterns
- Follow existing service patterns in the project
- Use async/await for file processing operations
- Implement proper error handling for corrupted files

### API/Data Structures
```csharp
public interface ITextExtractionService
{
    Task<DocumentContent> ExtractTextAsync(string filePath, string fileName);
    bool IsSupported(string fileName);
}

public class DocumentContent
{
    public string ExtractedText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileType { get; set; } = string.Empty;
    public bool ExtractionSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
```

## Acceptance Criteria
- [ ] PDF text extraction working with iTextSharp
- [ ] DOCX text extraction working with OpenXml SDK
- [ ] Extracted text stored in database with document metadata
- [ ] Proper error handling for corrupted or unreadable files
- [ ] Page count detection for PDF files
- [ ] File type and size metadata captured
- [ ] Text extraction preserves basic formatting (paragraphs)
- [ ] Service registered in dependency injection
- [ ] Database migration created for Document entity

## Testing Strategy
- Manual validation: Upload various PDF and DOCX files
- Content verification: Verify extracted text matches document content
- Error testing: Test with corrupted files, password-protected PDFs
- Performance testing: Test with large documents (within 10MB limit)

## System Stability
- How this task maintains operational state: Adds document processing without breaking uploads
- Rollback strategy if needed: Remove text extraction service, drop Document table
- Impact on existing functionality: Enhances file upload with text extraction capability