# NoSQL Data Layer Implementation - Architecture Decisions

**Created**: 2025-09-22
**Version**: 2.0
**Updated**: Simplified for fresh start application

## Decision 1: NoSQL Database Selection

### Context
Need to select a NoSQL database for storing legal documents, case analysis results, and unstructured legal research data. The current application runs on AWS infrastructure and stores complex nested objects as JSON in SQL Server.

### Options Considered
1. **AWS DocumentDB (MongoDB-compatible)**
2. **MongoDB Atlas (Cloud)**
3. **Azure Cosmos DB**
4. **Amazon DynamoDB**

### Chosen Solution: AWS DocumentDB

### Rationale
- **AWS Integration**: Seamless integration with existing AWS infrastructure (Bedrock, S3, Textract)
- **MongoDB Compatibility**: Familiar query syntax and development patterns
- **Managed Service**: Automatic scaling, backups, and maintenance
- **Cost Efficiency**: Better pricing than MongoDB Atlas for our expected workload
- **Security**: Built-in VPC support and AWS IAM integration
- **JSON Document Support**: Native storage for complex CaseAnalysis and DocumentText objects
- **Full-Text Search**: Built-in text search capabilities for legal documents

### Technical Impact
- Use MongoDB .NET driver for data access
- Implement repository pattern with async/await patterns
- Support for complex aggregation queries for analysis reporting
- Built-in indexing for fast document and case lookups

---

## Decision 2: Data Partitioning Strategy

### Context
Determine how to organize and partition data between SQL Server (user management) and DocumentDB (documents/analysis) while maintaining data relationships and query performance.

### Options Considered
1. **Microservices with separate databases**
2. **Hybrid approach with cross-database references**
3. **Event-driven eventual consistency**
4. **Dual-write with rollback capability**

### Chosen Solution: Hybrid Approach with Cross-Database References

### Rationale
- **Clear Separation**: SQL for structured user/auth data, NoSQL for unstructured document/analysis data
- **Referential Integrity**: Maintain UserId references in NoSQL documents for authorization
- **Query Flexibility**: Enable efficient queries within each database type
- **Simplified Implementation**: No migration complexity for fresh start
- **API Compatibility**: Maintain consistent API contracts through application-layer joins

### Data Distribution
**Remain in SQL Server:**
- User, Role, AuditLog entities (ASP.NET Identity)
- RegistrationCode entities
- Core Case metadata (CaseId, UserId, basic properties)

**Move to DocumentDB:**
- Document, DocumentText, DocumentAnnotation entities
- CaseAnalysis, EvidenceEvaluation, TimelineAnalysis entities
- LegalCase, CourtOpinion, LegalStatute entities
- CaseMatch, MatchingCriteria entities
- All entities with complex JSON metadata

### Implementation Pattern
```csharp
// SQL: User and basic case info
public class Case 
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaseNumber { get; set; }
    public string Title { get; set; }
    public CaseStatus Status { get; set; }
    // Basic metadata only
}

// NoSQL: Rich document and analysis data
public class CaseDocument 
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }  // Reference to SQL Case
    public Guid UserId { get; set; }  // Denormalized for security
    public DocumentMetadata Metadata { get; set; }
    public List<CaseAnalysis> Analyses { get; set; }
    // Complex nested objects
}
```

---

## Decision 3: Implementation Strategy

### Context
Implement NoSQL integration for new application with no existing data to migrate.

### Options Considered
1. **Direct Implementation**: Write directly to NoSQL for appropriate entities
2. **Hybrid Approach**: Maintain both databases with clear separation
3. **Eventual Migration**: Start with SQL, migrate later if needed

### Chosen Solution: Direct Implementation with Clear Separation

### Rationale
- **Simplified Architecture**: No complex migration or dual-write logic needed
- **Performance Optimized**: Each database handles appropriate data types
- **Reduced Complexity**: No cross-database transaction management
- **Faster Deployment**: No migration validation or rollback procedures required

### Data Distribution
**SQL Server (Existing):**
- User, Role, AuditLog entities (ASP.NET Identity)
- RegistrationCode entities
- Core Case metadata (CaseId, UserId, basic properties)

**DocumentDB (New):**
- Document, DocumentText, DocumentAnnotation entities
- CaseAnalysis, EvidenceEvaluation, TimelineAnalysis entities
- LegalCase, CourtOpinion, LegalStatute entities
- CaseMatch, MatchingCriteria entities
- All entities with complex JSON metadata

### Implementation Approach
- Direct writes to appropriate database based on entity type
- Clear separation of data responsibilities
- Simplified error handling and rollback
- Performance optimized for each database type

---

## Standard Patterns

### Repository Pattern
Implement consistent repository interfaces for both SQL and NoSQL data access:

```csharp
public interface IDocumentRepository
{
    Task<CaseDocument> GetByIdAsync(Guid id);
    Task<List<CaseDocument>> GetByCaseIdAsync(Guid caseId);
    Task<CaseDocument> CreateAsync(CaseDocument document);
    Task<CaseDocument> UpdateAsync(CaseDocument document);
    Task DeleteAsync(Guid id);
    Task<List<CaseDocument>> SearchAsync(DocumentSearchCriteria criteria);
}
```

### Dependency Injection
Maintain clean service registration with both SQL and NoSQL contexts:

```csharp
// SQL Services
services.AddDbContext<BetterCallSaulContext>(options => 
    options.UseSqlServer(connectionString));
services.AddScoped<IUserRepository, SqlUserRepository>();

// NoSQL Services  
services.AddSingleton<IMongoClient>(provider => 
    new MongoClient(documentDbConnectionString));
services.AddScoped<IDocumentRepository, MongoDocumentRepository>();
```

### Error Handling
Implement consistent error handling patterns for database operations:

```csharp
public class CaseService
{
    public async Task<CaseWithDocuments> GetCaseWithDocumentsAsync(Guid caseId)
    {
        // Get case from SQL
        var case = await _sqlCaseRepository.GetByIdAsync(caseId);
        if (case == null) throw new NotFoundException("Case not found");
        
        // Get documents from NoSQL
        var documents = await _documentRepository.GetByCaseIdAsync(caseId);
        
        return new CaseWithDocuments { Case = case, Documents = documents };
    }
}
```

## Technical Dependencies

- **MongoDB .NET Driver** v2.19+ for DocumentDB connectivity
- **AWS SDK for .NET** for DocumentDB cluster management
- **Entity Framework Core** 8.0+ for SQL Server operations
- **System.Text.Json** for consistent JSON serialization
- **Serilog** for structured logging across both databases

## Performance Considerations

- **Connection Pooling**: Optimize connections for both SQL and DocumentDB
- **Indexing Strategy**: Create compound indexes for common query patterns
- **Caching Layer**: Implement Redis caching for frequently accessed documents
- **Query Optimization**: Use aggregation pipelines for complex analysis queries
- **Bulk Operations**: Batch document operations for performance