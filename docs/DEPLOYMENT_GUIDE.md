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

### AWS Backend Deployment (ECS Fargate)
```yaml
name: Deploy Backend API to AWS
on:
  push:
    branches: [ main ]
    paths:
      - 'BetterCallSaul.API/**'
      - 'BetterCallSaul.Core/**'
      - 'BetterCallSaul.Infrastructure/**'
      - 'BetterCallSaul.Tests/**'
      - 'BetterCallSaul.sln'
      - '.github/workflows/deploy-backend-aws.yml'
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v4
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with: { dotnet-version: '8.0.x' }
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --configuration Release --no-restore
    - name: Run tests
      run: dotnet test --no-restore --verbosity normal
    - name: Build Docker image
      run: docker build -t bettercallsaul-api:latest .
    - name: Login to Amazon ECR
      uses: aws-actions/amazon-ecr-login@v2
    - name: Push Docker image to ECR
      run: docker push ${{ steps.login-ecr.outputs.registry }}/bettercallsaul-api:latest
    - name: Deploy to ECS
      uses: aws-actions/amazon-ecs-deploy-task-definition@v1
      with:
        task-definition: .aws/task-definition.json
        service: bettercallsaul-api
        cluster: bettercallsaul-cluster
        wait-for-service-stability: true
```

### AWS Frontend Deployment (S3 + CloudFront)
```yaml
name: Deploy Frontend to AWS S3 + CloudFront
on:
  push:
    branches: [ main ]
    paths:
      - 'better-call-saul-frontend/**'
      - '.github/workflows/deploy-frontend-aws.yml'
  workflow_dispatch:

jobs:
  build_and_deploy_job:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with: { node-version: '18' }
      - name: Install dependencies
        run: cd better-call-saul-frontend && npm ci
      - name: Build application
        run: cd better-call-saul-frontend && npm run build
        env:
          VITE_API_BASE_URL: ${{ secrets.API_BASE_URL }}
      - name: Deploy to S3
        run: aws s3 sync better-call-saul-frontend/dist/ s3://better-call-saul-frontend --delete
      - name: Invalidate CloudFront cache
        run: aws cloudfront create-invalidation --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} --paths "/*"
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

### AWS Infrastructure Setup
```bash
# Deploy CloudFormation stack
cd .aws
chmod +x deploy-infrastructure.sh
./deploy-infrastructure.sh

# Create S3 bucket for frontend
aws s3 mb s3://better-call-saul-frontend --region us-east-1

# Create ECR repository for backend
aws ecr create-repository --repository-name bettercallsaul-api --region us-east-1
```

### Backend (ECS Fargate)
```bash
# Build and push Docker image
docker build -t bettercallsaul-api:latest .
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker tag bettercallsaul-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest

# Deploy to ECS
aws ecs update-service --cluster bettercallsaul-cluster --service bettercallsaul-api --force-new-deployment --region us-east-1
```

### Frontend (S3 + CloudFront)
```bash
cd better-call-saul-frontend
npm ci
npm run build
aws s3 sync dist/ s3://better-call-saul-frontend --delete
aws cloudfront create-invalidation --distribution-id <distribution-id> --paths "/*"
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