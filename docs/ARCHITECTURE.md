# Better Call Saul - Architecture Overview

## System Architecture

### High-Level Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React Frontend│    │  .NET 8 Web API │    │   SQL Database  │
│   (TypeScript)  │◄──►│   (Backend)     │◄──►│   (EF Core)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                       │                       │
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   User Browser  │    │ Azure Services  │    │  Legal APIs     │
│   (Tailwind CSS)│    │ (OpenAI, etc.)  │    │ (CourtListener) │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Backend Architecture

### Project Structure
- **BetterCallSaul.API** - Web API controllers and configuration
- **BetterCallSaul.Core** - Domain models, interfaces, and business logic
- **BetterCallSaul.Infrastructure** - Data access, services, and implementations
- **BetterCallSaul.Tests** - Unit and integration tests

### Key Components

#### 1. Authentication System
- JWT-based authentication with refresh tokens
- Role-based authorization (Admin, User)
- Registration code system for controlled access
- Secure token storage and rotation

#### 2. Case Management
- Case entity with comprehensive metadata
- Document upload and processing pipeline
- AI-powered case analysis and prediction
- Legal research integration

#### 3. AI Services
- Azure OpenAI integration for case analysis
- Document processing with Form Recognizer
- Success prediction algorithms
- Legal precedent matching

#### 4. Legal Research
- CourtListener API integration
- Justia legal database access
- Unified search across multiple legal sources
- Case law and statute retrieval

#### 5. File Processing
- Secure file upload and validation
- Virus scanning with ClamAV
- Document text extraction
- PDF and document processing

## Frontend Architecture

### Technology Stack
- **React 18** with TypeScript
- **Vite** for build tooling
- **Tailwind CSS** for styling
- **React Query** for server state
- **React Router** for navigation
- **SignalR** for real-time updates

### Component Structure
- `/src/components/` - Reusable UI components
- `/src/pages/` - Page-level components
- `/src/services/` - API integration
- `/src/hooks/` - Custom React hooks
- `/src/types/` - TypeScript definitions
- `/src/utils/` - Utility functions

### Key Features
- Responsive dashboard with case overview
- Real-time file upload with progress
- Case analysis visualization
- Admin management interface
- Audit logging and system health

## Data Model

### Core Entities
- **User** - System users with roles and permissions
- **Case** - Legal cases with metadata and status
- **Document** - Uploaded legal documents
- **CaseAnalysis** - AI-generated case analysis
- **AuditLog** - System activity tracking
- **RegistrationCode** - Access control codes

### Database
- **Development**: SQLite with Entity Framework Core
- **Production**: Azure SQL Database
- **Migrations**: Code-first migrations with EF Core
- **Relationships**: Proper foreign keys and navigation properties

## Security Architecture

### Authentication & Authorization
- JWT tokens with HS256 signing
- HTTP-only cookies for production
- Role-based access control
- Secure password hashing

### Data Protection
- Encryption at rest (database encryption)
- Encryption in transit (HTTPS/TLS)
- Secure file storage with access controls
- Audit logging for sensitive operations

### API Security
- CORS configuration for frontend domain
- Input validation and sanitization
- Rate limiting and request throttling
- SQL injection prevention

## Integration Points

### External APIs
- **Azure OpenAI** - Case analysis and predictions
- **Azure Form Recognizer** - Document processing
- **CourtListener** - Legal case research
- **Justia** - Legal database access
- **ClamAV** - Virus scanning

### Real-time Communication
- **SignalR** for progress updates
- WebSocket connections for live data
- Real-time case status updates
- File processing progress tracking

## Deployment Architecture

### Development Environment
- Local development with hot reload
- SQLite database for rapid iteration
- Development-specific configuration

### Production Environment
- **Backend**: Azure App Service
- **Frontend**: Azure Static Web Apps
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage
- **CDN**: Azure CDN for static assets

### CI/CD Pipeline
- GitHub Actions for automated deployment
- Environment-specific configuration
- Automated testing and quality checks
- Zero-downtime deployment strategies

## Monitoring & Logging

### Application Insights
- Performance monitoring
- Error tracking and alerting
- Usage analytics
- Dependency tracking

### Audit Logging
- User activity tracking
- System operations logging
- Security event monitoring
- Compliance reporting

### Health Monitoring
- System health endpoints
- Database connectivity checks
- External service status
- Performance metrics collection