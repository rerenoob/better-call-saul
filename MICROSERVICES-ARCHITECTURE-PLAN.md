# Microservices Architecture Plan: User Management + Document/Case Management Separation

## Current Architecture Analysis

The application currently uses a monolithic architecture with:
- **User Management**: ASP.NET Core Identity with PostgreSQL/SQLite
- **Case Management**: Entity Framework with complex relationships
- **Document Management**: File storage with metadata in SQL
- **AI Analysis**: Integrated case analysis services

### Key Dependencies Identified:
- Cases belong to Users (foreign key relationship)
- Documents belong to Cases (foreign key relationship)
- CaseAnalysis links Cases and Documents
- All entities share the same database context

## Proposed Microservices Architecture

### 1. User Management Service
**Technology Stack**: .NET 8 Web API + PostgreSQL
**Responsibilities**:
- User authentication & authorization (JWT)
- User profiles and lawyer-specific data
- Registration code management
- Audit logging for user actions

**Database Schema**:
```sql
-- Users table (ASP.NET Core Identity)
Users (Id, Email, FirstName, LastName, BarNumber, LawFirm, etc.)
Roles (Id, Name)
UserRoles (UserId, RoleId)
RegistrationCodes (Id, Code, CreatedBy, UsedBy, etc.)
AuditLogs (Id, UserId, Action, Timestamp, etc.)
```

**API Endpoints**:
```
POST /auth/login
POST /auth/register
POST /auth/refresh-token
GET /users/{id}
PUT /users/{id}
GET /users/{id}/audit-logs
```

### 2. Document & Case Management Service
**Technology Stack**: .NET 8 Web API + MongoDB (AWS DocumentDB)
**Responsibilities**:
- Case lifecycle management
- Document storage and metadata
- AI analysis results
- Case-document relationships
- Legal research data

**NoSQL Document Structure**:

```javascript
// Cases Collection
{
  "_id": ObjectId("..."),
  "caseNumber": "CASE-2024-001",
  "title": "State vs. Smith",
  "description": "Criminal defense case...",
  "status": "Active",
  "type": "Criminal",
  "priority": "High",
  "court": "Superior Court",
  "judge": "Hon. Jane Doe",
  "dates": {
    "filed": ISODate("2024-01-15"),
    "hearing": ISODate("2024-03-01"),
    "trial": ISODate("2024-06-15")
  },
  "probability": {
    "success": 0.75,
    "estimated_value": 50000
  },
  "userId": "guid-from-user-service",
  "createdAt": ISODate("2024-01-10"),
  "updatedAt": ISODate("2024-01-20"),
  "isDeleted": false,
  "metadata": {}
}

// Documents Collection
{
  "_id": ObjectId("..."),
  "caseId": ObjectId("case-id"),
  "fileName": "evidence-photo-1.jpg",
  "originalFileName": "IMG_20240115_143022.jpg",
  "fileType": "image/jpeg",
  "fileSize": 2048576,
  "storagePath": "s3://bucket/case-123/documents/...",
  "documentType": "Evidence",
  "status": "Processed",
  "description": "Crime scene photo",
  "isProcessed": true,
  "processedAt": ISODate("2024-01-15T14:35:00Z"),
  "uploadedBy": "guid-from-user-service",
  "extractedText": {
    "content": "Extracted text content...",
    "confidence": 0.95,
    "pages": [...],
    "metadata": {}
  },
  "createdAt": ISODate("2024-01-15"),
  "updatedAt": ISODate("2024-01-15"),
  "isDeleted": false,
  "metadata": {}
}

// Case Analysis Collection
{
  "_id": ObjectId("..."),
  "caseId": ObjectId("case-id"),
  "documentId": ObjectId("document-id"),
  "analysisText": "Comprehensive legal analysis...",
  "scores": {
    "viability": 85.5,
    "confidence": 0.92
  },
  "legalIssues": [
    "Fourth Amendment violation",
    "Chain of custody issues"
  ],
  "potentialDefenses": [
    "Illegal search and seizure",
    "Suppression of evidence"
  ],
  "evidenceEvaluation": {
    "strengthScore": 0.7,
    "strongEvidence": ["Witness testimony", "Video footage"],
    "weakEvidence": ["Circumstantial evidence"],
    "evidenceGaps": ["DNA analysis pending"],
    "additionalNeeded": ["Expert witness testimony"]
  },
  "timelineAnalysis": {
    "events": [
      {
        "date": ISODate("2024-01-01"),
        "description": "Incident occurred",
        "significance": "Primary event",
        "confidence": 0.95
      }
    ],
    "chronologicalIssues": [],
    "criticalTimePoints": []
  },
  "recommendations": [
    {
      "action": "File motion to suppress evidence",
      "rationale": "Fourth Amendment violation",
      "priority": "Critical",
      "impactScore": 0.9
    }
  ],
  "status": "Completed",
  "createdAt": ISODate("2024-01-15"),
  "completedAt": ISODate("2024-01-15T15:30:00Z"),
  "processingTime": "PT30M",
  "metadata": {}
}

// Legal Research Collection
{
  "_id": ObjectId("..."),
  "caseId": ObjectId("case-id"),
  "searchQuery": "Fourth Amendment vehicle search",
  "searchResults": {
    "courtListener": [...],
    "justia": [...]
  },
  "relevantCases": [
    {
      "caseTitle": "Terry v. Ohio",
      "citation": "392 U.S. 1 (1968)",
      "relevanceScore": 0.85,
      "summary": "..."
    }
  ],
  "createdAt": ISODate("2024-01-15"),
  "metadata": {}
}
```

