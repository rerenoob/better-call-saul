# Better Call Saul - Development Guide

## Getting Started

### Prerequisites
- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **SQL Server** (or SQLite for development)
- **Git**

### Clone and Setup
```bash
git clone <repository-url>
cd better-call-saul

# Restore backend dependencies
dotnet restore

# Setup frontend
cd better-call-saul-frontend
npm install
```

## Development Environment

### Backend Setup
1. **Restore packages**: `dotnet restore`
2. **Build solution**: `dotnet build`
3. **Run migrations**: 
   ```bash
   dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
   ```
4. **Run API**: `dotnet run --project BetterCallSaul.API`

### Frontend Setup
1. **Install dependencies**: `npm install`
2. **Start dev server**: `npm run dev`
3. **Build for production**: `npm run build`

### Environment Configuration

#### Backend (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=BetterCallSaul.db"
  },
  "JwtSettings": {
    "SecretKey": "development-secret-key-change-in-production",
    "Issuer": "BetterCallSaul",
    "Audience": "BetterCallSaulUsers",
    "ExpiryMinutes": 60,
    "RefreshExpiryDays": 7
  }
}
```

#### Frontend (.env.development)
```env
VITE_API_BASE_URL=https://localhost:7191
VITE_SIGNALR_HUB_URL=https://localhost:7191/hubs
```

## Code Organization

### Backend Structure
```
BetterCallSaul.API/
├── Controllers/          # API endpoints
├── DTOs/                # Data transfer objects
├── Middleware/          # Custom middleware
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration

BetterCallSaul.Core/
├── Models/              # Domain models
├── Interfaces/          # Service interfaces
├── Enums/              # Enumerations
└── Configuration/       # Configuration classes

BetterCallSaul.Infrastructure/
├── Data/               # Entity Framework context
├── Services/           # Service implementations
├── Http/               # HTTP clients
├── Validators/         # Validation logic
└── Migrations/         # Database migrations
```

### Frontend Structure
```
better-call-saul-frontend/
├── src/
│   ├── components/     # Reusable UI components
│   ├── pages/         # Page components
│   ├── services/      # API services
│   ├── hooks/         # Custom React hooks
│   ├── types/         # TypeScript definitions
│   ├── utils/         # Utility functions
│   └── contexts/      # React contexts
├── public/            # Static assets
└── tests/             # Test files
```

## Development Workflow

### 1. Feature Development
1. Create feature branch: `git checkout -b feature/your-feature`
2. Implement backend changes
3. Implement frontend changes
4. Write tests
5. Test locally
6. Create pull request

### 2. Database Changes
1. Create migration: 
   ```bash
   dotnet ef migrations add YourMigrationName --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
   ```
2. Apply migration: 
   ```bash
   dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
   ```
3. Test migration works correctly

### 3. Testing

#### Backend Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test BetterCallSaul.Tests

# Run tests with filter
dotnet test --filter "Category=Unit"
```

#### Frontend Tests
```bash
# Run unit tests
npm test

# Run E2E tests
npm run test:e2e

# Run specific test file
npm test -- src/services/authService.test.ts
```

### 4. Code Quality

#### Backend
```bash
# Format code
dotnet format

# Analyze code quality
dotnet analyze
```

#### Frontend
```bash
# Lint code
npm run lint

# Type checking
npm run type-check

# Format code
npm run format
```

## Common Tasks

### Adding a New API Endpoint

1. **Create DTOs** (in Core project)
2. **Create Controller** (in API project)
3. **Implement Service** (in Infrastructure project)
4. **Register Service** in DI container
5. **Add Authorization** if needed
6. **Write Tests**

### Adding a New Frontend Component

1. **Create TypeScript interfaces**
2. **Create React component**
3. **Add to appropriate folder**
4. **Write tests**
5. **Add to storybook** (if applicable)

### Database Operations

#### Query Examples
```csharp
// Basic query
var cases = await _context.Cases
    .Where(c => c.Status == CaseStatus.Active)
    .ToListAsync();

// Include related data
var caseWithDocuments = await _context.Cases
    .Include(c => c.Documents)
    .FirstOrDefaultAsync(c => c.Id == caseId);

// Projection to DTO
var caseSummaries = await _context.Cases
    .Select(c => new CaseSummaryDto
    {
        Id = c.Id,
        Title = c.Title,
        Status = c.Status
    })
    .ToListAsync();
```

#### Transaction Management
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // Multiple operations
    _context.Cases.Add(newCase);
    _context.Documents.AddRange(documents);
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Debugging

### Backend Debugging
1. **Set breakpoints** in Visual Studio or VS Code
2. **Attach debugger** to running process
3. **Use logging**: `_logger.LogInformation("Message {Value}", value);`
4. **Check database** with SQL Server Management Studio

### Frontend Debugging
1. **Browser DevTools** for React components
2. **React Developer Tools** extension
3. **Network tab** for API calls
4. **Console logging** for debugging

### Common Issues

#### Database Connection Issues
- Check connection string in appsettings.json
- Verify database server is running
- Check migration status

#### CORS Issues
- Verify CORS configuration in Program.cs
- Check frontend URL is in allowed origins

#### Authentication Issues
- Verify JWT secret key
- Check token expiration settings
- Validate refresh token logic

## Performance Optimization

### Backend
- Use async/await for I/O operations
- Implement caching where appropriate
- Use EF Core performance best practices
- Monitor database query performance

### Frontend
- Implement React.memo for expensive components
- Use React Query for server state caching
- Optimize bundle size with code splitting
- Implement lazy loading for routes

## Security Considerations

### Backend Security
- Validate all inputs
- Use parameterized queries to prevent SQL injection
- Implement proper authorization checks
- Secure sensitive configuration in production

### Frontend Security
- Validate user inputs
- Sanitize HTML content
- Implement proper error handling
- Use HTTPS in production

## Deployment Preparation

### Backend
1. **Update appsettings.Production.json**
2. **Run migrations** on production database
3. **Set environment variables** for secrets
4. **Configure logging** for production

### Frontend
1. **Update environment variables**
2. **Build production bundle**: `npm run build`
3. **Test production build**: `npm run preview`
4. **Configure CDN** for static assets

## Useful Commands

### Database
```bash
# Create migration
dotnet ef migrations add MigrationName --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Apply migration
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Remove migration
dotnet ef migrations remove --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API

# Generate SQL script
dotnet ef migrations script --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
```

### Development
```bash
# Watch mode for backend
dotnet watch --project BetterCallSaul.API

# Watch mode for frontend
npm run dev

# Check for outdated packages
npm outdated

# Update packages
npm update
```

### Testing
```bash
# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestClass"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Debug test
dotnet test --debug
```

## Troubleshooting

### Common Problems

#### "Unable to create migration"
- Ensure EF Core tools are installed: `dotnet tool install --global dotnet-ef`
- Check project references are correct

#### "Database connection failed"
- Verify connection string
- Check database server is running
- Ensure firewall allows connections

#### "JWT validation failed"
- Check secret key configuration
- Verify token expiration settings

#### "CORS error"
- Verify allowed origins in CORS configuration
- Check frontend URL matches configured origins