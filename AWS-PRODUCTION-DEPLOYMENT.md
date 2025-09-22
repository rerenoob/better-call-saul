# Better Call Saul - AWS Production Deployment Guide

## Overview

This comprehensive guide covers the complete AWS infrastructure setup and deployment process for the Better Call Saul application. The application is designed to leverage AWS services for AI analysis, document storage, and text extraction in production environments.

## Architecture Overview

### Core AWS Services
- **Backend API**: AWS Elastic Beanstalk (.NET 8 Web API)
- **Frontend**: AWS S3 + CloudFront (React TypeScript)
- **SQL Database**: Amazon RDS PostgreSQL (Case metadata, users, audit logs)
- **NoSQL Database**: Amazon DocumentDB (Case documents, analysis results, legal research)
- **File Storage**: Amazon S3
- **AI Services**: Amazon Bedrock (Claude AI)
- **Document Processing**: Amazon Textract
- **Secrets Management**: AWS Systems Manager Parameter Store

### Network Architecture
```
CloudFront (CDN) → S3 (Frontend) → ALB → Elastic Beanstalk → RDS PostgreSQL (SQL)
                                              ↓
                                          DocumentDB (NoSQL)
                                          S3 (Documents)
                                          Bedrock (AI)
                                          Textract (OCR)
```

## Prerequisites

### AWS Account Requirements
- AWS account with appropriate permissions
- IAM user with programmatic access
- AWS CLI configured locally
- Required service quotas and limits

### Application Requirements
- .NET 8 SDK for backend
- Node.js 18+ for frontend
- Git for version control

## Phase 1: Core Infrastructure Setup

### 1.1 Create RDS PostgreSQL Database (SQL)

```bash
# Create RDS PostgreSQL instance
aws rds create-db-instance \
  --db-instance-identifier bettercallsaul-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 15.4 \
  --master-username bettercallsaul_admin \
  --master-user-password 'BetterCallSaul2024!DB' \
  --allocated-storage 20 \
  --storage-type gp2 \
  --vpc-security-group-ids default \
  --db-name bettercallsaul \
  --backup-retention-period 7 \
  --multi-az false \
  --publicly-accessible true \
  --storage-encrypted \
  --deletion-protection false

# Wait for database to be available
aws rds wait db-instance-available --db-instance-identifier bettercallsaul-db

# Get database endpoint
DB_ENDPOINT=$(aws rds describe-db-instances --db-instance-identifier bettercallsaul-db --query 'DBInstances[0].Endpoint.Address' --output text)
echo "Database endpoint: $DB_ENDPOINT"
```

### 1.2 Create Amazon DocumentDB Cluster (NoSQL)

```bash
# Create DocumentDB cluster
aws docdb create-db-cluster \
  --db-cluster-identifier bettercallsaul-nosql \
  --engine docdb \
  --master-username docdbadmin \
  --master-user-password 'BetterCallSaul2024!NoSQL' \
  --vpc-security-group-ids default \
  --backup-retention-period 7 \
  --storage-encrypted

# Create cluster instances
aws docdb create-db-instance \
  --db-instance-identifier bettercallsaul-nosql-1 \
  --db-cluster-identifier bettercallsaul-nosql \
  --db-instance-class db.t3.medium \
  --engine docdb

aws docdb create-db-instance \
  --db-instance-identifier bettercallsaul-nosql-2 \
  --db-cluster-identifier bettercallsaul-nosql \
  --db-instance-class db.t3.medium \
  --engine docdb

# Wait for cluster to be available
aws docdb wait db-cluster-available --db-cluster-identifier bettercallsaul-nosql

# Get DocumentDB endpoint
DOCDB_ENDPOINT=$(aws docdb describe-db-clusters --db-cluster-identifier bettercallsaul-nosql --query 'DBClusters[0].Endpoint' --output text)
DOCDB_PORT=$(aws docdb describe-db-clusters --db-cluster-identifier bettercallsaul-nosql --query 'DBClusters[0].Port' --output text)
echo "DocumentDB endpoint: $DOCDB_ENDPOINT:$DOCDB_PORT"
```

### 1.3 Create S3 Buckets

```bash
# Create document storage bucket
aws s3 mb s3://better-call-saul-documents-946591677346 --region us-east-1

# Create frontend hosting bucket
aws s3 mb s3://better-call-saul-frontend-946591677346 --region us-east-1

# Enable versioning for document bucket
aws s3api put-bucket-versioning \
  --bucket better-call-saul-documents-946591677346 \
  --versioning-configuration Status=Enabled

# Enable encryption for document bucket
aws s3api put-bucket-encryption \
  --bucket better-call-saul-documents-946591677346 \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'
```

