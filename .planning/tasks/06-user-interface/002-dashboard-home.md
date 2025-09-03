# Task: User Dashboard and Home Page

## Overview
- **Parent Feature**: User Interface (UI-006 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 4 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 002-case-list-page.md: Case data and services needed
- [ ] 001-responsive-layout.md: Layout components required

### External Dependencies
- Chart.js or similar for basic statistics visualization

## Implementation Details
### Files to Create/Modify
- Update `Components/Pages/Home.razor`: Transform into user dashboard
- `Components/Shared/DashboardCard.razor`: Reusable dashboard statistics card
- `Components/Shared/RecentCases.razor`: Display recent cases widget
- `Components/Shared/QuickActions.razor`: Quick action buttons widget
- `Services/IDashboardService.cs`: Dashboard data service

### Code Patterns
- Create widget-based dashboard layout
- Use cards and grid system for responsive dashboard
- Implement data aggregation for dashboard statistics

### API/Data Structures
```csharp
public class DashboardData
{
    public int TotalCases { get; set; }
    public int CasesThisMonth { get; set; }
    public Dictionary<CaseStatus, int> CasesByStatus { get; set; } = new();
    public List<CaseSummary> RecentCases { get; set; } = new();
    public int DocumentsProcessed { get; set; }
    public int AnalysesCompleted { get; set; }
}

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(string userId);
}
```

## Acceptance Criteria
- [ ] Dashboard shows key statistics (total cases, documents, analyses)
- [ ] Recent cases widget displays last 5 cases with quick links
- [ ] Case status distribution shown with visual indicators
- [ ] Quick action buttons for "New Case", "Upload Document", "View All Cases"
- [ ] Empty state for new users with onboarding guidance
- [ ] Dashboard data updates when user creates/modifies cases
- [ ] Mobile-responsive dashboard layout
- [ ] Loading states while fetching dashboard data

## Testing Strategy
- Manual validation: View dashboard with various user data states
- Empty state testing: Test with new user (no cases)
- Data accuracy: Verify statistics match actual case counts
- Responsive testing: Verify dashboard layout on mobile devices

## System Stability
- How this task maintains operational state: Improves home page without breaking navigation
- Rollback strategy if needed: Revert to original static home page
- Impact on existing functionality: Enhances user experience with personalized dashboard