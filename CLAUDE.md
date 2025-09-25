## Important Notes
- Use the "~/Projects/dev-prompts/RULES.md" file for additional rules and
guidance for development.
- Use the "~/Projects/dev-prompts/[file-name].md" files for development tasks.
- Update README.md and CLAUDE.md whenever applicable.
- Do not make any code changes before reading the RULES.md file mentioned above.
- Do not add code attribution in git commit

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a modern web application called "better-call-saul" - an AI Lawyer application for public defenders. The project uses a React + TypeScript frontend with a .NET 8 Web API backend, designed to help public defenders analyze cases, conduct legal research, and make data-driven decisions.

## Common Commands

**Backend (.NET Web API):**
```bash
dotnet build              # Build the Web API project
dotnet run                # Run the API (https://localhost:7191)
dotnet watch              # Run with hot reload during development
dotnet test               # Run backend unit and integration tests
dotnet restore            # Restore NuGet packages
```

**Frontend (React + TypeScript):**
```bash
cd better-call-saul-frontend  # Navigate to frontend directory
npm install               # Install dependencies
npm run dev               # Start development server (http://localhost:5173)
npm run build             # Build for production
npm run preview           # Preview production build
npm run type-check        # TypeScript type checking
npm run lint              # ESLint code quality checks
```

**Full Stack Development:**
```bash
# Terminal 1 - Backend API
dotnet watch

# Terminal 2 - Frontend React app (from better-call-saul-frontend/)
cd better-call-saul-frontend && npm run dev
```

**Testing Commands:**
```bash
# Backend tests
dotnet test                                    # Run all backend tests
dotnet test --filter Category=Unit           # Run unit tests only
dotnet test --filter Category=Integration    # Run integration tests only

# Frontend tests
cd better-call-saul-frontend
npx playwright test                           # Run E2E tests

# Script-based testing
./test-case-analysis.sh                       # Test case analysis endpoints
```

**Database Management:**
```bash
# Entity Framework migrations
dotnet ef migrations add <MigrationName> --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Registration code management
./scripts/manage-registration-codes.sh seed 100    # Generate 100 codes
./scripts/manage-registration-codes.sh list        # List existing codes
./scripts/manage-registration-codes.sh stats       # Show statistics
./scripts/manage-registration-codes.sh cleanup     # Remove expired codes
```

## Architecture

The solution follows clean architecture principles with clear separation between layers.

