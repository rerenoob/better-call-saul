# Task: Database Schema Design and Entity Framework Setup

## Overview
- **Parent Feature**: IMPL-001 Backend Infrastructure and API Setup
- **Complexity**: Medium
- **Estimated Time**: 6 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-dotnet-project-setup.md: Project foundation needed

### External Dependencies
- SQL Server or Azure SQL Database
- Entity Framework Core 8.0

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Models/User.cs`: User entity model
- `BetterCallSaul.Core/Models/Case.cs`: Case entity model  
- `BetterCallSaul.Core/Models/Document.cs`: Document entity model
- `BetterCallSaul.Core/Models/AuditLog.cs`: Audit logging entity
- `BetterCallSaul.Infrastructure/Data/BetterCallSaulContext.cs`: EF DbContext
- `BetterCallSaul.Infrastructure/Data/Configurations/`: Entity configurations
- `BetterCallSaul.API/Program.cs`: Add EF registration

### Code Patterns
- Follow Entity Framework Core conventions
- Use Fluent API for complex entity relationships
- Implement soft delete pattern for sensitive data

## Acceptance Criteria
- [ ] Database schema supports all required entities
- [ ] Entity relationships properly defined with navigation properties
- [ ] Data annotations and Fluent API configurations applied
- [ ] Audit logging captures created/modified timestamps and user
- [ ] Database migrations generated and can be applied successfully
- [ ] All sensitive data fields encrypted at rest
- [ ] Database connection string configuration working

## Testing Strategy
- Unit tests: Entity validation and relationships
- Integration tests: Database operations through DbContext
- Manual validation: Run migrations and verify schema in database

## System Stability
- Database changes require careful migration planning
- Include rollback migration scripts
- Test migrations against copy of production data structure

## Notes
- Design for GDPR compliance and data retention policies
- Consider indexing strategy for query performance
- Implement database-level constraints for data integrity