# Better Call Saul - Deployment Guide

## Overview

This guide covers deploying the Better Call Saul application to production environments.

## Prerequisites

- Azure subscription
- GitHub account
- .NET 8 SDK
- Node.js 18+

## Azure Resource Setup

### 1. Create Resource Group
```bash
az group create --name better-call-saul-rg --location eastus
```

### 2. Create SQL Database
```bash
az sql server create --name bcs-sql-server --resource-group better-call-saul-rg --location eastus --admin-user adminuser --admin-password "StrongPassword123!"
az sql db create --name BetterCallSaulDb --server bcs-sql-server --resource-group better-call-saul-rg --service-objective S1
```

### 3. Create App Service
```bash
az appservice plan create --name bcs-app-plan --resource-group better-call-saul-rg --sku P1v2 --is-linux
az webapp create --name bcs-api --resource-group better-call-saul-rg --plan bcs-app-plan --runtime "DOTNET:8"
```

### 4. Create Static Web App
```bash
az staticwebapp create --name bcs-frontend --resource-group better-call-saul-rg --source https://github.com/your-username/better-call-saul --branch main --app-location "better-call-saul-frontend" --output-location "dist"
```

## Configuration

### Backend Settings
```json
{
  "ConnectionStrings:DefaultConnection": "Server=tcp:bcs-sql-server.database.windows.net,1433;Database=BetterCallSaulDb;...",
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

### Frontend Environment
```env
VITE_API_BASE_URL=https://bcs-api.azurewebsites.net
VITE_SIGNALR_HUB_URL=https://bcs-api.azurewebsites.net/hubs
```

## CI/CD Pipeline

### Backend Deployment
```yaml
name: Deploy Backend
on: [push]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with: { dotnet-version: '8.0.x' }
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --configuration Release
    - name: Test
      run: dotnet test
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'bcs-api'
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: ./publish
```

### Frontend Deployment
```yaml
name: Deploy Frontend
on: [push]
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with: { node-version: '18' }
    - name: Install dependencies
      run: cd better-call-saul-frontend && npm ci
    - name: Build
      run: cd better-call-saul-frontend && npm run build
    - name: Deploy
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_SWA_TOKEN }}
        app_location: "better-call-saul-frontend"
        output_location: "dist"
```

## Database Migration

### Apply Migrations
```bash
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API --connection "ProductionConnectionString"
```

### Generate SQL Script
```bash
dotnet ef migrations script --output migrations.sql
```

## Manual Deployment

### Backend
```bash
cd BetterCallSaul.API
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip --resource-group better-call-saul-rg --name bcs-api --src ./publish.zip
```

### Frontend
```bash
cd better-call-saul-frontend
npm ci
npm run build
# Upload dist/ folder to web host
```

## Monitoring

### Application Insights
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Checks
```csharp
builder.Services.AddHealthChecks().AddSqlServer(connectionString);
app.MapHealthChecks("/health");
```

## Security

### Key Vault Integration
```csharp
builder.Configuration.AddAzureKeyVault(new Uri("https://your-vault.vault.azure.net/"), new DefaultAzureCredential());
```

### SSL Configuration
```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

## Troubleshooting

### Common Issues
- Verify database connection string
- Check CORS configuration
- Validate JWT secret key
- Confirm Azure service permissions

### Logs
```bash
az webapp log tail --name bcs-api --resource-group better-call-saul-rg
```