### Solution Structure
- **BetterCallSaul.API** - Web API project with controllers and configuration
- **BetterCallSaul.Core** - Domain entities, interfaces, and business logic
- **BetterCallSaul.Infrastructure** - Data access, external services, and implementations
- **BetterCallSaul.Tests** - Unit and integration tests
- **better-call-saul-frontend/** - React TypeScript frontend application

### Backend (.NET 8 Web API)
- **Entry Point:** `Program.cs` - ASP.NET Core Web API with JWT authentication
- **Controllers:** RESTful API endpoints for case management, AI analysis, legal research
- **Core Services:**
  - Case analysis with AI integration
  - Legal research and document processing
  - User authentication and registration code management
- **Data Layer:**
  - Entity Framework Core with SQLite (development) / SQL Server (production)
  - Models for cases, users, documents, analysis results
  - Audit logging and compliance tracking
- **Configuration:**
  - JWT authentication with refresh tokens
  - CORS configured for React frontend
  - OpenAPI/Swagger documentation
  - Azure service integrations (OpenAI, Form Recognizer, Key Vault)

### Frontend (React + TypeScript)
- **Entry Point:** `src/main.tsx` - React 18 with TypeScript
- **Application Structure:**
  - `src/components/` - Reusable UI components
  - `src/pages/` - Page-level components (Dashboard, CaseAnalysis, etc.)
  - `src/services/` - API integration and HTTP client
  - `src/hooks/` - Custom React hooks for state management
  - `src/types/` - TypeScript type definitions
  - `src/utils/` - Utility functions and helpers
- **Key Features:**
  - JWT-based authentication with automatic token refresh
  - Real-time updates via SignalR integration
  - Responsive design with Tailwind CSS
  - File upload with drag-and-drop and progress tracking
  - Data visualization with Recharts
  - PDF document viewer with annotations
- **State Management:**
  - React Query for server state and caching
  - React Context for global application state
  - Local state with React hooks

### Integration
- **API Communication:** RESTful APIs with TypeScript-generated client types
- **Real-time Updates:** SignalR for long-running AI analysis progress
- **Authentication:** JWT tokens with automatic refresh and secure storage
- **File Handling:** Secure upload to backend with client-side progress tracking

### Namespace
- **Backend:** Uses `BetterCallSaul` namespace following C# conventions
- **Frontend:** Uses consistent naming with TypeScript interfaces and types

### Admin Panel Features
- **Dashboard:** System overview with user metrics and activity
- **User Management:** View, activate/deactivate, and manage user accounts
- **Case Management:** View all uploaded cases, filter by status, search, and manage case files
- **System Health:** Monitor database connectivity, memory usage, and system uptime
- **Audit Logs:** Track system activity, security events, and user actions
- **Registration Codes:** Generate, manage, and clean up user registration codes

## Development Notes

### Backend Configuration
- Target Framework: .NET 8.0
- Nullable reference types enabled
- Implicit usings enabled
- Default API URL: https://localhost:7191
- OpenAPI/Swagger UI available at `/swagger`
- Development environment configured for hot reload

### Frontend Configuration
- React 18 with TypeScript
- Vite for fast development and building
- Default dev server: http://localhost:5173
- Tailwind CSS for styling
- ESLint and Prettier for code quality
- Vitest for unit testing
- Playwright for E2E testing

### Environment Variables
- Backend: `appsettings.Development.json` for local development
- Frontend: `.env.development` for development configuration
- Production secrets managed via Azure Key Vault

### Database
- Entity Framework Core with SQLite (development) / SQL Server (production)
- Migrations managed via `dotnet ef` commands (see Database Management section above)
- Local database file: `BetterCallSaul.db` in project root
- Registration codes managed via `./scripts/manage-registration-codes.sh`

### External Services
- Azure OpenAI Service for case analysis
- Azure Form Recognizer for document processing
- Public legal databases (CourtListener, Justia) for research
- SignalR for real-time communication

### Security
- JWT authentication with HS256 signing
- CORS configured for frontend domain
- HTTPS enforcement in production
- Data encryption at rest and in transit
- Comprehensive audit logging

### Deployment
- Backend: AWS ECS Fargate
- Frontend: AWS S3 + CloudFront
- CI/CD: GitHub Actions with AWS deployment workflows
- Monitoring: AWS CloudWatch

### Available CLI tools
- AWS CLI and GH CLI are installed and configured. Use them when troubleshooting.
- Playwright MCP server is installed and configured. Use it when troubleshooting.

#### AWS Environment Variables
**Required for production deployment:**
```bash
# AWS Credentials
AWS_ACCESS_KEY_ID="your-access-key-id"
AWS_SECRET_ACCESS_KEY="your-secret-access-key"
AWS_REGION="us-east-1"

# Application Configuration
ASPNETCORE_ENVIRONMENT="Production"
JWT_SECRET_KEY="your-secure-secret-key-at-least-32-chars"
```

**Set via AWS CLI:**
```bash
# For ECS task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# For GitHub Actions secrets (set in repository settings)
# AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, CLOUDFRONT_DISTRIBUTION_ID, API_BASE_URL
```

**Infrastructure Deployment:**
```bash
cd .aws
./deploy-infrastructure.sh
```

### Development Workflow
1. Start backend API: `dotnet watch`
2. Start frontend dev server: `npm run dev`
3. Run tests: `dotnet test` (backend), `npm test` (frontend)
4. Build for production: `dotnet publish`, `npm run build`

## Production Deployment Safeguards

### Dependency Management
- **Critical**: Always verify Serilog sink dependencies when modifying logging configuration
- If adding new sinks to `appsettings.Production.json`, ensure corresponding NuGet packages are installed
- Required packages for current configuration:
  - `Serilog.Sinks.Console` (for Console sink)
  - `Serilog.Sinks.ApplicationInsights` (for ApplicationInsights sink)

### Pre-deployment Checklist
1. **Build Verification**: Run `dotnet build --configuration Release` to ensure all dependencies are resolved
2. **Production Environment Test**: Test startup with Production environment settings locally
3. **Dependency Audit**: Verify all Serilog sinks in config have corresponding packages referenced
4. **CI/CD Pipeline**: GitHub Actions workflow automatically validates dependencies and build integrity

### Common Issues Prevention
- **Missing Serilog Sinks**: The build will fail in CI if configuration references missing sink packages
- **Environment Variables**: All required AWS credentials are validated during startup
- **AWS Service Dependencies**: Mock endpoints used in CI to validate service integration points

### Troubleshooting Production Issues
1. Check ECS service logs: `aws logs describe-log-streams --log-group-name /ecs/bettercallsaul-api`
2. Look for missing dependency errors in Docker logs
3. **Verify all environment variables are configured in ECS task definition** - Most common cause of 503 errors
   - Check `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are set
   - Check `JWT_SECRET_KEY` is set (minimum 32 characters)
   - Check `ASPNETCORE_ENVIRONMENT` is set to "Production"
4. Use GitHub Actions workflow to test configuration changes before deployment
5. **If API returns 503 Service Unavailable**: Missing AWS credentials or IAM permissions - verify steps in "AWS Environment Variables" section above

