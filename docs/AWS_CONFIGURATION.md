# AWS Configuration Guide

## Overview

Better Call Saul uses AWS services for production deployments to provide scalable AI analysis, document storage, and text extraction capabilities. This guide covers the required AWS configuration for production environments.

## Required AWS Services

### 1. Amazon Bedrock
**Purpose**: AI analysis and legal document processing
- **Required IAM Permissions**:
  - `bedrock:InvokeModel`
  - `bedrock:ListFoundationModels`
- **Configuration**:
  - Region: `us-east-1` (default)
  - Model ID: `anthropic.claude-v2` (default)

### 2. Amazon S3
**Purpose**: Document storage and retrieval
- **Required IAM Permissions**:
  - `s3:GetObject`
  - `s3:PutObject`
  - `s3:DeleteObject`
  - `s3:ListBucket`
  - `s3:GetBucketLocation`
- **Configuration**:
  - Bucket Name: `better-call-saul-prod` (default)
  - Region: `us-east-1` (default)

### 3. Amazon Textract
**Purpose**: Document text extraction
- **Required IAM Permissions**:
  - `textract:DetectDocumentText`
  - `textract:AnalyzeDocument`
- **Configuration**:
  - Region: `us-east-1` (default)

## Environment Variables

### Required Environment Variables
```bash
# AWS Credentials (Required for production)
AWS_ACCESS_KEY_ID=your-access-key-id
AWS_SECRET_ACCESS_KEY=your-secret-access-key

# AWS Region (Optional, defaults to us-east-1)
AWS_REGION=us-east-1
```

### Optional Environment Variables
```bash
# Service-specific overrides
AWS_BEDROCK_MODEL_ID=anthropic.claude-v2
AWS_S3_BUCKET_NAME=better-call-saul-prod
```

## IAM Policy Requirements

Create an IAM user with the following policy:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "bedrock:InvokeModel",
                "bedrock:ListFoundationModels"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:PutObject",
                "s3:DeleteObject",
                "s3:ListBucket",
                "s3:GetBucketLocation"
            ],
            "Resource": [
                "arn:aws:s3:::better-call-saul-prod",
                "arn:aws:s3:::better-call-saul-prod/*"
            ]
        },
        {
            "Effect": "Allow",
            "Action": [
                "textract:DetectDocumentText",
                "textract:AnalyzeDocument"
            ],
            "Resource": "*"
        }
    ]
}
```

## AWS Resource Setup

### 1. Create S3 Bucket
```bash
aws s3 mb s3://better-call-saul-prod --region us-east-1
```

### 2. Configure CORS for S3 Bucket
Create `cors.json`:
```json
{
  "CORSRules": [
    {
      "AllowedOrigins": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
      "AllowedHeaders": ["*"],
      "ExposeHeaders": []
    }
  ]
}
```

Apply CORS configuration:
```bash
aws s3api put-bucket-cors --bucket better-call-saul-prod --cors-configuration file://cors.json
```

### 3. Enable Bedrock Model Access
```bash
aws bedrock list-foundation-models --region us-east-1
# Ensure anthropic.claude-v2 is available in your account
```

## Configuration Validation

The application validates AWS configuration at startup in production environments:

1. **AWS Credentials**: Validates that `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` are set
2. **AWS Region**: Validates region configuration (defaults to `us-east-1` if not specified)
3. **Service Configuration**: Validates service-specific configuration through appsettings

### Error Messages

- **Missing AWS credentials**: "AWS credentials are required for production environment. Set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables."
- **Missing configuration**: Service-specific error messages for missing S3 bucket, Bedrock model, etc.

## Cost Considerations

### Estimated Monthly Costs (US-East-1)

| Service | Usage Tier | Estimated Cost |
|---------|------------|----------------|
| **Amazon Bedrock** | 1,000 requests/month | ~$8.00 |
| **Amazon S3** | 50GB storage + 10,000 requests | ~$1.50 |
| **Amazon Textract** | 1,000 pages/month | ~$15.00 |
| **Total** | | **~$24.50** |

### Service Limits

- **Bedrock**: 1000 requests/minute (default)
- **Textract**: 10 pages/second (default)
- **S3**: 3500 PUT/COPY/POST/DELETE and 5500 GET/HEAD requests/second

## Regional Considerations

- **US-East-1 (N. Virginia)**: Recommended for best service availability and lowest latency
- **Other Regions**: Supported but may require manual configuration updates
- **Data Residency**: Ensure compliance with local data protection regulations

## Troubleshooting

### Common Issues

1. **Missing Permissions**: Verify IAM user has all required permissions
2. **Region Mismatch**: Ensure all services are configured for the same region
3. **Bucket Not Found**: Verify S3 bucket exists and is accessible
4. **Model Access**: Ensure Bedrock model access is enabled in your AWS account

### Debug Logs

Enable debug logging by setting:
```bash
ASPNETCORE_ENVIRONMENT=Development
# or
Serilog:MinimumLevel:Default=Debug
```

## Security Best Practices

1. **IAM Roles**: Use IAM roles instead of access keys when possible
2. **Least Privilege**: Grant only necessary permissions
3. **Key Rotation**: Rotate access keys regularly
4. **Bucket Policies**: Implement restrictive bucket policies
5. **Encryption**: Enable SSE-S3 encryption for S3 buckets

## Backup and Recovery

### S3 Backup
```bash
# Enable versioning
aws s3api put-bucket-versioning --bucket better-call-saul-prod --versioning-configuration Status=Enabled

# Enable cross-region replication (optional)
aws s3api put-bucket-replication --bucket better-call-saul-prod --replication-configuration file://replication.json
```

### Configuration Backup
Regularly backup:
- IAM policies and users
- S3 bucket configurations
- Service configuration settings