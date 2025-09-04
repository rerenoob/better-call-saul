# Task: JWT Authentication and Authorization Setup

## Overview
- **Parent Feature**: IMPL-001 Backend Infrastructure and API Setup
- **Complexity**: Medium
- **Estimated Time**: 5 hours
- **Status**: Not Started

## Dependencies
### Required Tasks
- [x] 002-database-schema-design.md: User entity model required

### External Dependencies
- ASP.NET Core Identity packages
- JWT authentication middleware

## Implementation Details
### Files to Create/Modify
- `BetterCallSaul.Core/Services/IAuthenticationService.cs`: Authentication interface
- `BetterCallSaul.Infrastructure/Services/AuthenticationService.cs`: JWT implementation
- `BetterCallSaul.API/Controllers/AuthController.cs`: Login/logout endpoints
- `BetterCallSaul.API/Models/LoginRequest.cs`: Login request DTO
- `BetterCallSaul.API/Models/AuthResponse.cs`: Authentication response DTO
- `BetterCallSaul.API/Program.cs`: JWT middleware configuration
- `BetterCallSaul.API/appsettings.json`: JWT configuration section

### Code Patterns
- Use ASP.NET Core Identity with custom User entity
- Implement refresh token pattern for long-lived sessions
- Use role-based authorization attributes on controllers

## Acceptance Criteria
- [ ] User registration endpoint creates accounts with hashed passwords
- [ ] Login endpoint returns valid JWT tokens
- [ ] JWT tokens include user ID, roles, and appropriate claims
- [ ] Protected endpoints require valid JWT authentication
- [ ] Token refresh mechanism working correctly
- [ ] Role-based access control implemented (Admin, PublicDefender, User)
- [ ] Authentication state persists across application restarts

## Testing Strategy
- Unit tests: Token generation, validation, and user authentication
- Integration tests: End-to-end login flow and protected endpoint access
- Manual validation: Test login via Swagger UI and verify token contents

## System Stability
- Implement proper token expiration and renewal
- Secure JWT secret key management
- Rate limiting on authentication endpoints to prevent brute force

## Notes
- Store JWT secret in Azure Key Vault for production
- Consider implementing account lockout after failed attempts
- Ensure HTTPS-only for authentication endpoints