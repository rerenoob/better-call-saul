# NoSQL Data Layer Migration - Testing Strategy

**Created**: 2025-09-22
**Version**: 1.0

## Testing Overview

### Testing Objectives
1. **Data Integrity**: Ensure 100% accurate migration from SQL to NoSQL
2. **Performance Validation**: Confirm 60% performance improvement targets
3. **API Compatibility**: Verify no breaking changes to existing APIs
4. **Cross-Database Consistency**: Validate referential integrity across databases
5. **System Reliability**: Ensure high availability during and after migration

### Testing Scope
- All NoSQL repository implementations
- Cross-database service operations
- Data migration and validation scripts
- API endpoints with hybrid data sources
- Performance benchmarks and load testing
- Security and authorization patterns

## Core Test Categories

### 1. Unit Testing
**Scope**: Individual components and repository methods  
**Coverage Target**: 90% code coverage  
**Framework**: xUnit with FluentAssertions  
**Environment**: In-memory MongoDB (Mongo2Go)

#### Key Unit Test Areas

**Repository Layer Tests**:
```csharp
[Fact]
public async Task CreateCaseDocumentAsync_ValidDocument_ReturnsCreatedDocument()
{
    // Arrange
    var document = new CaseDocument
    {
        CaseId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Documents = new List<DocumentInfo> { /* test data */ }
    };
    
    // Act
    var result = await _caseDocumentRepo.CreateAsync(document);
    
    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeEmpty();
    result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
}

[Fact]
public async Task SearchCaseDocumentsAsync_WithFilters_ReturnsMatchingDocuments()
{
    // Test complex query scenarios with various filters
}

[Fact]
public async Task GetByUserIdAsync_NonExistentUser_ReturnsEmptyList()
{
    // Test error handling and edge cases
}
```

**Service Layer Tests**:
```csharp
[Fact]
public async Task GetCaseWithAnalysisAsync_ValidCaseId_CombinesDataFromBothDatabases()
{
    // Arrange
    var caseId = Guid.NewGuid();
    var sqlCase = CreateTestCase(caseId);
    var nosqlDocument = CreateTestCaseDocument(caseId);
    
    _mockSqlRepo.Setup(r => r.GetByIdAsync(caseId)).ReturnsAsync(sqlCase);
    _mockNoSqlRepo.Setup(r => r.GetByIdAsync(caseId)).ReturnsAsync(nosqlDocument);
    
    // Act
    var result = await _caseService.GetCaseWithAnalysisAsync(caseId);
    
    // Assert
    result.Case.Should().BeEquivalentTo(sqlCase);
    result.Documents.Should().HaveCount(nosqlDocument.Documents.Count);
    result.Analyses.Should().HaveCount(nosqlDocument.Analyses.Count);
}
```

**Data Model Tests**:
```csharp
[Fact]
public void CaseDocument_Serialization_PreservesComplexObjects()
{
    // Test JSON serialization/deserialization of complex nested objects
}

[Fact]
public void DocumentValidation_InvalidDocument_ThrowsValidationException()
{
    // Test model validation rules
}
```

### 2. Integration Testing
**Scope**: Cross-component interactions and database operations  
**Coverage Target**: All critical user workflows  
**Framework**: xUnit with TestContainers for real MongoDB  
**Environment**: Docker containers with real databases

#### Key Integration Test Areas

**Cross-Database Operations**:
```csharp
[Fact]
public async Task CreateCaseWithDocuments_EndToEnd_WorksAcrossBothDatabases()
{
    // Test complete workflow from case creation to document storage
    var userId = await CreateTestUserInSql();
    var caseData = new CreateCaseRequest { /* test data */ };
    
    var result = await _caseController.CreateCaseAsync(caseData);
    
    // Verify SQL data
    var sqlCase = await _sqlContext.Cases.FirstOrDefaultAsync(c => c.Id == result.Id);
    sqlCase.Should().NotBeNull();
    
    // Verify NoSQL data
    var nosqlDocument = await _nosqlRepo.GetByIdAsync(result.Id);
    nosqlDocument.Should().NotBeNull();
    nosqlDocument.UserId.Should().Be(userId);
}

[Fact]
public async Task DocumentUpload_WithAnalysis_CreatesCompleteDocumentStructure()
{
    // Test document upload, text extraction, and analysis pipeline
}

[Fact]
public async Task UserDeletion_CascadesToNoSQLDocuments_MaintainsDataIntegrity()
{
    // Test referential integrity across databases
}
```