### 1.4 Verify AWS Service Access

```bash
# Verify Bedrock access
aws bedrock list-foundation-models --region us-east-1

# Verify Textract access
aws textract list-adapters --region us-east-1

# Verify S3 bucket creation
aws s3 ls
```

## Phase 2: Security & Configuration

### 2.1 Store Secrets in Parameter Store

```bash
# Generate secure JWT secret
JWT_SECRET=$(openssl rand -base64 32)

# Store JWT secret
aws ssm put-parameter \
  --name "/bettercallsaul/jwt-secret" \
  --value "$JWT_SECRET" \
  --type "SecureString" \
  --description "JWT secret key for Better Call Saul application"

# Store database connection string
DB_CONNECTION="Server=$DB_ENDPOINT;Database=bettercallsaul;User Id=bettercallsaul_admin;Password=BetterCallSaul2024!DB;SSL Mode=Require;"

aws ssm put-parameter \
  --name "/bettercallsaul/db-connection" \
  --value "$DB_CONNECTION" \
  --type "SecureString" \
  --description "Database connection string for Better Call Saul"

# Store DocumentDB connection string
DOCDB_CONNECTION="mongodb://docdbadmin:BetterCallSaul2024!NoSQL@$DOCDB_ENDPOINT:$DOCDB_PORT/bettercallsaul?tls=true&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false"

aws ssm put-parameter \
  --name "/bettercallsaul/documentdb-connection" \
  --value "$DOCDB_CONNECTION" \
  --type "SecureString" \
  --description "DocumentDB connection string for Better Call Saul NoSQL data"

# Store application configuration
aws ssm put-parameter \
  --name "/bettercallsaul/environment" \
  --value "Production" \
  --type "String" \
  --description "Application environment"
```

### 2.2 Configure S3 Bucket Policies

```bash
# Configure CORS for document uploads
aws s3api put-bucket-cors \
  --bucket better-call-saul-documents-946591677346 \
  --cors-configuration '{
    "CORSRules": [{
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
      "AllowedOrigins": ["https://your-frontend-domain.com", "http://localhost:5173"],
      "ExposeHeaders": ["ETag"],
      "MaxAgeSeconds": 3000
    }]
  }'

# Configure bucket policy for application access
aws s3api put-bucket-policy \
  --bucket better-call-saul-documents-946591677346 \
  --policy '{
    "Version": "2012-10-17",
    "Statement": [{
      "Sid": "AllowApplicationAccess",
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::946591677346:role/aws-elasticbeanstalk-ec2-role"
      },
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::better-call-saul-documents-946591677346/*"
    }]
  }'

### 2.3 Configure DocumentDB Security Group

```bash
# Get VPC security group ID for DocumentDB
DOCDB_SG_ID=$(aws docdb describe-db-clusters --db-cluster-identifier bettercallsaul-nosql --query 'DBClusters[0].VpcSecurityGroups[0].VpcSecurityGroupId' --output text)

# Allow Elastic Beanstalk instances to access DocumentDB
aws ec2 authorize-security-group-ingress \
  --group-id $DOCDB_SG_ID \
  --protocol tcp \
  --port $DOCDB_PORT \
  --source-group $(aws ec2 describe-security-groups --group-names aws-elasticbeanstalk-ec2-role --query 'SecurityGroups[0].GroupId' --output text)
```

### 2.4 Create IAM Policy for Application

```bash
# Create IAM policy for Elastic Beanstalk EC2 instances
aws iam create-policy \
  --policy-name BetterCallSaulAppPolicy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [
      {
        "Effect": "Allow",
        "Action": [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject"
        ],
        "Resource": [
          "arn:aws:s3:::better-call-saul-documents-946591677346/*"
        ]
      },
      {
        "Effect": "Allow",
        "Action": [
          "ssm:GetParameter",
          "ssm:GetParameters"
        ],
        "Resource": [
          "arn:aws:ssm:us-east-1:946591677346:parameter/bettercallsaul/*"
        ]
      },
      {
        "Effect": "Allow",
        "Action": [
          "docdb:DescribeDBClusters",
          "docdb:DescribeDBInstances"
        ],
        "Resource": [
          "arn:aws:docdb:us-east-1:946591677346:cluster:bettercallsaul-nosql",
          "arn:aws:docdb:us-east-1:946591677346:db:bettercallsaul-nosql-*"
        ]
      },
      {
        "Effect": "Allow",
        "Action": [
          "bedrock:InvokeModel"
        ],
        "Resource": "*"
      },
      {
        "Effect": "Allow",
        "Action": [
          "textract:AnalyzeDocument",
          "textract:DetectDocumentText"
        ],
        "Resource": "*"
      }
    ]
  }'

