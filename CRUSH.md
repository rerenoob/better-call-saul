## Important Notes
- Update README.md and CLAUDE.md whenever applicable.
- Use the "~/Projects/dev-prompts/RULES.md" file for additional rules and guidance for development.
- Use the "~/Projects/dev-prompts/[file-name].md" files for development tasks.
- Do not make any code changes before reading the RULES.md file mentioned above.
- Don't make any code attribution for git commit

# CRUSH.md - Agentic Coding Assistant Guidelines

## Technology Stack
- **Backend**: .NET 8 Web API with Entity Framework Core
- **Frontend**: React 18 + TypeScript with Vite
- **Authentication**: JWT with refresh tokens
- **Database**: SQL Server (production) / SQLite (development) with Entity Framework Core
- **Styling**: Tailwind CSS
- **Real-time**: SignalR for progress updates
- **Testing**: xUnit (backend), Jest/Vitest (frontend), Playwright (E2E)

## Backend Development Commands
```bash
# .NET Web API Commands
dotnet build              # Build Web API project
dotnet run                # Run API (https://localhost:7191)
dotnet watch              # Run with hot reload
dotnet restore            # Restore NuGet packages
dotnet test               # Run backend unit/integration tests

# Entity Framework Commands
dotnet ef migrations add <MigrationName>    # Add new migration
dotnet ef database update                   # Apply migrations to database
dotnet ef database drop                     # Drop development database
```

## Frontend Development Commands
```bash
# React + TypeScript Commands
npm install               # Install dependencies
npm run dev               # Start dev server (http://localhost:5173)
npm run build             # Build for production
npm run preview           # Preview production build
npm test                  # Run Jest/Vitest tests
npm run test:e2e          # Run Playwright E2E tests
npm run type-check        # TypeScript type checking
npm run lint              # ESLint code quality checks
npm run format            # Prettier code formatting
```

## Git Hooks
```bash
# Pre-commit hook automatically runs:
# - Backend tests (dotnet test)
# - Frontend linting (npm run lint)
# - Frontend type checking (npm run type-check)

# To skip pre-commit hook (use with caution):
git commit --no-verify
```

## Full Stack Development Workflow
```bash
# Terminal 1 - Start Backend API
cd /backend/path
dotnet watch

# Terminal 2 - Start Frontend Dev Server
cd /frontend/path
npm run dev

# Terminal 3 - Run Tests (as needed)
dotnet test               # Backend tests
npm test                  # Frontend tests
```

## Backend Code Style Guidelines
- **Namespace**: `BetterCallSaul` following C# conventions
- **Framework**: .NET 8 Web API with minimal APIs or Controllers
- **Nullable**: Reference types enabled
- **Implicit Usings**: Enabled globally
- **File Organization**: 
  - `/Controllers/` - API controllers
  - `/Services/` - Business logic services
  - `/Models/` - Data models and DTOs
  - `/Data/` - Entity Framework context and configurations
  - `/Security/` - Authentication and authorization
- **Naming**: PascalCase for classes/methods, camelCase for parameters/variables
- **Error Handling**: Use built-in ASP.NET Core exception handling with custom middleware

## Frontend Code Style Guidelines
- **Framework**: React 18 with TypeScript
- **File Organization**:
  - `/src/components/` - Reusable UI components
  - `/src/pages/` - Page-level components
  - `/src/services/` - API integration and HTTP client
  - `/src/hooks/` - Custom React hooks
  - `/src/types/` - TypeScript type definitions
  - `/src/utils/` - Utility functions
- **Naming**: PascalCase for components, camelCase for functions/variables
- **State Management**: React Query for server state, React Context for global state
- **Styling**: Tailwind CSS utility classes with custom components

## Architecture Patterns

### Backend Patterns
- RESTful API design with OpenAPI/Swagger documentation
- JWT authentication with automatic refresh token rotation
- Entity Framework Core with Code First migrations
- Dependency injection for services and repositories
- CORS configuration for React frontend integration
- SignalR hubs for real-time communication
- Azure service integration (OpenAI, Form Recognizer, Key Vault)

### Frontend Patterns
- Component-based architecture with TypeScript interfaces
- Custom hooks for API integration and state management
- React Query for server state caching and synchronization
- React Router for client-side routing
- Context providers for global state (auth, theme, etc.)
- Form handling with React Hook Form and validation
- Real-time updates via SignalR connection

## Development Environment

### Backend Configuration
- Default API URL: https://localhost:7191
- OpenAPI/Swagger UI: https://localhost:7191/swagger
- Development database: LocalDB or SQL Server Express
- Configuration: `appsettings.Development.json`
- Hot reload supported via `dotnet watch`

### Frontend Configuration
- Default dev server: http://localhost:5173
- Vite for fast bundling and HMR
- TypeScript strict mode enabled
- ESLint + Prettier for code quality
- Configuration: `.env.development`

### Environment Variables
```bash
# Backend (.env or appsettings.json)
ConnectionStrings__DefaultConnection="..."
AzureOpenAI__Endpoint="..."
AzureOpenAI__ApiKey="..."
JwtSettings__SecretKey="..."

# Frontend (.env.development)
VITE_API_BASE_URL="https://localhost:7191"
VITE_SIGNALR_HUB_URL="https://localhost:7191/hubs"
```

## API Integration Patterns

### Backend API Structure
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CasesController : ControllerBase
{
    // RESTful endpoints with proper HTTP methods
    // OpenAPI documentation attributes
    // Async/await pattern for all operations
    // Proper error handling and status codes
}
```

### Frontend API Integration
```typescript
// Type-safe API client with React Query
const { data, isLoading, error } = useQuery({
  queryKey: ['cases', caseId],
  queryFn: () => apiClient.getCaseAnalysis(caseId),
});
```

## Security Guidelines
- JWT tokens stored in HTTP-only cookies (production) or localStorage (development)
- CORS properly configured for React frontend domain
- All API endpoints require authentication except auth endpoints
- Data encryption at rest and in transit
- Input validation on both frontend and backend
- Audit logging for all sensitive operations
- Azure Key Vault for production secrets

## Testing Patterns

### Backend Testing
```csharp
[Fact]
public async Task AnalyzeCase_ValidInput_ReturnsAnalysis()
{
    // Arrange, Act, Assert pattern
    // Mock external services
    // Test both success and error scenarios
}
```

### Frontend Testing
```typescript
describe('CaseAnalysis Component', () => {
  it('displays analysis results', async () => {
    // React Testing Library with user events
    // Mock API responses with MSW
    // Test user interactions and state changes
  });
});
```

## Azure OpenAI Configuration
- Configure in backend `appsettings.json` or Azure Key Vault
- Required: Endpoint, ApiKey, DeploymentName
- Service: `CaseAnalysisService` with dependency injection
- Error handling: Graceful fallback for AI service failures
- Rate limiting: Implement backoff strategies for API limits

## SignalR Real-time Communication
```csharp
// Backend Hub
public class CaseProcessingHub : Hub
{
    public async Task JoinCaseGroup(string caseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"case-{caseId}");
    }
}
```

```typescript
// Frontend Connection
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/caseprocessing')
  .build();
```

## Deployment Configuration
- **Backend**: Azure App Service with Application Insights
- **Frontend**: Azure Static Web Apps with CDN
- **Database**: Azure SQL Database with automatic backups
- **CI/CD**: GitHub Actions or Azure DevOps pipelines
- **Monitoring**: Application Insights + frontend telemetry

💘 Generated with Crush
Co-Authored-By: Crush <crush@charm.land>