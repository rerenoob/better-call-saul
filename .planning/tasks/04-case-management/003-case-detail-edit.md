# Task: Case Detail and Edit Pages

## Overview
- **Parent Feature**: Case Management (CASE-004 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-case-model-crud.md: Case service required
- [ ] 002-case-list-page.md: Navigation from list page needed
- [ ] 001-file-upload-component.md: Document upload component needed

### External Dependencies
- Bootstrap 5 for form and layout styling

## Implementation Details
### Files to Create/Modify
- `Components/Pages/Cases/Details.razor`: Case detail view page
- `Components/Pages/Cases/Edit.razor`: Case edit form page
- `Components/Pages/Cases/Create.razor`: New case creation page
- `Components/Shared/CaseForm.razor`: Reusable case form component
- `Components/Shared/DocumentList.razor`: Display associated documents

### Code Patterns
- Follow existing Blazor form patterns
- Use EditForm component with validation
- Implement proper navigation between pages

### API/Data Structures
```csharp
public class CaseDetailViewModel
{
    public Case Case { get; set; } = null!;
    public IEnumerable<DocumentSummary> Documents { get; set; } = new List<DocumentSummary>();
    public bool CanEdit { get; set; }
}

public class DocumentSummary
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
}
```

## Acceptance Criteria
- [ ] Case detail page shows all case information and associated documents
- [ ] Edit button navigates to edit form (only for case owner)
- [ ] Case edit form pre-populates with existing data
- [ ] Form validation prevents empty title and description
- [ ] Status can be updated via dropdown
- [ ] Save button updates case and returns to detail view
- [ ] Cancel button returns to detail view without saving
- [ ] Create new case page with empty form
- [ ] Associated documents displayed with download links
- [ ] Proper error handling for unauthorized access
- [ ] Success messages after create/update operations

## Testing Strategy
- Manual validation: Create new case, edit existing case, view case details
- Authorization testing: Verify users cannot edit other users' cases
- Validation testing: Try to save case with empty required fields
- Navigation testing: Verify all page transitions work correctly

## System Stability
- How this task maintains operational state: Completes case management CRUD functionality
- Rollback strategy if needed: Remove edit/create pages, keep detail page read-only
- Impact on existing functionality: None (completes case management feature)