**API Integration Tests**:
```csharp
[Fact]
public async Task GetCaseAnalysis_ReturnsIdenticalStructure_BeforeAndAfterMigration()
{
    // Ensure API compatibility during migration
}

[Fact]
public async Task SearchDocuments_PerformsBetterThanSQLVersion_WithSameResults()
{
    // Performance and accuracy validation
}
```

### 3. Data Migration Testing
**Scope**: Migration scripts and data validation  
**Coverage Target**: All entities and edge cases  
**Framework**: Custom migration test framework  
**Environment**: Production data copies

#### Migration Test Scenarios

**Data Accuracy Tests**:
```csharp
[Fact]
public async Task MigrateCaseAnalysis_ComplexNestedObjects_PreservesAllData()
{
    // Arrange
    var originalAnalysis = CreateComplexCaseAnalysis();
    await _sqlContext.CaseAnalyses.AddAsync(originalAnalysis);
    await _sqlContext.SaveChangesAsync();
    
    // Act
    await _migrationService.MigrateCaseAnalysisAsync(originalAnalysis.Id);
    
    // Assert
    var migratedDocument = await _nosqlRepo.GetAnalysisAsync(originalAnalysis.Id);
    ValidateCompleteDataMigration(originalAnalysis, migratedDocument);
}

[Fact]
public async Task MigrateDocumentText_LargeTextContent_HandlesCorrectly()
{
    // Test migration of large text documents (>1MB)
}

[Fact]
public async Task MigrationRollback_FailedMigration_RestoresOriginalState()
{
    // Test rollback capabilities
}
```

**Data Consistency Tests**:
```csharp
[Fact]
public async Task ValidateDataConsistency_PostMigration_AllReferencesValid()
{
    // Run comprehensive data validation
    var inconsistencies = await _validationService.ValidateAllDataAsync();
    inconsistencies.Should().BeEmpty();
}

[Fact]
public async Task CrossDatabaseReferentialIntegrity_NoOrphanedRecords()
{
    // Ensure no orphaned documents without valid user references
}
```

### 4. Performance Testing
**Scope**: Query performance and system load  
**Coverage Target**: All major query patterns  
**Framework**: NBomber for load testing  
**Environment**: Production-like infrastructure

#### Performance Test Scenarios

**Query Performance Tests**:
```csharp
[Fact]
public async Task DocumentListQuery_NoSQL_Performs60PercentFaster()
{
    // Baseline: Current SQL query performance
    var sqlStopwatch = Stopwatch.StartNew();
    var sqlResults = await _sqlDocumentRepo.GetDocumentsByUserAsync(userId);
    sqlStopwatch.Stop();
    
    // Target: NoSQL query performance
    var nosqlStopwatch = Stopwatch.StartNew();
    var nosqlResults = await _nosqlDocumentRepo.GetDocumentsByUserAsync(userId);
    nosqlStopwatch.Stop();
    
    // Assert 60% improvement
    var improvementRatio = (double)sqlStopwatch.ElapsedMilliseconds / nosqlStopwatch.ElapsedMilliseconds;
    improvementRatio.Should().BeGreaterThan(1.6); // 60% improvement
}

[Fact]
public async Task ComplexAnalysisQuery_AggregationPipeline_PerformsWithinTargets()
{
    // Test complex aggregation queries for reporting
}
```

**Load Testing**:
```csharp
var scenario = Scenario.Create("document_search", async context =>
{
    var searchCriteria = GenerateRandomSearchCriteria();
    var response = await _httpClient.PostAsync("/api/documents/search", 
        JsonContent.Create(searchCriteria));
    
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(5))
);

var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .Run();

// Assert performance targets
stats.AllOkCount.Should().BeGreaterThan(2700); // 90% success rate
stats.ScenarioStats[0].Ok.Response.Mean.Should().BeLessThan(500); // <500ms mean response
```

### 5. Security Testing
**Scope**: Authorization and data access controls  
**Coverage Target**: All security boundaries  
**Framework**: Custom security test framework  
**Environment**: Isolated test environment

#### Security Test Areas

