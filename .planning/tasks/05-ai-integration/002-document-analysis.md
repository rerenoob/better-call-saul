# Task: AI Document Analysis Integration

## Overview
- **Parent Feature**: AI Integration (AI-005 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-azure-openai-setup.md: Azure OpenAI service required
- [ ] 002-text-extraction-service.md: Document text extraction needed
- [ ] 001-case-model-crud.md: Case entity for storing analysis results

### External Dependencies
- Valid Azure OpenAI deployment with GPT model

## Implementation Details
### Files to Create/Modify
- `Models/CaseAnalysis.cs`: Entity to store AI analysis results
- `Services/IDocumentAnalysisService.cs`: Document analysis orchestration service
- `Services/DocumentAnalysisService.cs`: Implementation combining text extraction and AI
- Update `Data/ApplicationDbContext.cs`: Add CaseAnalysis DbSet
- `Models/ViewModels/AnalysisResultViewModel.cs`: View model for displaying results

### Code Patterns
- Orchestrate text extraction + AI analysis in single service
- Store analysis results in database for future reference
- Implement proper error handling and fallback strategies

### API/Data Structures
```csharp
public class CaseAnalysis : BaseEntity
{
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string KeyPoints { get; set; } = string.Empty; // JSON array
    public double ConfidenceScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string AnalysisVersion { get; set; } = "1.0";
}

public interface IDocumentAnalysisService
{
    Task<CaseAnalysis> AnalyzeCaseDocumentsAsync(int caseId, string userId);
    Task<CaseAnalysis?> GetLatestAnalysisAsync(int caseId, string userId);
    Task<bool> HasAnalysisAsync(int caseId, string userId);
}

public class AnalysisResultViewModel
{
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public bool IsRecent => AnalyzedAt > DateTime.UtcNow.AddDays(-1);
}
```

## Acceptance Criteria
- [ ] Service can analyze all documents in a case and generate combined analysis
- [ ] Analysis results stored in database with timestamp
- [ ] Summary generation focuses on legal implications and key points
- [ ] Recommendation provides clear proceed/review guidance
- [ ] Confidence score indicates reliability of analysis
- [ ] Service handles cases with multiple documents
- [ ] Proper error handling when AI service is unavailable
- [ ] Analysis results are versioned for future improvements
- [ ] Users can only analyze their own cases

## Testing Strategy
- Manual validation: Analyze cases with different document types
- Multi-document testing: Test cases with multiple PDFs/DOCX files
- Error testing: Test when AI service is unavailable
- Authorization testing: Verify users cannot analyze other users' cases

## System Stability
- How this task maintains operational state: Adds AI analysis without breaking document processing
- Rollback strategy if needed: Remove analysis service, keep document processing working
- Impact on existing functionality: None (adds AI analysis capability to existing cases)