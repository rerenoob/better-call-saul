# IAM User and Group Setup for GitHub CI/CD

## ✅ Successfully Created IAM Resources

### 1. IAM Group
- **Group Name**: `GitHubCICD`
- **Group ID**: `AGPA5YZKCXORGRP3TI5PE`
- **ARN**: `arn:aws:iam::946591677346:group/GitHubCICD`
- **Created**: 2025-09-22T20:49:21+00:00

### 2. IAM Policy
- **Policy Name**: `BetterCallSaulGitHubCICD`
- **Policy ID**: `ANPA5YZKCXORDEVIVF4YY`
- **ARN**: `arn:aws:iam::946591677346:policy/BetterCallSaulGitHubCICD`
- **Description**: Policy for Better Call Saul GitHub CI/CD deployment

### 3. IAM User
- **User Name**: `github-actions-bettercallsaul`
- **User ID**: `AIDA5YZKCXORNZOPPAUYD`
- **ARN**: `arn:aws:iam::946591677346:user/github-actions-bettercallsaul`
- **Tags**:
  - Purpose: GitHubCICD
  - Project: BetterCallSaul

### 4. Access Keys (For GitHub Secrets)
- **Access Key ID**: `[Generated - Set in GitHub Secrets]`
- **Secret Access Key**: `[Generated - Set in GitHub Secrets]`
- **Status**: Active

## 🔐 Permissions Granted

The IAM policy provides the following scoped permissions:

### ECR (Elastic Container Registry)
- Authentication token retrieval
- Image push/pull operations
- Repository: `arn:aws:ecr:us-east-1:946591677346:repository/bettercallsaul-api`

### ECS (Elastic Container Service)
- Task definition management
- Service updates
- Cluster operations
- Resources:
  - Cluster: `arn:aws:ecs:us-east-1:946591677346:cluster/bettercallsaul-cluster-production`
  - Service: `arn:aws:ecs:us-east-1:946591677346:service/bettercallsaul-cluster-production/bettercallsaul-api`
  - Task Definition: `arn:aws:ecs:us-east-1:946591677346:task-definition/bettercallsaul-api:*`

### S3 (Simple Storage Service)
- Frontend deployment operations
- Bucket: `arn:aws:s3:::better-call-saul-frontend-production`

### CloudFront
- Cache invalidation
- Distribution: `arn:aws:cloudfront::946591677346:distribution/E31UK0DRGK6P2Y`

### IAM (Limited)
- Pass role permissions for ECS tasks
- Roles: `better-call-saul-infrastructure-ECSExecutionRole-*` and `better-call-saul-infrastructure-ECSTaskRole-*`

### CloudWatch Logs
- Log group operations
- Log Group: `/ecs/bettercallsaul-api-production`

### Minimal Read Permissions
- `sts:GetCallerIdentity` for identity verification

## 🔄 Update GitHub Repository Secrets

**Replace the old AWS credentials in your GitHub repository with these new ones:**

### Go to GitHub Repository Settings
1. Navigate to your repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Update or create these secrets:

```
AWS_ACCESS_KEY_ID: [Your Generated Access Key ID]
AWS_SECRET_ACCESS_KEY: [Your Generated Secret Access Key]
```

## 🛡️ Security Best Practices

### What Makes This Secure:
1. **Principle of Least Privilege**: Only permissions needed for deployment
2. **Resource-Specific**: Permissions scoped to your specific resources
3. **No Admin Access**: No broad administrative permissions
4. **Dedicated User**: Separate from personal/administrative accounts

### Resource Boundaries:
- ECR: Only your `bettercallsaul-api` repository
- ECS: Only your production cluster and service
- S3: Only your frontend bucket
- CloudFront: Only your distribution
- IAM: Only pass role for your ECS roles

## 🧪 Testing the Setup

### Verify Permissions Work:
```bash
# Test ECR access (using your credentials)
export AWS_ACCESS_KEY_ID=[Your Access Key ID]
export AWS_SECRET_ACCESS_KEY=[Your Secret Access Key]
export AWS_DEFAULT_REGION=us-east-1

# Test commands
aws sts get-caller-identity
aws ecr describe-repositories --repository-names bettercallsaul-api
aws ecs describe-clusters --clusters bettercallsaul-cluster-production
aws s3 ls s3://better-call-saul-frontend-production
```

## 🚀 Ready for Deployment

Your GitHub Actions workflows will now use these dedicated credentials with:
- ✅ Secure, scoped permissions
- ✅ No admin access
- ✅ Resource-specific boundaries
- ✅ Production-ready security

## 🔄 Credential Rotation

**Important**: These credentials should be rotated periodically for security:

```bash
# When rotating (future):
aws iam create-access-key --user-name github-actions-bettercallsaul
# Update GitHub secrets with new key
aws iam delete-access-key --user-name github-actions-bettercallsaul --access-key-id [OLD_ACCESS_KEY_ID]
```

## 📋 Summary

- ✅ IAM Group: `GitHubCICD` created
- ✅ IAM Policy: `BetterCallSaulGitHubCICD` with minimal permissions
- ✅ IAM User: `github-actions-bettercallsaul` in group
- ✅ Access Keys: Generated for GitHub Secrets
- ✅ Permissions: Scoped to your infrastructure only

**Next Step**: Update GitHub repository secrets with the new AWS credentials!