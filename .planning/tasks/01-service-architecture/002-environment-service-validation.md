# Task: Environment-Based Service Selection Validation

## Overview
- **Parent Feature**: AZURE-01 Service Architecture Simplification (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Low
- **Estimated Time**: 2 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-di-container-refactor.md: DI container must be refactored first

### External Dependencies
- Ability to run application in different environments
- Access to logging output for service validation

## Implementation Details
### Files to Create/Modify
- Create `BetterCallSaul.Tests/Integration/ServiceRegistrationTests.cs`: New test file for service resolution validation
- Modify existing integration test setup if needed

### Code Patterns
- Follow existing test patterns in `BetterCallSaul.Tests/` directory
- Use ASP.NET Core TestHost for integration testing
- Environment variable manipulation for testing

### Test Implementation
```csharp
[TestClass]
public class ServiceRegistrationTests
{
    [TestMethod]
    public void Development_Environment_RegistersMockServices()
    {
        // Test environment=Development resolves to mock services
    }

    [TestMethod]
    public void Production_Environment_RegistersAWSServices()
    {
        // Test environment=Production resolves to AWS services
    }
}
```

## Acceptance Criteria
- [ ] Integration test validates Development environment registers mock services
- [ ] Integration test validates Production environment registers AWS services
- [ ] Service interface contracts verified for both environments
- [ ] Application startup logging shows correct service selection
- [ ] All service dependencies resolve correctly in both environments
- [ ] No service resolution exceptions during application startup
- [ ] Test coverage for environment-based service registration

## Testing Strategy
- Unit tests: Service registration validation tests
- Integration tests: Full application startup in both environments
- Manual validation:
  1. Start app in Development mode, check logs for mock service registration
  2. Start app in Production mode, check logs for AWS service registration
  3. Verify API endpoints work with both service types

## System Stability
- How this task maintains operational state: Validates service registration works correctly
- Rollback strategy if needed: Fix service registration issues or revert DI changes
- Impact on existing functionality: No impact, purely validation task

## Notes
- This task ensures the DI refactoring works correctly before proceeding
- Logging statements should clearly indicate which services are registered
- Tests should be environment-agnostic and run in CI/CD pipeline