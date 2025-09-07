# Better Call Saul - Deployment Guide

This document provides instructions for setting up automated deployment using GitHub Actions to deploy the Better Call Saul AI Lawyer application to Azure.

## Architecture Overview

- **Backend API**: ASP.NET Core Web API deployed to Azure App Service
- **Frontend**: React TypeScript SPA deployed to Azure Static Web Apps
- **Database**: Azure SQL Database
- **CI/CD**: GitHub Actions for automated deployment

## Azure Resources

### Existing Resources
- **Resource Group**: `bettercallsaul-rg`
- **App Service**: `bettercallsaul-api` (https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net)
- **Static Web App**: `bettercallsaul-app` (https://orange-island-0a659d210.1.azurestaticapps.net)
- **SQL Server**: `bettercallsaul-db-server.database.windows.net`
- **SQL Database**: `bettercallsaul-db`

## GitHub Actions Setup

### Required Secrets

To enable automated deployment, add the following secrets to your GitHub repository:

#### 1. Azure Service Principal Credentials
**Secret Name**: `AZURE_CREDENTIALS`

**Value** (JSON format - replace with your actual service principal credentials):
```json
{
  "clientId": "YOUR_CLIENT_ID",
  "clientSecret": "YOUR_CLIENT_SECRET",
  "subscriptionId": "YOUR_SUBSCRIPTION_ID", 
  "tenantId": "YOUR_TENANT_ID",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

#### 2. Static Web Apps Deployment Token
**Secret Name**: `AZURE_STATIC_WEB_APPS_API_TOKEN_ORANGE_ISLAND_0A659D210`

**Value**:
```
YOUR_STATIC_WEB_APPS_DEPLOYMENT_TOKEN
```

### How to Add Secrets to GitHub

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with the exact name and value specified above

## Deployment Workflows

### Backend API Deployment (`.github/workflows/deploy-backend.yml`)

**Triggers**:
- Push to `main` branch with changes to backend files
- Manual workflow dispatch

**Process**:
1. Setup .NET 8.0 environment
2. Restore dependencies
3. Build solution
4. Run tests
5. Publish API project
6. Deploy to Azure App Service

### Frontend Deployment (`.github/workflows/deploy-frontend.yml`)

**Triggers**:
- Push to `main` branch with changes to frontend files
- Pull request events
- Manual workflow dispatch

**Process**:
1. Setup Node.js 18 environment
2. Install dependencies
3. Build React application with production API URL
4. Deploy to Azure Static Web Apps

## Database Configuration

The API is configured with the following connection string (stored as App Service connection string):
```
Server=tcp:YOUR_SQL_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DATABASE;User ID=YOUR_SQL_ADMIN;Password=YOUR_SQL_PASSWORD;Encrypt=true;TrustServerCertificate=False;Connection Timeout=30;
```

## Environment Variables

### Backend API (App Service Settings)
- `ASPNETCORE_ENVIRONMENT`: Production
- `DefaultConnection`: SQL Database connection string (configured as connection string)

### Frontend (Static Web App Settings)
- `VITE_API_BASE_URL`: https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net

## Security Configuration

- **SQL Server**: Public access enabled with Azure services firewall rule
- **App Service**: HTTPS enforced, secure headers configured
- **Static Web App**: HTTPS enforced, connected to Azure AD for authentication

## Testing the Deployment

### API Health Check
```bash
curl https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net/api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-09-07T16:13:36.2240735Z"
}
```

### Frontend Access
Visit: https://orange-island-0a659d210.1.azurestaticapps.net

## Troubleshooting

### Common Issues

1. **Build Failures**: Check .NET SDK version and dependencies
2. **Database Connection Issues**: Verify SQL Server firewall rules
3. **Frontend API Calls**: Ensure CORS is configured and API URL is correct
4. **Authentication**: Verify service principal permissions and secret expiry

### Logs Access
- **App Service Logs**: Azure Portal → App Service → Monitoring → Log stream
- **GitHub Actions Logs**: GitHub repository → Actions tab → Select workflow run

## Manual Deployment Commands

If needed, you can deploy manually using Azure CLI:

### Backend
```bash
dotnet publish BetterCallSaul.API/BetterCallSaul.API.csproj -c Release -o ./publish
az webapp deploy --resource-group bettercallsaul-rg --name bettercallsaul-api --src-path ./publish.zip --type zip
```

### Frontend
```bash
cd better-call-saul-frontend
npm run build
# Manual upload via Azure Portal or SWA CLI
```

## Service Principal Details

**Service Principal**: `bettercallsaul-github-actions`
- **Role**: Contributor
- **Scope**: `/subscriptions/892354e3-0fa5-4e41-9ff3-6a0d42c64d23/resourceGroups/bettercallsaul-rg`

**Note**: Store credentials securely and rotate them periodically for security best practices.