# Attach policy to Elastic Beanstalk EC2 role
aws iam attach-role-policy \
  --role-name aws-elasticbeanstalk-ec2-role \
  --policy-arn arn:aws:iam::946591677346:policy/BetterCallSaulAppPolicy
```

## Phase 3: Application Deployment

### 3.1 Deploy Backend to Elastic Beanstalk

```bash
# Install EB CLI if not already installed
pip install awsebcli

# Initialize Elastic Beanstalk application
cd /home/dpham/Projects/better-call-saul
eb init bettercallsaul-api \
  --platform ".NET Core on Linux" \
  --region us-east-1

# Create production environment
eb create production \
  --instance-type t3.micro \
  --envvars \
    ASPNETCORE_ENVIRONMENT=Production,\
    CLOUD_PROVIDER=AWS,\
    AWS_DEFAULT_REGION=us-east-1

# Set environment variables from Parameter Store
eb setenv \
  ConnectionStrings__DefaultConnection="$(aws ssm get-parameter --name /bettercallsaul/db-connection --with-decryption --query Parameter.Value --output text)" \
  JWT_SECRET_KEY="$(aws ssm get-parameter --name /bettercallsaul/jwt-secret --with-decryption --query Parameter.Value --output text)"

# Deploy application
eb deploy
```

### 3.2 Deploy Frontend to S3 and CloudFront

```bash
# Build frontend application
cd better-call-saul-frontend
npm ci
npm run build

# Get backend API URL from Elastic Beanstalk
API_URL=$(eb status production --region us-east-1 | grep "CNAME" | awk '{print $2}')
echo "Backend API URL: $API_URL"

# Update frontend environment variables
echo "VITE_API_BASE_URL=https://$API_URL" > .env.production
echo "VITE_SIGNALR_HUB_URL=https://$API_URL/hubs" >> .env.production

# Rebuild with production environment
npm run build

# Deploy to S3
aws s3 sync ./dist s3://better-call-saul-frontend-946591677346 --delete

# Enable static website hosting
aws s3api put-bucket-website \
  --bucket better-call-saul-frontend-946591677346 \
  --website-configuration '{
    "IndexDocument": {"Suffix": "index.html"},
    "ErrorDocument": {"Key": "index.html"}
  }'

# Create CloudFront distribution
aws cloudfront create-distribution \
  --distribution-config '{
    "CallerReference": "better-call-saul-'$(date +%s)'",
    "Comment": "Better Call Saul Frontend Distribution",
    "DefaultCacheBehavior": {
      "TargetOriginId": "S3-better-call-saul-frontend",
      "ViewerProtocolPolicy": "redirect-to-https",
      "TrustedSigners": {"Enabled": false, "Quantity": 0},
      "ForwardedValues": {"QueryString": false, "Cookies": {"Forward": "none"}}
    },
    "Origins": {
      "Quantity": 1,
      "Items": [{
        "Id": "S3-better-call-saul-frontend",
        "DomainName": "better-call-saul-frontend-946591677346.s3.amazonaws.com",
        "S3OriginConfig": {
          "OriginAccessIdentity": ""
        }
      }]
    },
    "Enabled": true
  }'
```

## Phase 4: Database Migration & Initialization

### 4.1 Apply SQL Database Migrations

```bash
# Get database connection string from Parameter Store
DB_CONNECTION=$(aws ssm get-parameter --name /bettercallsaul/db-connection --with-decryption --query Parameter.Value --output text)

# Apply migrations
dotnet ef database update \
  --project BetterCallSaul.Infrastructure \
  --startup-project BetterCallSaul.API \
  --connection "$DB_CONNECTION"

# Verify migration success
dotnet ef migrations list \
  --project BetterCallSaul.Infrastructure \
  --startup-project BetterCallSaul.API
```

### 4.2 Initialize DocumentDB Collections

```bash
# Get DocumentDB connection string from Parameter Store
DOCDB_CONNECTION=$(aws ssm get-parameter --name /bettercallsaul/documentdb-connection --with-decryption --query Parameter.Value --output text)