**API Endpoints**:
```
GET /cases?userId={guid}
POST /cases
PUT /cases/{id}
DELETE /cases/{id}

GET /cases/{caseId}/documents
POST /cases/{caseId}/documents
PUT /documents/{id}
DELETE /documents/{id}

GET /cases/{caseId}/analysis
POST /cases/{caseId}/analysis
GET /analysis/{id}

GET /cases/{caseId}/research
POST /cases/{caseId}/research
```

### 3. API Gateway & Service Communication

**AWS API Gateway** with:
- Authentication via User Management Service
- Request routing to appropriate services
- Rate limiting and throttling
- Request/response transformation

**Service Communication**:
- **Synchronous**: HTTP REST APIs via API Gateway
- **Asynchronous**: AWS SQS/EventBridge for events
- **Data Consistency**: Eventual consistency with event sourcing

**Authentication Flow**:
1. Client authenticates with User Management Service
2. Receives JWT token with user claims
3. API Gateway validates JWT for all requests
4. Services receive validated user context in headers

### 4. Data Migration Strategy

**Phase 1: Parallel Running**
```sql
-- Create user context table in Document Service
CREATE TABLE user_contexts (
  user_id UUID PRIMARY KEY,
  email VARCHAR(255),
  full_name VARCHAR(255),
  bar_number VARCHAR(50),
  law_firm VARCHAR(255),
  last_sync TIMESTAMP,
  is_active BOOLEAN
);
```

**Phase 2: Data Migration Script**
```csharp
public class DataMigrationService
{
    public async Task MigrateToMicroservices()
    {
        // 1. Migrate users to User Management Service
        var users = await _oldContext.Users.ToListAsync();
        foreach (var user in users)
        {
            await _userService.CreateUserAsync(user);
        }

        // 2. Migrate cases to MongoDB
        var cases = await _oldContext.Cases.Include(c => c.Documents).ToListAsync();
        foreach (var case in cases)
        {
            var caseDocument = new CaseDocument
            {
                CaseNumber = case.CaseNumber,
                Title = case.Title,
                UserId = case.UserId.ToString(),
                // ... map all properties
            };
            await _caseService.CreateCaseAsync(caseDocument);

            // 3. Migrate documents
            foreach (var doc in case.Documents)
            {
                var docDocument = new DocumentDocument
                {
                    CaseId = caseDocument.Id,
                    FileName = doc.FileName,
                    // ... map all properties
                };
                await _documentService.CreateDocumentAsync(docDocument);
            }
        }

        // 4. Migrate analysis data
        var analyses = await _oldContext.CaseAnalyses.ToListAsync();
        foreach (var analysis in analyses)
        {
            await _analysisService.CreateAnalysisAsync(analysis);
        }
    }
}
```

### 5. AWS Infrastructure Updates

