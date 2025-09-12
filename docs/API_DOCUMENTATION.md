# Better Call Saul - API Documentation

## Base URL
- **Development**: `https://localhost:7191`
- **Production**: `https://your-api-domain.com`

## Authentication

All endpoints (except auth endpoints) require JWT authentication.

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2024-01-01T00:00:00Z"
}
```

### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}
```

### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "registrationCode": "ABC123-DEF456"
}
```

## Case Management API

### Get All Cases
```http
GET /api/cases
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": 1,
    "title": "State v. Smith",
    "caseNumber": "CR-2024-001",
    "status": "Pending",
    "priority": "High",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### Get Case by ID
```http
GET /api/cases/{id}
Authorization: Bearer <token>
```

### Create Case
```http
POST /api/cases
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "State v. Johnson",
  "caseNumber": "CR-2024-002",
  "description": "Burglary case with potential search issues",
  "priority": "Medium"
}
```

### Update Case
```http
PUT /api/cases/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Updated Case Title",
  "status": "InProgress",
  "priority": "High"
}
```

### Delete Case
```http
DELETE /api/cases/{id}
Authorization: Bearer <token>
```

## Document Management API

### Upload Document
```http
POST /api/documents/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <file_content>
caseId: 1
```

**Response:**
```json
{
  "id": 1,
  "fileName": "document.pdf",
  "fileSize": 102400,
  "uploadedAt": "2024-01-01T00:00:00Z",
  "status": "Processing"
}
```

### Get Document
```http
GET /api/documents/{id}
Authorization: Bearer <token>
```

### Get Documents for Case
```http
GET /api/cases/{caseId}/documents
Authorization: Bearer <token>
```

### Delete Document
```http
DELETE /api/documents/{id}
Authorization: Bearer <token>
```

## Case Analysis API

### Analyze Case
```http
POST /api/case-analysis/analyze/{caseId}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": 1,
  "caseId": 1,
  "status": "Completed",
  "summary": "Case analysis summary...",
  "viabilityScore": 75,
  "recommendations": [
    {
      "type": "PleaDeal",
      "confidence": 0.85,
      "reasoning": "Strong evidence suggests favorable plea deal"
    }
  ],
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### Get Case Analysis
```http
GET /api/case-analysis/{caseId}
Authorization: Bearer <token>
```

### Get Analysis History
```http
GET /api/case-analysis/history/{caseId}
Authorization: Bearer <token>
```

## Legal Research API

### Search Case Law
```http
POST /api/legal-research/search
Authorization: Bearer <token>
Content-Type: application/json

{
  "query": "fourth amendment search and seizure",
  "jurisdiction": "federal",
  "maxResults": 10
}
```

**Response:**
```json
{
  "results": [
    {
      "id": "courtlistener-123",
      "title": "Smith v. State",
      "court": "Supreme Court",
      "date": "2020-06-15",
      "summary": "Case regarding search and seizure...",
      "url": "https://courtlistener.com/opinion/123"
    }
  ],
  "totalCount": 25
}
```

### Get Case Details
```http
GET /api/legal-research/case/{caseId}
Authorization: Bearer <token>
```

### Get Related Cases
```http
GET /api/legal-research/related/{caseId}
Authorization: Bearer <token>
```

## Admin API

### Get System Health
```http
GET /api/admin/health
Authorization: Bearer <token>
```

**Response:**
```json
{
  "status": "Healthy",
  "services": {
    "database": "Online",
    "aiService": "Online",
    "legalApi": "Online"
  },
  "uptime": "5d 12h 30m"
}
```

### Get Audit Logs
```http
GET /api/admin/audit-logs
Authorization: Bearer <token>
```

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20)
- `startDate` (optional)
- `endDate` (optional)
- `userId` (optional)
- `action` (optional)

### Manage Users
```http
GET /api/admin/users
Authorization: Bearer <token>

POST /api/admin/users
Authorization: Bearer <token>
Content-Type: application/json

PUT /api/admin/users/{id}
Authorization: Bearer <token>
Content-Type: application/json

DELETE /api/admin/users/{id}
Authorization: Bearer <token>
```

### Manage Registration Codes
```http
GET /api/admin/registration-codes
Authorization: Bearer <token>

POST /api/admin/registration-codes/generate
Authorization: Bearer <token>
Content-Type: application/json

{
  "count": 10,
  "expiresAt": "2024-12-31T23:59:59Z"
}

DELETE /api/admin/registration-codes/{code}
Authorization: Bearer <token>
```

## Real-time API (SignalR)

### Hub Connections
- **Hub URL**: `/hubs/caseprocessing`
- **Protocol**: WebSockets with fallback

### Events

#### Case Processing Events
```csharp
// Client subscribes to case updates
connection.on("CaseProcessingUpdate", (update) => {
  console.log(`Case ${update.caseId}: ${update.progress}%`);
});

// Server events
public async Task SendCaseUpdate(string caseId, int progress, string status)
{
  await Clients.Group($"case-{caseId}").SendAsync("CaseProcessingUpdate", 
    new { caseId, progress, status });
}
```

#### Document Processing Events
```csharp
// Client subscribes to document updates
connection.on("DocumentProcessingUpdate", (update) => {
  console.log(`Document ${update.documentId}: ${update.status}`);
});
```

### Client Connection
```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/caseprocessing')
  .withAutomaticReconnect()
  .build();

// Join case group
await connection.invoke('JoinCaseGroup', caseId);

// Start connection
await connection.start();
```

## Error Responses

### Standard Error Format
```json
{
  "error": {
    "code": "NotFound",
    "message": "Case not found",
    "details": "The requested case with ID 123 does not exist."
  }
}
```

### Common HTTP Status Codes
- `200 OK` - Successful request
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid input data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Rate Limiting

- **General API**: 100 requests per minute per user
- **File Upload**: 5 concurrent uploads per user
- **AI Analysis**: 2 concurrent analyses per user
- **Legal Research**: 10 requests per minute per user

## Versioning

API versioning is handled through URL path:
- Current version: `v1`
- Example: `/api/v1/cases`

All endpoints are versioned to ensure backward compatibility.