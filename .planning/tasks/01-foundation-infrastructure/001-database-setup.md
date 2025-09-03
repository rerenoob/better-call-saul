# Task: Database Setup and Configuration

## Overview
- **Parent Feature**: Foundation Infrastructure (DEV-001 from 3_IMPLEMENTATION.md)
- **Complexity**: Low
- **Estimated Time**: 4 hours
- **Status**: Completed

## Dependencies
### Required Tasks
- None (This is the foundation task)

### External Dependencies
- SQL Server LocalDB must be available on development machine
- .NET 8 SDK installed

## Implementation Details
### Files to Create/Modify
- `appsettings.json`: Add connection string configuration
- `appsettings.Development.json`: Local development database settings
- `Program.cs`: Configure Entity Framework DbContext
- `Data/ApplicationDbContext.cs`: Create main database context
- `Models/BaseEntity.cs`: Create base entity with common properties

### Code Patterns
- Follow ASP.NET Core configuration patterns
- Use Entity Framework Code First migrations
- Implement soft delete pattern for auditing

### API/Data Structures
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class ApplicationDbContext : DbContext
{
    // Database sets will be added by subsequent tasks
}
```

## Acceptance Criteria
- [ ] SQL Server LocalDB connection string configured
- [ ] ApplicationDbContext created and registered in DI
- [ ] Database can be created via migration commands
- [ ] Connection test succeeds with `dotnet run`
- [ ] Base entity pattern established for future models

## Testing Strategy
- Manual validation: Verify database connection on startup
- Console logging: Confirm EF is connecting successfully
- Migration test: Run `dotnet ef database update` successfully

## System Stability
- How this task maintains operational state: Provides foundation for all data access
- Rollback strategy if needed: Remove connection string, unregister DbContext
- Impact on existing functionality: None (foundation task)