# Initialize DocumentDB collections using MongoDB shell
# Note: You'll need to install MongoDB shell locally or use AWS CloudShell
mongo "$DOCDB_CONNECTION" --eval '
  // Create collections with proper indexes
  db.createCollection("casedocuments");
  db.createCollection("legalresearch");
  
  // Create indexes for optimal query performance
  db.casedocuments.createIndex({ "CaseId": 1 }, { unique: true });
  db.casedocuments.createIndex({ "UserId": 1 });
  db.casedocuments.createIndex({ "CreatedAt": -1 });
  db.casedocuments.createIndex({ "Metadata.Status": 1 });
  
  db.legalresearch.createIndex({ "Citation": 1 }, { unique: true });
  db.legalresearch.createIndex({ "Title": "text", "FullText": "text" });
  db.legalresearch.createIndex({ "IndexedAt": -1 });
  
  print("DocumentDB collections initialized successfully");
'

# Verify collections were created
mongo "$DOCDB_CONNECTION" --eval '
  print("Collections in database:");
  db.getCollectionNames().forEach(function(collection) {
    print(" - " + collection);
  });
  
  print("\nIndexes in casedocuments:");
  db.casedocuments.getIndexes().forEach(function(index) {
    print(" - " + JSON.stringify(index.key));
  });
'
```

### 4.3 Seed Initial Data

```bash
# Generate registration codes
./scripts/manage-registration-codes.sh seed 100 365 "System" "Initial production deployment"

# Verify database connectivity
dotnet run --project BetterCallSaul.API -- --check-db
```

## Phase 5: Configuration Validation

### 5.1 Validate AWS Configuration

```bash
# Test AWS service connectivity
aws s3 ls s3://better-call-saul-documents-946591677346/
aws bedrock list-foundation-models --region us-east-1
aws textract list-adapters --region us-east-1

# Test application endpoints
API_URL=$(eb status production --region us-east-1 | grep "CNAME" | awk '{print $2}')
curl -s https://$API_URL/health | jq .
curl -s https://$API_URL/api/cases | jq .

# Test NoSQL connectivity through API
curl -s https://$API_URL/api/cases/test-nosql | jq .
```

### 5.2 DocumentDB Connectivity Validation

```bash
# Test DocumentDB connectivity from application
API_URL=$(eb status production --region us-east-1 | grep "CNAME" | awk '{print $2}')

# Test NoSQL health endpoint
curl -s https://$API_URL/health/nosql | jq .

# Test case document operations
curl -s https://$API_URL/api/cases/test-document | jq .

# Verify DocumentDB metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/DocDB \
  --metric-name CPUUtilization \
  --dimensions Name=DBClusterIdentifier,Value=bettercallsaul-nosql \
  --start-time $(date -u -d '5 minutes ago' +%Y-%m-%dT%H:%M:%SZ) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
  --period 300 \
  --statistics Average
```

### 5.3 Environment Variable Validation

```bash
# Verify all required environment variables are set
eb printenv production

# Expected environment variables:
# - ASPNETCORE_ENVIRONMENT=Production
# - CLOUD_PROVIDER=AWS
# - AWS_DEFAULT_REGION=us-east-1
# - ConnectionStrings__DefaultConnection
# - ConnectionStrings__DocumentDB
# - JWT_SECRET_KEY
```

## Application Configuration

### Backend Configuration (appsettings.Production.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "YOUR_APPLICATION_INSIGHTS_CONNECTION_STRING"
        }
      }
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "{{from Parameter Store}}",
    "DocumentDB": "{{from Parameter Store}}"
  },
  "JwtSettings": {
    "SecretKey": "{{from Parameter Store}}",
    "Issuer": "BetterCallSaul.API",
    "Audience": "BetterCallSaul.Client",
    "ExpiryMinutes": 60
  },
  "AWS": {
    "Bedrock": { "Region": "us-east-1", "ModelId": "anthropic.claude-v2" },
    "S3": { "BucketName": "better-call-saul-documents-946591677346", "Region": "us-east-1" },
    "Textract": { "Region": "us-east-1" },
    "DocumentDB": { "ClusterId": "bettercallsaul-nosql", "Region": "us-east-1" }
  },
  "NoSqlSettings": {
    "ConnectionString": "{{from Parameter Store}}",
    "DatabaseName": "bettercallsaul",
    "UseTls": true
  }
}
```

### Frontend Configuration (.env.production)

```env
VITE_API_BASE_URL=https://your-eb-environment.elasticbeanstalk.com
VITE_SIGNALR_HUB_URL=https://your-eb-environment.elasticbeanstalk.com/hubs
VITE_ENVIRONMENT=production
```

