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
npm install               # Install dependencies
npm run dev               # Start development server (http://localhost:5173)
npm run build             # Build for production
npm run preview           # Preview production build
npm test                  # Run frontend tests (Jest/Vitest)
npm run type-check        # TypeScript type checking
npm run lint              # ESLint code quality checks
```

**Full Stack Development:**
```bash
# Terminal 1 - Backend API
dotnet watch

# Terminal 2 - Frontend React app
npm run dev
```

## Architecture

### Backend (.NET 8 Web API)
- **Entry Point:** `Program.cs` - ASP.NET Core Web API with JWT authentication
- **Controllers:** RESTful API endpoints for case management, AI analysis, legal research
- **Services:**
  - `Services/CaseAnalysisService.cs` - AI-powered case analysis and predictions
  - `Services/LegalResearchService.cs` - Integration with legal databases
  - `Services/DocumentProcessingService.cs` - File upload and OCR processing
  - `Services/AuthenticationService.cs` - JWT token management
- **Data Layer:**
  - Entity Framework Core with SQL Server
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
- Entity Framework Core with SQL Server
- Migrations managed via `dotnet ef` commands
- Development database: LocalDB or SQL Server Express
- Production: Azure SQL Database

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
- Backend: Azure App Service
- Frontend: Azure Static Web Apps
- CI/CD: Azure DevOps or GitHub Actions
- Monitoring: Application Insights

### Development Workflow
1. Start backend API: `dotnet watch`
2. Start frontend dev server: `npm run dev`
3. Run tests: `dotnet test` (backend), `npm test` (frontend)
4. Build for production: `dotnet publish`, `npm run build`

