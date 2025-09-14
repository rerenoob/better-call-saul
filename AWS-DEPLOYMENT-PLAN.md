# AWS Deployment Plan for Better Call Saul Application

## Overview
Deploy the .NET 8 Web API backend and React TypeScript frontend to AWS using cloud-native services. The application already supports AWS provider selection with Bedrock AI, S3 storage, and Textract document processing.

## Architecture Components

### 1. Database Layer
**AWS RDS PostgreSQL**
- Instance: `db.t3.micro` (production can scale to `db.t3.small` or higher)
- Engine: PostgreSQL 15.4
- Storage: 20GB GP2 (encrypted)
- Multi-AZ: Enabled for production
- Backup retention: 7 days
- Security: VPC with restricted security groups

### 2. Backend API (.NET 8)
**AWS Elastic Beanstalk**
- Platform: `.NET Core on Linux`
- Instance type: `t3.micro` (can scale up)
- Load balancer: Application Load Balancer (ALB)
- Auto Scaling: 1-3 instances
- Health checks: `/health` endpoint
- SSL termination at load balancer

### 3. Frontend (React TypeScript)
**AWS S3 + CloudFront**
- S3 bucket: Static website hosting
- CloudFront: Global CDN with HTTPS
- Custom domain support with Route 53
- Automatic compression and caching

### 4. File Storage
**AWS S3**
- Bucket: `better-call-saul-documents-946591677346`
- Versioning enabled
- Server-side encryption (AES-256)
- Lifecycle policies for cost optimization
- CORS configuration for frontend uploads

### 5. Secrets Management
**AWS Systems Manager Parameter Store**
- JWT secret key (SecureString)
- Database connection string (SecureString)
- Third-party API keys (SecureString)
- Application configuration (String parameters)

### 6. AI Services (Already Configured)
- **AWS Bedrock**: Claude AI for case analysis
- **AWS Textract**: Document processing and OCR
- **AWS S3**: Document storage integration

## Deployment Steps

### Phase 1: Core Infrastructure
```bash
# 1. Create RDS PostgreSQL database
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

# 2. Create S3 bucket for documents
aws s3 mb s3://better-call-saul-documents-946591677346 \
  --region us-east-1

# 3. Create S3 bucket for frontend
aws s3 mb s3://better-call-saul-frontend-946591677346 \
  --region us-east-1

# 4. Enable S3 bucket versioning
aws s3api put-bucket-versioning \
  --bucket better-call-saul-documents-946591677346 \
  --versioning-configuration Status=Enabled

# 5. Enable S3 bucket encryption
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

### Phase 2: Security & Configuration
```bash
# 1. Store secrets in Parameter Store
aws ssm put-parameter \
  --name "/bettercallsaul/jwt-secret" \
  --value "YOUR_JWT_SECRET_KEY_32_CHARS_MIN" \
  --type "SecureString" \
  --description "JWT secret key for Better Call Saul application"

aws ssm put-parameter \
  --name "/bettercallsaul/db-connection" \
  --value "Server=bettercallsaul-db.REGION.rds.amazonaws.com,5432;Database=bettercallsaul;User Id=bettercallsaul_admin;Password=BetterCallSaul2024!DB;SSL Mode=Require;" \
  --type "SecureString" \
  --description "Database connection string for Better Call Saul"

# 2. Configure S3 bucket CORS for document uploads
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

# 3. Configure S3 bucket policy for application access
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
```

### Phase 3: Application Deployment
```bash
# 1. Deploy backend to Elastic Beanstalk
# First, install EB CLI if not already installed
pip install awsebcli

# Initialize Elastic Beanstalk application
cd /home/dpham/Projects/better-call-saul
eb init bettercallsaul-api \
  --platform ".NET Core on Linux" \
  --region us-east-1

# Create production environment
eb create production \
  --instance-type t3.micro \
  --envvars ASPNETCORE_ENVIRONMENT=Production,CLOUD_PROVIDER=AWS,AWS_DEFAULT_REGION=us-east-1

# 2. Deploy frontend to S3 and configure CloudFront
cd better-call-saul-frontend
npm run build

aws s3 sync ./dist s3://better-call-saul-frontend-946591677346 \
  --delete

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

## Environment Variables for Production

### Elastic Beanstalk Environment Variables
```bash
# Set environment variables via EB CLI
eb setenv \
  ASPNETCORE_ENVIRONMENT=Production \
  CLOUD_PROVIDER=AWS \
  AWS_DEFAULT_REGION=us-east-1 \
  ConnectionStrings__DefaultConnection="$(aws ssm get-parameter --name /bettercallsaul/db-connection --with-decryption --query Parameter.Value --output text)" \
  JWT_SECRET_KEY="$(aws ssm get-parameter --name /bettercallsaul/jwt-secret --with-decryption --query Parameter.Value --output text)"
```

### Application Configuration Updates
The app is already configured to use AWS services when `CLOUD_PROVIDER=AWS`:
- **AI Service**: AWS Bedrock (Claude AI)
- **Storage**: AWS S3
- **Document Processing**: AWS Textract

## Security Configuration

### 1. IAM Roles and Policies
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

### 2. Network Security
- RDS in private subnets with security group allowing only EB instances
- Security groups restricting database access to application tier
- ALB with SSL certificates from AWS Certificate Manager
- CloudFront with security headers and WAF (optional)

### 3. Data Protection
- RDS encryption at rest
- S3 bucket encryption (AES-256)
- Parameter Store SecureString for sensitive data
- HTTPS enforcement everywhere
- VPC with private subnets for database

## Cost Estimation (Monthly)
- **RDS PostgreSQL (db.t3.micro)**: ~$13
- **Elastic Beanstalk (t3.micro)**: ~$8.50
- **Application Load Balancer**: ~$16
- **S3 Storage (50GB)**: ~$1.15
- **CloudFront (1TB transfer)**: ~$85
- **Parameter Store**: Free tier
- **NAT Gateway**: ~$32 (if using private subnets)
- **Total**: ~$155/month

## Monitoring & Logging
- **CloudWatch**: Application logs and metrics
- **AWS X-Ray**: Request tracing (optional)
- **RDS Enhanced Monitoring**: Database performance
- **Application Load Balancer**: Access logs to S3
- **CloudFront**: Access logs and real-time metrics

## Backup & Disaster Recovery
- **RDS Automated Backups**: 7-day retention with point-in-time recovery
- **S3 Cross-Region Replication**: For document storage (optional)
- **Infrastructure as Code**: CloudFormation templates for reproducible deployments
- **Application Deployment**: Blue/Green deployments via Elastic Beanstalk
- **Database Snapshots**: Manual snapshots before major updates

## Scaling Considerations
- **Auto Scaling**: Configure EB to scale from 1-5 instances based on CPU/memory
- **Database Scaling**: Start with db.t3.micro, upgrade to db.t3.small or higher as needed
- **CDN**: CloudFront automatically scales globally
- **Storage**: S3 scales automatically with usage

## Security Best Practices
- Enable AWS CloudTrail for audit logging
- Use AWS Config for compliance monitoring
- Implement AWS WAF on CloudFront for web application firewall
- Regular security updates via Elastic Beanstalk platform updates
- Rotate database passwords and JWT secrets regularly

This plan leverages AWS's managed services for scalability, security, and cost-effectiveness while maintaining the application's existing multi-cloud architecture support.

## Next Steps
1. Review and approve this deployment plan
2. Execute Phase 1 (Core Infrastructure)
3. Execute Phase 2 (Security & Configuration)
4. Execute Phase 3 (Application Deployment)
5. Configure monitoring and alerts
6. Set up CI/CD pipeline for automated deployments