**Authorization Tests**:
```csharp
[Fact]
public async Task GetCaseDocument_UnauthorizedUser_DeniesAccess()
{
    // Test that users can only access their own documents
}

[Fact]
public async Task CrossDatabaseAuthorization_ConsistentAcrossServices()
{
    // Ensure authorization works consistently across SQL and NoSQL
}
```

**Data Security Tests**:
```csharp
[Fact]
public async Task SensitiveDataMigration_EncryptedCorrectly()
{
    // Test encryption of sensitive legal data
}

[Fact]
public async Task AuditLogging_CapturesAllNoSQLOperations()
{
    // Ensure audit logging works for NoSQL operations
}
```

## Automated vs Manual Testing

### Automated Testing (80% of effort)
- **Unit Tests**: 100% automated with CI/CD integration
- **Integration Tests**: Automated with TestContainers
- **Performance Tests**: Automated benchmarking with alerts
- **Data Migration**: Automated validation scripts
- **API Tests**: Automated contract testing

### Manual Testing (20% of effort)
- **User Acceptance Testing**: Manual workflow validation
- **Security Penetration Testing**: Manual security assessment
- **Disaster Recovery**: Manual backup/restore procedures
- **Production Monitoring**: Manual validation of metrics

## Testing Tools and Frameworks

### Primary Testing Stack
- **xUnit**: Primary testing framework for .NET
- **FluentAssertions**: Readable assertion library
- **Moq**: Mocking framework for dependencies
- **TestContainers**: Docker-based integration testing
- **Mongo2Go**: In-memory MongoDB for unit tests
- **NBomber**: Load testing and performance validation
- **SpecFlow**: Behavior-driven development (if needed)

### Supporting Tools
- **MongoDB Compass**: Database inspection and query testing
- **Postman/Newman**: API testing and automation
- **Azure DevOps**: Test case management and reporting
- **SonarQube**: Code coverage and quality analysis
- **Application Insights**: Performance monitoring

## Critical Test Scenarios

### Edge Cases and Error Conditions
1. **Large Document Migration**: Documents >10MB with complex metadata
2. **Concurrent Operations**: Multiple users modifying same case simultaneously
3. **Network Failures**: DocumentDB connectivity issues during operations
4. **Data Corruption**: Handling corrupted JSON in migrated documents
5. **Memory Limits**: Large result sets and pagination handling

### Business Logic Validation
1. **Case Analysis Workflow**: Complete AI analysis pipeline with NoSQL storage
2. **Document Search**: Full-text search across legal documents
3. **User Deletion**: Proper cascading of user data across databases
4. **Audit Compliance**: Legal audit trail requirements
5. **Performance Benchmarks**: Real-world usage patterns and loads

## Testing Environment Setup

### Local Development Testing
```yaml
version: '3.8'
services:
  mongodb:
    image: mongo:6.0
    environment:
      MONGO_INITDB_ROOT_USERNAME: testuser
      MONGO_INITDB_ROOT_PASSWORD: testpass
    ports:
      - "27017:27017"
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: TestPassword123!
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"
```

### CI/CD Integration
```yaml
# Azure DevOps Pipeline
steps:
- task: DockerCompose@0
  displayName: 'Start Test Dependencies'
  inputs:
    dockerComposeFile: 'docker-compose.test.yml'
    action: 'Run services'

- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration Release --logger trx --collect:"XPlat Code Coverage"'

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: '**/*IntegrationTests.csproj'
    arguments: '--configuration Release --logger trx'
```

## Success Criteria

### Test Coverage Targets
- **Unit Test Coverage**: ≥90% for new NoSQL code
- **Integration Test Coverage**: 100% of critical user workflows
- **Performance Test Coverage**: All major query patterns validated
- **Security Test Coverage**: All authorization boundaries tested

### Quality Gates
- **Zero Critical Bugs**: No high-severity defects in production
- **Performance Targets**: 60% improvement in document query performance
- **Data Integrity**: 100% migration accuracy validated
- **API Compatibility**: Zero breaking changes to existing contracts
- **Security Compliance**: All security tests passing

### Acceptance Criteria
- All automated tests passing in CI/CD pipeline
- Manual user acceptance testing completed successfully
- Performance benchmarks meeting or exceeding targets
- Security assessment completed with no critical findings
- Production deployment tested with rollback procedures validated