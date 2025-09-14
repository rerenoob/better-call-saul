# Better Call Saul - Deployment Guide

## Overview

This guide covers deploying the Better Call Saul application to production environments. The application is designed to be cloud-agnostic and can be deployed to various cloud providers or on-premises infrastructure.

## Prerequisites

- Cloud provider account (AWS, Azure, GCP, or other)
- GitHub account (for CI/CD)
- .NET 8 SDK
- Node.js 18+

## Cloud Resource Setup

### 1. Database Setup
Configure a SQL Server database (cloud-managed or self-hosted):
- **Database Name**: BetterCallSaulDb
- **Authentication**: SQL authentication or managed identity
- **Connection String**: Update in production configuration

### 2. Backend Hosting
Choose one of these options:
- **Cloud App Service** (Azure App Service, AWS Elastic Beanstalk, etc.)
- **Container Platform** (Kubernetes, Docker containers)
- **Virtual Machine** (Self-managed server)

### 3. Frontend Hosting
Deploy to static web hosting:
- **AWS**: S3 + CloudFront
- **Azure**: Static Web Apps
- **GCP**: Cloud Storage + Load Balancer
- **Netlify/Vercel**: Modern hosting platforms

### 4. File Storage
Configure object storage for production:
- **AWS S3**: Recommended bucket configuration
- **Azure Blob Storage**: Alternative option
- **Other**: Any S3-compatible storage

## Configuration

### Backend Settings
```json
{
  "ConnectionStrings:DefaultConnection": "Server=your-database-server;Database=BetterCallSaulDb;User Id=username;Password=password;TrustServerCertificate=true;",
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

### AWS Configuration (Required for Production)
```bash
# Required environment variables for AWS services
AWS_ACCESS_KEY_ID=your-access-key-id
AWS_SECRET_ACCESS_KEY=your-secret-access-key
AWS_REGION=us-east-1

# Optional service-specific overrides
AWS_BEDROCK_MODEL_ID=anthropic.claude-v2
AWS_S3_BUCKET_NAME=better-call-saul-prod
```

For detailed AWS setup instructions, see [AWS_CONFIGURATION.md](./AWS_CONFIGURATION.md)

### Frontend Environment
```env
VITE_API_BASE_URL=https://your-api-domain.com
VITE_SIGNALR_HUB_URL=https://your-api-domain.com/hubs
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
    - name: Deploy to Cloud
      # Replace with your cloud provider's deployment action
      # Example for AWS: uses: aws-actions/...
      # Example for Azure: uses: azure/webapps-deploy@v2
      # Example for GCP: uses: google-github-actions/...
      run: echo "Configure your cloud deployment step here"
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
      # Replace with your static hosting deployment action
      # Example for AWS S3: uses: jakejarvis/s3-sync-action@v0.5
      # Example for Netlify: uses: netlify/actions/cli@master
      # Example for Vercel: uses: vercel/action@v20
      run: echo "Configure your static hosting deployment step here"
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

### Secrets Management
```csharp
// Use environment variables or your cloud provider's secrets manager
// Example for AWS: AWS Secrets Manager
// Example for Azure: Azure Key Vault  
// Example for GCP: Secret Manager
builder.Configuration.AddEnvironmentVariables();
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
- Confirm cloud service permissions

### Logs
```bash
# Use your cloud provider's logging tools
# AWS: aws logs tail /aws/lambda/your-function
# Azure: az webapp log tail
# GCP: gcloud logging read
```