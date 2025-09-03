# Task: Case Entity and Basic CRUD Operations

## Overview
- **Parent Feature**: Case Management (CASE-004 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 5 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [ ] 001-database-setup.md: Database context required
- [ ] 002-text-extraction-service.md: Document entity relationship needed

### External Dependencies
- Entity Framework migrations

## Implementation Details
### Files to Create/Modify
- `Models/Case.cs`: Case entity model
- `Models/ViewModels/CaseViewModel.cs`: Case form view model
- Update `Data/ApplicationDbContext.cs`: Add Cases DbSet and relationships
- `Services/ICaseService.cs`: Case service interface
- `Services/CaseService.cs`: Case business logic implementation
- Database migration for Case entity and relationships

### Code Patterns
- Follow existing entity patterns with BaseEntity
- Use repository pattern through service layer
- Implement proper entity relationships (Case -> Documents, Case -> User)

### API/Data Structures
```csharp
public class Case : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.New;
    public DateTime? ClosedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public enum CaseStatus
{
    New = 0,
    InReview = 1,
    NeedsMoreInfo = 2,
    Completed = 3,
    Closed = 4
}

public interface ICaseService
{
    Task<IEnumerable<Case>> GetUserCasesAsync(string userId);
    Task<Case?> GetCaseByIdAsync(int caseId, string userId);
    Task<Case> CreateCaseAsync(CaseViewModel model, string userId);
    Task<bool> UpdateCaseAsync(int caseId, CaseViewModel model, string userId);
    Task<bool> DeleteCaseAsync(int caseId, string userId);
}
```

## Acceptance Criteria
- [ ] Case entity created with all required properties
- [ ] Database migration created and can be applied successfully
- [ ] Case service implements all CRUD operations
- [ ] Cases are properly associated with authenticated users
- [ ] Case status enum with appropriate values
- [ ] Proper navigation properties between Case, User, and Documents
- [ ] Service methods include proper authorization (users can only access their cases)
- [ ] Soft delete implemented for cases

## Testing Strategy
- Manual validation: Create, read, update, delete cases via service methods
- Authorization testing: Verify users cannot access other users' cases
- Relationship testing: Verify case-document associations work
- Migration testing: Apply and rollback database migration

## System Stability
- How this task maintains operational state: Adds case management foundation
- Rollback strategy if needed: Remove case service, drop Case table
- Impact on existing functionality: None (adds new functionality, ready for document association)