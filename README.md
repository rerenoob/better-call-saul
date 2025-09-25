# Better Call Saul - AI Legal Assistant

An AI-powered legal assistant platform designed specifically for public defenders to analyze cases, conduct legal research, and generate comprehensive reports.

## 🎯 Overview

Better Call Saul leverages artificial intelligence to help public defense attorneys:
- Analyze case viability and strength with confidence scoring
- Extract and process legal documents with OCR technology
- Conduct intelligent legal research with precedent matching
- Generate professional reports for court proceedings
- Manage cases with comprehensive document annotation

## 🏗️ Architecture

### Backend (.NET 8 Web API)
- **Clean Architecture** with separation of concerns
- **Entity Framework Core** with PostgreSQL (production) / SQLite (development)
- **AWS Services Integration** - Bedrock (AI), S3 (storage), Textract (OCR)
- **JWT Authentication** with role-based access control
- **Swagger/OpenAPI** documentation
- **NoSQL Support** with MongoDB/DocumentDB for case documents

### Project Structure
- **BetterCallSaul.API** - Web API controllers and configuration
- **BetterCallSaul.Core** - Domain entities, interfaces, and business logic
- **BetterCallSaul.Infrastructure** - Data access, external services, and implementations
- **BetterCallSaul.Tests** - Unit and integration tests

### Frontend (React + TypeScript)
- **React 18** with TypeScript for type safety
- **Vite** for fast development and building
- **Tailwind CSS** for modern, responsive design
- **React Query** for efficient server state management
- **SignalR** for real-time analysis updates

### Database
- **SQLite** for development
- **SQL Server** for production
- **Entity Framework** migrations for schema management

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Git

### Backend Setup

#### Monolithic Mode (Original)
```bash
# Clone repository
git clone https://github.com/rerenoob/better-call-saul.git
cd better-call-saul

# Run the main API
dotnet run --project BetterCallSaul.API
```

#### Microservices Mode (New)
```bash
# Terminal 1 - API Gateway (routes to all services)
cd better-call-saul/BetterCallSaul.Gateway
dotnet run

# Terminal 2 - User Management Service
cd better-call-saul/BetterCallSaul.UserService
dotnet run

# Terminal 3 - Case Management Service  
cd better-call-saul/BetterCallSaul.CaseService
dotnet run

# Terminal 4 - Frontend
cd better-call-saul/better-call-saul-frontend
npm run dev
```

# Local development - no additional environment setup needed
dotnet restore
dotnet build

