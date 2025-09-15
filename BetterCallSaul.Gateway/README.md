# BetterCallSaul API Gateway

This is the API Gateway for the BetterCallSaul microservices architecture. It provides centralized routing, authentication validation, and request/response transformation between the User Management Service and Case Management Service.

## Features

- **Request Routing**: Routes requests to appropriate microservices based on URL patterns
- **JWT Authentication**: Validates JWT tokens issued by the User Service
- **CORS Configuration**: Handles cross-origin requests for the React frontend
- **Service Discovery**: Configurable service endpoints with timeout management
- **Health Checks**: Provides health and readiness endpoints for monitoring
- **Request Logging**: Comprehensive logging for debugging and monitoring

## Service Routing

The gateway routes requests based on URL patterns:

### User Service Routes
- `/api/auth/*` - Authentication endpoints (login, register, refresh token)
- `/api/users/*` - User management endpoints
- `/api/admin/*` - Administrative endpoints

### Case Service Routes
- `/api/cases/*` - Case management endpoints
- `/api/documents/*` - Document upload and management
- `/api/analysis/*` - AI analysis endpoints
- `/api/research/*` - Legal research endpoints

## Configuration

### Environment Variables

```bash
# JWT Configuration
JWT_SECRET_KEY=YourSuperSecretKeyForJWTTokenGenerationAtLeast32CharactersLong

# Service URLs
USER_SERVICE_URL=https://localhost:7191
CASE_SERVICE_URL=https://localhost:7192

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

### appsettings.json

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGenerationAtLeast32CharactersLong",
    "Issuer": "BetterCallSaul.UserService",
    "Audience": "BetterCallSaul.API"
  },
  "Services": {
    "UserService": {
      "BaseUrl": "https://localhost:7191",
      "Timeout": 30
    },
    "CaseService": {
      "BaseUrl": "https://localhost:7192", 
      "Timeout": 30
    }
  }
}
```

## Development

### Running the Gateway

```bash
# Build and run
cd BetterCallSaul.Gateway
dotnet run

# Or with hot reload
dotnet watch run
```

The gateway will be available at `https://localhost:5001`

### Testing

Use the provided `BetterCallSaul.Gateway.http` file to test the gateway endpoints:

```bash
# Health check
GET https://localhost:5001/health

# Test routing to User Service
GET https://localhost:5001/api/users
Authorization: Bearer <your_jwt_token>

# Test routing to Case Service  
GET https://localhost:5001/api/cases
Authorization: Bearer <your_jwt_token>
```

## Authentication Flow

1. Client authenticates with User Service (`/api/auth/login`)
2. User Service returns JWT token
3. Client includes JWT token in Authorization header for all subsequent requests
4. API Gateway validates JWT token for all requests except auth endpoints
5. Validated requests are routed to appropriate services with user context

## Deployment

### Production Configuration

For production deployment, ensure:

1. Set proper JWT secret key via environment variable
2. Configure production service URLs
3. Enable proper logging and monitoring
4. Set up SSL certificates
5. Configure load balancing if needed

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BetterCallSaul.Gateway.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BetterCallSaul.Gateway.dll"]
```

## Monitoring

The gateway provides health endpoints:

- `GET /health` - Basic health status
- `GET /health/ready` - Readiness check with service connectivity

## Troubleshooting

### Common Issues

1. **JWT Validation Errors**: Ensure the JWT secret key matches between User Service and Gateway
2. **Service Connectivity**: Check that downstream services are running and accessible
3. **CORS Issues**: Verify frontend URLs are included in CORS configuration
4. **Timeout Errors**: Adjust service timeout settings in configuration

### Logs

Enable debug logging for detailed request/response information:

```json
{
  "Logging": {
    "LogLevel": {
      "BetterCallSaul.Gateway": "Debug"
    }
  }
}
```