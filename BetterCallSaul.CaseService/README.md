# BetterCallSaul.CaseService

A .NET 8 Web API microservice for case and document management using MongoDB as the primary database.

## Overview

This service handles:
- Case lifecycle management
- Document storage and metadata
- AI analysis results
- Legal research data
- File upload and processing

## Architecture

### MongoDB Document Structure

#### Cases Collection
```javascript
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
```

#### Documents Collection
```javascript
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
```

## API Endpoints

### Cases
- `GET /api/cases?userId={guid}` - Get cases for a user
- `GET /api/cases/{id}` - Get specific case
- `POST /api/cases` - Create new case
- `PUT /api/cases/{id}` - Update case
- `DELETE /api/cases/{id}` - Delete case

### Documents
- `GET /api/cases/{caseId}/documents` - Get documents for a case
- `GET /api/cases/{caseId}/documents/{id}` - Get specific document
- `POST /api/cases/{caseId}/documents/upload` - Upload document
- `PUT /api/cases/{caseId}/documents/{id}` - Update document
- `DELETE /api/cases/{caseId}/documents/{id}` - Delete document

### Analysis
- `GET /api/cases/{caseId}/analysis` - Get analyses for a case
- `GET /api/cases/{caseId}/analysis/{id}` - Get specific analysis
- `POST /api/cases/{caseId}/analysis` - Analyze case with AI
- `PUT /api/cases/{caseId}/analysis/{id}/status` - Update analysis status

## Configuration

### MongoDB Settings
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "BetterCallSaulCaseService"
  }
}
```

### Development Settings
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "BetterCallSaulCaseService_Dev"
  }
}
```

## Getting Started

1. **Install Dependencies**
   ```bash
   dotnet restore
   ```

2. **Run the Service**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI**
   - Development: http://localhost:5150/swagger

4. **Build for Production**
   ```bash
   dotnet build --configuration Release
   ```

## Dependencies

- MongoDB.Driver (3.5.0)
- MongoDB.Driver.Core (2.30.0)
- Microsoft.Extensions.Options.ConfigurationExtensions (9.0.9)
- ASP.NET Core 8.0 Web API

## Development Notes

- Uses MongoDB ObjectId for document identifiers
- Soft delete implementation with `isDeleted` flag
- Real-time analysis progress events
- File upload validation and size limits
- CORS configured for frontend integration

## Integration with User Service

This service expects user authentication to be handled by the API Gateway. User IDs are passed as strings and stored as foreign keys in MongoDB documents.

## Production Deployment

For production deployment, configure:
- AWS DocumentDB connection string
- S3 bucket for file storage
- Proper CORS origins
- JWT authentication validation