# Run database migrations (creates local SQLite database)
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Start the API (https://localhost:7191)
dotnet run --project BetterCallSaul.API
```

### 🔐 Environment Configuration
The application automatically configures itself based on the environment:

- **Development**: Uses SQLite database and mock AI services (no setup required)
- **Production**: Uses PostgreSQL, DocumentDB, and AWS services (configured via ECS)

For production deployment, see the deployment section below.

### Frontend Setup
```bash
# Navigate to frontend directory
cd better-call-saul-frontend

# Install dependencies
npm install

# Start development server (http://localhost:5173)
npm run dev
```

### Full Stack Development
```bash
# Terminal 1 - Backend API
dotnet watch --project BetterCallSaul.API

# Terminal 2 - Frontend React app
cd better-call-saul-frontend && npm run dev
```

## 📋 Key Features

### 🔍 Case Analysis
- AI-powered viability assessment (0-100% scoring)
- Key legal issues identification
- Potential defense strategy recommendations
- Evidence gap analysis and suggestions
- Confidence scoring and reliability metrics

### 📄 Document Management
- PDF and image document processing with OCR
- Advanced document viewer with zoom and navigation
- Annotation system (highlights, notes, stamps)
- Full-text search with context highlighting
- Batch document processing

### 📊 Report Generation
- Multiple professional report templates
- Case analysis reports with AI insights
- Legal research compilation with citations
- Custom report builder with drag-and-drop sections
- Export to PDF, Word, and HTML formats

### 🔐 Security & Authentication
- JWT-based authentication with automatic token refresh
- Separate admin login page for system administration
- Role-based access control (Admin/User)
- Registration code system for controlled access
- Comprehensive audit logging
- Data encryption at rest and in transit

## 🧪 Testing

### Backend Tests
```bash
dotnet test                                    # Run all tests
dotnet test --filter Category=Unit           # Unit tests only
dotnet test --filter Category=Integration    # Integration tests only
```

### Frontend Tests
```bash
cd better-call-saul-frontend
npx playwright test                           # E2E tests
npm run type-check                           # TypeScript validation
npm run lint                                 # Code quality checks
```

### API Testing
```bash
./test-case-analysis.sh                      # Test case analysis endpoints
```

## 🚀 Production Deployment

### Automated AWS Deployment

The application uses **GitHub Actions** for fully automated deployment to AWS:

#### Infrastructure
- **Backend**: ECS Fargate with Application Load Balancer
- **Frontend**: S3 + CloudFront CDN
- **Database**: PostgreSQL RDS + DocumentDB
- **Storage**: S3 for file uploads
- **AI Services**: AWS Bedrock (Claude), Textract (OCR)

#### Deployment Process
1. **Set GitHub Secrets** (in repository Settings → Secrets):
   ```
   AWS_ACCESS_KEY_ID = your-aws-access-key-id
   AWS_SECRET_ACCESS_KEY = your-aws-secret-access-key
   ```

2. **Push to Deploy**:
   ```bash
   git push origin main
   ```

3. **Access Application**:
   - Frontend: https://d1c0215ar7cs56.cloudfront.net
   - Backend API: http://bettercallsaul-alb-production-1289827668.us-east-1.elb.amazonaws.com

#### Deployment Documentation
- **[GitHub Secrets Setup](GITHUB-SECRETS-SETUP.md)** - Complete deployment setup guide
- **[AWS Deployment Status](AWS-DEPLOYMENT-STATUS.md)** - Current infrastructure status

### Cost Estimation
Monthly AWS costs: ~$45-65 (PostgreSQL: $18-24, DocumentDB: $17-28, ECS: $8-12, ALB: $16, S3/CloudFront: $3-5)

## 📖 Documentation

- **[Development Guide](CLAUDE.md)** - Comprehensive development setup and guidelines
- **[NoSQL Implementation](NOSQL-IMPLEMENTATION-STATUS.md)** - MongoDB/DocumentDB implementation status

## 🔧 API Endpoints

### Authentication
- `POST /api/auth/register` - User registration with code
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Token refresh

### Case Management
- `GET /api/case` - List user cases
- `POST /api/case` - Create new case
- `GET /api/case/{id}` - Get case details
- `PUT /api/case/{id}` - Update case

### Case Analysis
- `POST /api/caseanalysis/analyze/{caseId}` - Start AI analysis
- `GET /api/caseanalysis/analysis/{analysisId}` - Get analysis results
- `POST /api/caseanalysis/viability/{caseId}` - Assess case viability
- `GET /api/caseanalysis/metrics` - Analysis performance metrics

### Document Management
- `POST /api/fileupload/upload` - Upload documents
- `GET /api/documentviewer/document/{id}/content` - Get document content
- `POST /api/documentviewer/document/{id}/annotations` - Add annotations
- `GET /api/documentviewer/document/{id}/search` - Search document

### Report Generation
- `GET /api/reports/case/{caseId}/analysis` - Case analysis report
- `POST /api/reports/generate` - Generate custom report
- `GET /api/reports/templates` - Available templates

## 🌟 Production Deployment

### AWS Infrastructure Setup
```bash
# Deploy AWS infrastructure using CloudFormation
cd .aws
./deploy-infrastructure.sh
```

### Environment Variables
Configure these in production:
- `AWS:AccessKey` - AWS access key ID
- `AWS:SecretKey` - AWS secret access key
- `AWS:Region` - AWS region (default: us-east-1)
- `AWS:Bedrock:ModelId` - AWS Bedrock model ID (default: anthropic.claude-v2)
- `AWS:S3:BucketName` - S3 bucket for document storage
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `JwtSettings:SecretKey` - JWT signing key

Or use environment variables:
- `AWS_ACCESS_KEY_ID` - AWS access key ID
- `AWS_SECRET_ACCESS_KEY` - AWS secret access key
- `AWS_REGION` - AWS region (default: us-east-1)
- `AWS_BEDROCK_MODEL_ID` - AWS Bedrock model ID
- `AWS_S3_BUCKET_NAME` - S3 bucket for document storage

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with AWS Bedrock for AI-powered legal analysis
- Legal research integration with CourtListener and Justia databases
- Modern web technologies: .NET 8, React 18, TypeScript, Tailwind CSS

---

**Better Call Saul** - Empowering public defenders with AI-assisted legal analysis 🏛️⚖️