# BetterCallSaul.UserService

A .NET 8 Web API microservice for user authentication, registration, and profile management in the BetterCallSaul application.

## Features

- **User Authentication**: JWT-based authentication with refresh tokens
- **User Registration**: Registration code-based user signup
- **User Management**: CRUD operations for user profiles
- **Role Management**: User and Admin role support
- **Registration Code Management**: Generate and manage registration codes
- **Audit Logging**: Comprehensive audit logging for user actions
- **PostgreSQL Support**: Production-ready PostgreSQL database integration
- **SQLite Support**: Development-friendly SQLite database

## Architecture

This service follows clean architecture principles and is designed to be a standalone microservice:

- **Models/Entities**: User, Role, RegistrationCode, AuditLog
- **Interfaces/Services**: IAuthenticationService for authentication abstraction
- **Services**: AuthenticationService, DatabaseSeedingService
- **Controllers**: AuthController, AdminController
- **Data**: Entity Framework Core with PostgreSQL/SQLite

## API Endpoints

### Authentication Endpoints
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration with registration code
- `POST /api/auth/refresh` - Refresh JWT token
- `GET /api/auth/profile` - Get current user profile

### Admin Endpoints (Requires Admin role)
- `GET /api/admin/dashboard/metrics` - Get dashboard metrics
- `GET /api/admin/users` - List users with pagination
- `GET /api/admin/users/{id}` - Get user details
- `PUT /api/admin/users/{id}/status` - Update user status
- `GET /api/admin/system/health` - Get system health status
- `GET /api/admin/audit-logs` - Get audit logs
- `GET /api/admin/registration-codes/stats` - Get registration code statistics
- `POST /api/admin/registration-codes/generate` - Generate new registration codes

## Database Schema

### Users Table
- `Id` (Guid, Primary Key)
- `Email` (string, Unique)
- `FirstName` (string)
- `LastName` (string)
- `BarNumber` (string, Optional)
- `LawFirm` (string, Optional)
- `IsActive` (bool)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

### RegistrationCodes Table
- `Id` (Guid, Primary Key)
- `Code` (string, Unique)
- `CreatedBy` (string)
- `IsUsed` (bool)
- `UsedByUserId` (Guid, Foreign Key)
- `UsedAt` (DateTime)
- `ExpiresAt` (DateTime)
- `Notes` (string)

### AuditLogs Table
- `Id` (Guid, Primary Key)
- `UserId` (Guid, Foreign Key)
- `Action` (string)
- `Description` (string)
- `Level` (AuditLogLevel enum)
- `IpAddress` (string)
- `UserAgent` (string)
- `CreatedAt` (DateTime)

## Configuration

### Environment Variables
- `JWT_SECRET_KEY` - Secret key for JWT token generation (min 32 chars)
- `ConnectionStrings__UserServiceConnection` - Database connection string

### AppSettings Configuration
```json
{
  "ConnectionStrings": {
    "UserServiceConnection": "Host=localhost;Port=5432;Database=bettercallsaul_users;Username=postgres;Password=password;"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "BetterCallSaul.UserService",
    "Audience": "BetterCallSaul.Frontend",
    "ExpiryMinutes": 60
  }
}
```

## Development

### Prerequisites
- .NET 8 SDK
- PostgreSQL (for production)
- SQLite (for development)

### Running the Service
```bash
cd BetterCallSaul.UserService
dotnet run --urls="https://localhost:7192"
```

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

## Production Deployment

### PostgreSQL Setup
1. Create PostgreSQL database:
   ```sql
   CREATE DATABASE bettercallsaul_users;
   ```

2. Update connection string in appsettings.json or environment variables

### Environment Variables for Production
```bash
export JWT_SECRET_KEY="your-super-secure-secret-key-here"
export ConnectionStrings__UserServiceConnection="Host=your-postgres-host;Port=5432;Database=bettercallsaul_users;Username=your-user;Password=your-password;"
```

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BetterCallSaul.UserService.csproj", "."]
RUN dotnet restore "BetterCallSaul.UserService.csproj"
COPY . .
RUN dotnet build "BetterCallSaul.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BetterCallSaul.UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BetterCallSaul.UserService.dll"]
```

## Integration with Main Application

This service is designed to be integrated with the main BetterCallSaul application via:

1. **API Gateway**: All requests should be routed through an API gateway
2. **JWT Authentication**: The main application should use JWT tokens issued by this service
3. **Service-to-Service Communication**: Other services can validate JWT tokens with this service

## Security Considerations

- JWT tokens are signed with a secure secret key
- Passwords are hashed using ASP.NET Core Identity's password hasher
- All endpoints except login/register require authentication
- Admin endpoints require the "Admin" role
- Database connections are encrypted in production
- Audit logging tracks all sensitive operations

## Monitoring

- Serilog structured logging
- Health check endpoints
- Audit logging for security compliance
- Database performance monitoring

## License

This project is part of the BetterCallSaul application suite.