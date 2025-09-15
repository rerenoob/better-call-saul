# Frontend Microservices Migration Guide

## Overview
This document outlines the changes made to the React TypeScript frontend to support the new microservices architecture with API Gateway.

## Changes Made

### 1. Environment Configuration
- Added `VITE_GATEWAY_BASE_URL` environment variable
- Updated `.env.development` and `.env.production` files
- Gateway now handles all API routing instead of direct service communication

### 2. API Client Updates
- **File**: `src/services/apiClient.ts`
- Changed base URL from `${API_BASE_URL}/api` to `${GATEWAY_BASE_URL}`
- Updated token refresh endpoint to use gateway path
- All API requests now go through the gateway instead of direct service endpoints

### 3. Service Endpoint Updates

#### Case Service (`src/services/caseService.ts`)
- Updated all endpoints from `/Case/*` to `/cases/*`
- Endpoints now match gateway routing patterns

#### File Upload Service (`src/services/fileUploadService.ts`)
- Updated upload endpoint from `/fileupload/upload` to `/documents/upload`
- Updated delete endpoint from `/fileupload/{id}` to `/documents/{id}`
- Updated case creation endpoint from `/case/create-with-files` to `/cases/create-with-files`

#### Auth Service (`src/services/authService.ts`)
- No changes needed - endpoints already match gateway routing (`/auth/*`)

#### Admin Service (`src/services/adminService.ts`)
- No changes needed - endpoints already match gateway routing (`/admin/*`)

### 4. Gateway Routing Patterns
The API Gateway routes requests as follows:
- `/auth/*` → User Service
- `/admin/*` → User Service  
- `/cases/*` → Case Service
- `/documents/*` → Case Service
- `/analysis/*` → Case Service
- `/research/*` → Case Service

## Testing

### Local Development
1. Start the API Gateway: `dotnet run` in `BetterCallSaul.Gateway/`
2. Start the frontend: `npm run dev` in `better-call-saul-frontend/`
3. Verify connectivity at `http://localhost:5173`

### Production Deployment
- Environment variables are configured for Azure deployment
- Gateway URL points to production API endpoint
- All services communicate through the gateway

## Benefits

1. **Centralized API Management**: All requests go through a single gateway
2. **Service Discovery**: Gateway handles routing to appropriate microservices
3. **Authentication**: JWT validation centralized at gateway level
4. **Load Balancing**: Gateway can distribute traffic across service instances
5. **Monitoring**: Centralized logging and monitoring through gateway

## Error Handling
- The frontend maintains existing error handling patterns
- Gateway returns appropriate HTTP status codes for service failures
- Token refresh and authentication flows remain unchanged

## Backward Compatibility
- All existing frontend functionality is preserved
- API contracts remain the same from frontend perspective
- Only the routing mechanism has changed

## Deployment Notes
- Ensure API Gateway is deployed and running before frontend
- Update environment variables for different deployment environments
- Verify CORS configuration in gateway allows frontend origins