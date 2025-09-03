# Task: Case List and Dashboard Page

## Overview
- **Parent Feature**: Case Management (CASE-004 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-case-model-crud.md: Case service and models required
- [ ] 002-login-registration-ui.md: Authentication required for case access

### External Dependencies
- Bootstrap 5 for table and card styling

## Implementation Details
### Files to Create/Modify
- `Components/Pages/Cases/Index.razor`: Case list page
- `Components/Shared/CaseCard.razor`: Individual case display component
- `Components/Shared/CaseStatusBadge.razor`: Status display component
- Update `Components/Layout/NavMenu.razor`: Add Cases navigation link
- `Models/ViewModels/CaseListViewModel.cs`: View model for case listing

### Code Patterns
- Follow existing page component patterns
- Use Blazor parameter binding for components
- Implement proper loading states and error handling

### API/Data Structures
```csharp
public class CaseListViewModel
{
    public IEnumerable<CaseSummary> Cases { get; set; } = new List<CaseSummary>();
    public int TotalCases { get; set; }
    public Dictionary<CaseStatus, int> StatusCounts { get; set; } = new();
}

public class CaseSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocumentCount { get; set; }
}
```

## Acceptance Criteria
- [ ] Case list page displays all user's cases in table/card format
- [ ] Cases show title, description, status, created date, document count
- [ ] Status badges with appropriate colors (New=blue, InReview=yellow, etc.)
- [ ] Empty state message when user has no cases
- [ ] Loading indicator while fetching cases
- [ ] Click on case navigates to case detail page
- [ ] Cases navigation link added to main menu
- [ ] Dashboard summary shows case counts by status
- [ ] Responsive design works on mobile devices

## Testing Strategy
- Manual validation: View case list as authenticated user
- Empty state testing: Test with user who has no cases
- Responsive testing: Verify mobile layout works
- Navigation testing: Verify links to case details work

## System Stability
- How this task maintains operational state: Provides case viewing without modifications
- Rollback strategy if needed: Remove case pages, remove navigation links
- Impact on existing functionality: None (adds new navigation and viewing capability)