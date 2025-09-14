# Task: Remove Azure-Specific Tests

## Overview
- **Parent Feature**: AZURE-06 Test Suite Updates (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 2 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 001-azure-service-implementations.md: Azure services removed
- [x] 002-azure-configuration-classes.md: Azure configuration removed

### External Dependencies
- Access to test project files
- Test runner for validation

## Implementation Details
### Files to Remove
- `BetterCallSaul.Tests/Services/AI/AzureOpenAIServiceTests_Simplified.cs`: Azure OpenAI service tests
- `BetterCallSaul.Tests/Services/FileProcessing/AzureBlobStorageServiceTests.cs`: Azure Blob Storage tests
- Any integration tests specifically testing Azure service functionality

### Files to Analyze for Azure References
- Search all test files for imports of Azure service classes
- Remove test methods that specifically test Azure service behavior
- Update test setup/teardown that configures Azure services

### Code Patterns
- Remove Azure service mocking and test setup
- Remove Azure configuration testing
- Ensure no Azure using statements remain in test files

### Test Categories to Remove
1. **Azure Service Unit Tests**: Direct testing of Azure service implementations
2. **Azure Integration Tests**: End-to-end tests using Azure services
3. **Azure Configuration Tests**: Tests validating Azure configuration binding

### Example Test Removal
```csharp
// Remove entire test classes like:
[TestClass]
public class AzureOpenAIServiceTests
{
    // All test methods in Azure-specific test classes
}

// Remove Azure-specific test methods from general test classes:
[TestMethod]
public void Should_Configure_Azure_Services() // Remove this
```

## Acceptance Criteria
- [ ] All Azure-specific test files deleted from test project
- [ ] No Azure service imports remain in any test files
- [ ] No test methods specifically testing Azure service functionality
- [ ] Test project builds successfully after Azure test removal
- [ ] Test runner executes remaining tests without Azure dependencies
- [ ] No Azure configuration setup code remains in test fixtures
- [ ] Test coverage reports exclude deleted Azure test files

## Testing Strategy
- Unit tests: Verify test project compiles after removal
- Integration tests: Ensure remaining tests run successfully
- Manual validation:
  1. Delete Azure test files and build test project
  2. Run full test suite and verify no Azure-related failures
  3. Check test coverage report for completeness

## System Stability
- How this task maintains operational state: Removes non-functional tests for deleted services
- Rollback strategy if needed: Restore Azure test files from git history
- Impact on existing functionality: No impact on application functionality, improves test suite maintainability

## Test Coverage Considerations
- Ensure overall test coverage percentage doesn't drop significantly
- Verify that mock service tests provide equivalent coverage
- Check that integration tests still validate core functionality

## Additional Cleanup
### Test Configuration Files
- Remove Azure service configuration from test appsettings
- Update test environment setup to not configure Azure services
- Clean up any test-specific Azure credential configuration

### Test Data and Fixtures
- Remove Azure-specific test data files
- Update test fixtures that relied on Azure service responses
- Ensure test databases/setup don't reference Azure services

## Notes
- This task is safe as it removes tests for non-existent services
- Focus on maintaining overall test quality and coverage
- Ensure remaining tests adequately cover the simplified architecture
- Consider if any Azure test patterns should be adapted for mock services