**User Management Service**:
- AWS RDS PostgreSQL (existing)
- Elastic Beanstalk deployment
- Parameter Store for secrets

**Document & Case Management Service**:
- **AWS DocumentDB** (MongoDB-compatible)
  - Instance: `db.t3.medium`
  - Storage: 100GB with auto-scaling
  - Backup: Automated with 7-day retention
- **AWS S3** for file storage (existing)
- **AWS Lambda** for background processing
- **AWS SQS** for async messaging

**API Gateway**:
- AWS API Gateway with custom domain
- Lambda authorizers for JWT validation
- CloudWatch for monitoring and logging

### 6. Implementation Roadmap

**Week 1-2: Infrastructure Setup**
```bash
# Create DocumentDB cluster
aws docdb create-db-cluster \
  --db-cluster-identifier bettercallsaul-docdb \
  --engine docdb \
  --master-username admin \
  --master-user-password SecurePassword123!

# Create DocumentDB instance
aws docdb create-db-instance \
  --db-instance-identifier bettercallsaul-docdb-instance \
  --db-instance-class db.t3.medium \
  --db-cluster-identifier bettercallsaul-docdb \
  --engine docdb
```

**Week 3-4: User Management Service**
- Extract user-related code into separate service
- Set up PostgreSQL database for users only
- Implement JWT-based authentication
- Deploy to Elastic Beanstalk

**Week 5-6: Document/Case Management Service**
- Create new .NET service with MongoDB driver
- Implement document schemas and repositories
- Set up API endpoints for cases and documents
- Deploy to Elastic Beanstalk

**Week 7-8: API Gateway & Integration**
- Configure AWS API Gateway
- Set up service-to-service communication
- Implement async messaging with SQS
- Update frontend to use API Gateway

**Week 9-10: Data Migration & Testing**
- Run data migration scripts
- Parallel testing of both systems
- Performance optimization
- Security audit

**Week 11-12: Go-Live & Monitoring**
- Switch DNS to new API Gateway
- Monitor performance and errors
- Fine-tune based on production usage
- Decommission old monolithic service

### 7. Benefits of This Architecture

**Scalability**:
- Independent scaling of user management vs. document processing
- MongoDB handles complex case data and large documents efficiently
- S3 provides unlimited file storage

**Performance**:
- NoSQL optimized for document-heavy workloads
- Reduced database joins and complex queries
- Better caching strategies per service

**Development**:
- Teams can work independently on each service
- Different deployment cycles
- Technology flexibility (SQL for users, NoSQL for documents)

**Maintenance**:
- Easier to debug and troubleshoot isolated services
- Independent backup and recovery strategies
- Cleaner separation of concerns

### 8. Cost Analysis

**Additional Costs**:
- AWS DocumentDB: ~$65/month (db.t3.medium)
- API Gateway: ~$3.50 per million requests
- Additional Elastic Beanstalk environment: ~$8.50/month

**Cost Savings**:
- More efficient queries (NoSQL for documents)
- Better resource utilization per service
- Reduced database size for user management

**Total Additional Cost**: ~$75-100/month for improved architecture

### 9. Security Considerations

**Authentication & Authorization**:
- JWT tokens issued by User Management Service
- API Gateway validates all requests
- Service-to-service authentication via IAM roles
- Encrypted communication between services

**Data Protection**:
- PostgreSQL encryption at rest for user data
- DocumentDB encryption at rest for case data
- S3 encryption for document files
- VPC isolation between services

**Compliance**:
- Audit logging in both services
- GDPR compliance for user data
- Legal industry data protection requirements
- Regular security assessments

### 10. Monitoring & Observability

**Application Monitoring**:
- CloudWatch metrics for each service
- Custom dashboards for business metrics
- Alerting for service health and performance
- Distributed tracing with AWS X-Ray

**Database Monitoring**:
- RDS Performance Insights for PostgreSQL
- DocumentDB CloudWatch metrics
- Query performance monitoring
- Storage utilization alerts

**Business Metrics**:
- Case processing times
- Document upload success rates
- AI analysis completion rates
- User activity patterns

This microservices architecture separates concerns effectively while leveraging the strengths of both SQL (structured user data) and NoSQL (flexible document/case data) databases, providing a scalable foundation for the Better Call Saul application.