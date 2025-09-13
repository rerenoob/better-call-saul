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
- **Entity Framework Core** for data persistence
- **Azure OpenAI Service** integration for AI analysis
- **JWT Authentication** with role-based access control
- **Swagger/OpenAPI** documentation

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
```bash
# Clone repository
git clone https://github.com/rerenoob/better-call-saul.git
cd better-call-saul

# 🔒 Configure environment variables (REQUIRED)
cp .env.example .env
# Edit .env with your actual values:
# - Generate a secure JWT secret key (32+ characters)
# - Add your Azure OpenAI endpoint and API key
nano .env

# Load environment variables
source .env  # or export them manually

# Restore dependencies and build
dotnet restore
dotnet build

# Run database migrations
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Start the API (https://localhost:7191)
dotnet run --project BetterCallSaul.API
```

### 🔐 Security Configuration
**IMPORTANT:** This application requires proper environment variable configuration for security:

1. **JWT Secret Key:** Must be 32+ characters, cryptographically secure
2. **Azure OpenAI Credentials:** Your actual Azure OpenAI service endpoint and API key
3. **Never commit secrets:** Secrets should never be committed to version control

See `SECURITY_FIXES.md` for detailed security information.

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

## 📖 Documentation

- **[Development Guide](CLAUDE.md)** - Comprehensive development setup and guidelines
- **[Implementation Summary](IMPLEMENTATION_SUMMARY.md)** - Detailed feature documentation
- **[Deployment Guide](DEPLOYMENT.md)** - Production deployment instructions
- **[Planning Archive](PLANNING_ARCHIVE.md)** - Original planning documentation

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

### Azure Configuration
```bash
# Set up Azure resources
az group create --name better-call-saul-rg --location eastus
az appservice plan create --name better-call-saul-plan --resource-group better-call-saul-rg --sku B1
az webapp create --name better-call-saul-api --plan better-call-saul-plan --resource-group better-call-saul-rg
```

### Environment Variables
Configure these in production:
- `AzureOpenAI:Endpoint` - Azure OpenAI service endpoint
- `AzureOpenAI:ApiKey` - Azure OpenAI API key
- `AzureFormRecognizer:EndpointFromConfig` - Azure Form Recognizer endpoint
- `AzureFormRecognizer:ApiKeyFromConfig` - Azure Form Recognizer API key
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `JwtSettings:SecretKey` - JWT signing key

Or use environment variables:
- `AZURE_OPENAI_ENDPOINT` - Azure OpenAI service endpoint
- `AZURE_OPENAI_API_KEY` - Azure OpenAI API key
- `AZURE_FORM_RECOGNIZER_ENDPOINT` - Azure Form Recognizer endpoint
- `AZURE_FORM_RECOGNIZER_API_KEY` - Azure Form Recognizer API key
- `AZURE_BLOB_STORAGE_CONNECTION_STRING` - Azure Blob Storage connection string

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with Azure OpenAI Service for AI-powered legal analysis
- Legal research integration with CourtListener and Justia databases
- Modern web technologies: .NET 8, React 18, TypeScript, Tailwind CSS

---

**Better Call Saul** - Empowering public defenders with AI-assisted legal analysis 🏛️⚖️