# Task: Environment-Based Service Integration Testing

## Overview
- **Parent Feature**: AZURE-06 Test Suite Updates (`.planning/3_IMPLEMENTATION.md`)
- **Complexity**: Medium
- **Estimated Time**: 3 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 002-environment-service-validation.md: Basic environment service validation complete
- [x] 002-mock-service-tests.md: Mock service tests implemented

### External Dependencies
- Both development and production service implementations available
- Test framework capability to manipulate environment variables

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Tests/Integration/EnvironmentServiceIntegrationTests.cs`: New integration test suite
- Update existing integration test setup to support environment switching

### Code Patterns
- Use ASP.NET Core TestHost for integration testing
- Manipulate ASPNETCORE_ENVIRONMENT for testing different configurations
- Follow existing integration test patterns in test project
- Mock AWS credentials for production environment testing

### Test Categories

#### Development Environment Tests
```csharp
[TestClass]
public class DevelopmentEnvironmentIntegrationTests
{
    [TestMethod]
    public async Task Development_Environment_RegistersMockServices()
    [TestMethod]
    public async Task Development_FileUpload_UsesLocalStorage()
    [TestMethod]
    public async Task Development_AIAnalysis_UsesMockService()
    [TestMethod]
    public async Task Development_TextExtraction_UsesMockService()
}
```

#### Production Environment Tests (with mocked AWS)
```csharp
[TestClass]
public class ProductionEnvironmentIntegrationTests
{
    [TestMethod]
    public async Task Production_Environment_RegistersAWSServices()
    [TestMethod]
    public async Task Production_MissingAWSConfig_FailsGracefully()
    [TestMethod]
    public async Task Production_ValidAWSConfig_StartsSuccessfully()
}
```

#### Cross-Environment Compatibility Tests
```csharp
[TestClass]
public class CrossEnvironmentCompatibilityTests
{
    [TestMethod]
    public async Task APIContract_ConsistentAcrossEnvironments()
    [TestMethod]
    public async Task ResponseModels_IdenticalStructure()
    [TestMethod]
    public async Task ErrorHandling_ConsistentBehavior()
}
```

## Test Environment Setup
### Environment Variable Manipulation
```csharp
[TestInitialize]
public void SetupEnvironment()
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    // or "Production" for production tests
}
```

### AWS Configuration Mocking
For production environment tests, provide mock AWS configuration:
```csharp
var mockConfig = new Dictionary<string, string>
{
    {"AWS:Bedrock:Region", "us-east-1"},
    {"AWS:S3:BucketName", "test-bucket"},
    {"AWS_ACCESS_KEY_ID", "mock-access-key"},
    {"AWS_SECRET_ACCESS_KEY", "mock-secret-key"}
};
```

## Acceptance Criteria
- [ ] Integration tests validate correct service resolution for each environment
- [ ] Development environment tests confirm mock service usage
- [ ] Production environment tests validate AWS service registration (with mocked credentials)
- [ ] API response consistency verified across environments
- [ ] Error handling behavior consistent between environments
- [ ] Configuration validation tests for production environment
- [ ] Service interface contracts verified for both environments
- [ ] Performance characteristics tested for both service types

## Testing Strategy
- Integration tests: Full application context with different environment configurations
- Contract tests: Verify API responses maintain same structure across environments
- Manual validation:
  1. Run integration tests in both Development and Production test modes
  2. Verify service resolution logs show correct service types
  3. Test API endpoints behave consistently across environments

## System Stability
- How this task maintains operational state: Validates environment switching works correctly
- Rollback strategy if needed: Fix service registration or environment detection issues
- Impact on existing functionality: Ensures reliable environment-based service selection

## Test Scenarios

### Service Resolution Validation
- Verify IStorageService resolves to LocalFileStorageService in development
- Verify IAIService resolves to MockAIService in development
- Verify AWS services resolve correctly in production (with mock config)
- Validate service lifetime and dependency injection work correctly

### API Endpoint Consistency
- File upload endpoints work with both storage implementations
- AI analysis endpoints return consistent response formats
- Error responses maintain same structure across environments
- HTTP status codes consistent between service implementations

### Configuration Handling
- Development environment starts without AWS configuration
- Production environment validates required AWS configuration
- Missing configuration produces clear, actionable error messages
- Environment variable overrides work correctly

## Performance Validation
- Mock services provide reasonable response times for development
- Service resolution time acceptable in both environments
- Memory usage reasonable with both service implementations
- No significant startup time differences between environments

## Notes
- These tests ensure the environment-based service selection works reliably
- Mock AWS credentials prevent actual AWS service calls during testing
- Tests should run in CI/CD pipeline to catch environment configuration issues
- Focus on validating the service switching mechanism rather than individual service functionality