## Monitoring and Logging

### 5.1 Configure CloudWatch Logs

```bash
# Enable detailed logging in Elastic Beanstalk
eb config save production --cfg production-config
eb config put production --cfg production-config \
  --options '{
    "aws:elasticbeanstalk:cloudwatch:logs": {
      "StreamLogs": "true",
      "RetentionInDays": "7"
    }
  }'
```

### 5.2 Set Up Health Checks

```bash
# Configure health check endpoint
eb config save production --cfg health-check-config
eb config put production --cfg health-check-config \
  --options '{
    "aws:elasticbeanstalk:environment:process:default": {
      "HealthCheckPath": "/health",
      "MatcherHTTPCode": "200"
    }
  }'
```

## Cost Management

### Estimated Monthly Costs (US-East-1)

| Service | Usage Tier | Estimated Cost |
|---------|------------|----------------|
| **Amazon RDS** | db.t3.micro, 20GB storage | ~$13.00 |
| **Amazon DocumentDB** | db.t3.medium x2, 20GB storage | ~$60.00 |
| **Elastic Beanstalk** | t3.micro instance | ~$8.50 |
| **Application Load Balancer** | Standard usage | ~$16.00 |
| **Amazon S3** | 50GB storage + 10K requests | ~$1.50 |
| **CloudFront** | 1TB data transfer | ~$85.00 |
| **Amazon Bedrock** | 1,000 requests/month | ~$8.00 |
| **Amazon Textract** | 1,000 pages/month | ~$15.00 |
| **Total** | | **~$207.00** |

## Security Best Practices

### 1. Network Security
- RDS in private subnets with security groups
- DocumentDB in private subnets with security groups
- ALB with SSL certificates from AWS Certificate Manager
- CloudFront with security headers
- VPC with restricted access
- DocumentDB TLS encryption enabled

### 2. Data Protection
- RDS encryption at rest
- DocumentDB encryption at rest
- S3 bucket encryption (AES-256)
- Parameter Store SecureString for sensitive data
- HTTPS enforcement everywhere
- DocumentDB TLS connections required

### 3. Access Control
- IAM roles with least privilege
- Regular key rotation
- Audit logging with CloudTrail
- WAF implementation for web protection

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify RDS instance is running
   - Check security group rules
   - Validate connection string

2. **S3 Access Issues**
   - Verify bucket policies
   - Check IAM role permissions
   - Validate CORS configuration

3. **DocumentDB Connectivity**
   - Verify DocumentDB cluster is running
   - Check security group rules
   - Validate TLS certificate trust
   - Test connection from application

4. **Bedrock Model Access**
   - Ensure model access is enabled
   - Check region compatibility
   - Verify IAM permissions

5. **Application Deployment**
   - Check Elastic Beanstalk logs
   - Verify environment variables
   - Test health check endpoints (SQL + NoSQL)

### Debug Commands

```bash
# Check Elastic Beanstalk logs
eb logs production

# Check RDS status
aws rds describe-db-instances --db-instance-identifier bettercallsaul-db

# Check DocumentDB status
aws docdb describe-db-clusters --db-cluster-identifier bettercallsaul-nosql
aws docdb describe-db-instances --filters Name=db-cluster-id,Values=bettercallsaul-nosql

# Check S3 bucket contents
aws s3 ls s3://better-call-saul-documents-946591677346/

# Test application health
curl https://your-api-endpoint/health
curl https://your-api-endpoint/health/nosql
```

## Backup and Recovery

### Database Backups
- RDS automated backups (7-day retention)
- DocumentDB automated backups (7-day retention)
- Manual snapshots before major updates
- Point-in-time recovery capability for both databases

### Application Data
- S3 versioning enabled
- Cross-region replication (optional)
- Regular backup validation

### Configuration Backup
- Parameter Store configuration exports
- Elastic Beanstalk configuration saves
- IAM policy documentation

## Next Steps

1. **Execute Phase 1** (Core Infrastructure)
2. **Execute Phase 2** (Security & Configuration)  
3. **Execute Phase 3** (Application Deployment)
4. **Execute Phase 4** (Database Migration)
5. **Execute Phase 5** (Configuration Validation)
6. **Set up monitoring and alerts**
7. **Configure CI/CD pipeline** for automated deployments

This consolidated guide provides a complete roadmap for deploying the Better Call Saul application to AWS production environment with proper security, monitoring, and cost management practices.
```