# GitHub Deployment Setup Guide

This guide will help you configure your GitHub repository for automatic deployment to your newly created AWS infrastructure.

## 🏗️ Infrastructure Overview

Your AWS infrastructure has been successfully deployed and includes:

- **ECS Cluster**: `bettercallsaul-cluster-production`
- **ECS Service**: `bettercallsaul-api`
- **ECR Repository**: `946591677346.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api`
- **S3 Bucket**: `better-call-saul-frontend-production`
- **CloudFront Distribution**: `E31UK0DRGK6P2Y`

## 🔐 Required GitHub Repository Secrets

You need to configure the following secrets in your GitHub repository settings:

### 1. AWS Credentials
```
AWS_ACCESS_KEY_ID: [Your AWS Access Key ID]
AWS_SECRET_ACCESS_KEY: [Your AWS Secret Access Key]
```

### 2. Frontend Configuration
```
API_BASE_URL: https://[ECS-ALB-URL]/api
```
*Note: You'll get the actual ECS service URL after the first deployment*

### 3. Optional Secrets (Already configured in workflows)
- `CLOUDFRONT_DISTRIBUTION_ID`: E31UK0DRGK6P2Y (hardcoded in workflow)

## 📝 How to Add GitHub Secrets

1. **Navigate to Your Repository**:
   - Go to your GitHub repository
   - Click on **Settings** tab
   - Click on **Secrets and variables** → **Actions**

2. **Add Each Secret**:
   - Click **New repository secret**
   - Enter the secret name (exactly as shown above)
   - Enter the secret value
   - Click **Add secret**

## 🚀 Deployment Workflows

Your repository now has two main deployment workflows:

### Backend Deployment (`deploy-backend-aws.yml`)
**Triggers:**
- Push to `main` branch with changes to backend files
- Manual workflow dispatch

**What it does:**
1. Builds and tests the .NET API
2. Creates Docker image
3. Pushes to ECR
4. Updates ECS service

### Frontend Deployment (`deploy-frontend-aws.yml`)
**Triggers:**
- Push to `main` branch with changes to frontend files
- Manual workflow dispatch
- Pull request events

**What it does:**
1. Builds React application
2. Deploys to S3
3. Invalidates CloudFront cache

## 🔧 First Deployment Steps

### Step 1: Configure Secrets
Set up all the required GitHub secrets as listed above.

### Step 2: Test Backend Deployment
```bash
# Make a small change to trigger deployment
echo "# Deployment test" >> BetterCallSaul.API/README.md
git add .
git commit -m "test: trigger backend deployment"
git push origin main
```

### Step 3: Test Frontend Deployment
```bash
# Make a small change to trigger frontend deployment
echo "# Deployment test" >> better-call-saul-frontend/README.md
git add .
git commit -m "test: trigger frontend deployment"
git push origin main
```

### Step 4: Monitor Deployments
1. Go to **Actions** tab in your GitHub repository
2. Watch the workflow runs
3. Check for any errors and resolve them

## 🔍 Getting the API Base URL

After your first backend deployment succeeds:

1. **Get ECS Service URL**:
   ```bash
   aws ecs describe-services \
     --cluster bettercallsaul-cluster-production \
     --services bettercallsaul-api \
     --region us-east-1 \
     --query 'services[0].loadBalancers[0].targetGroupArn'
   ```

2. **Alternative - Check ECS Console**:
   - Go to AWS ECS Console
   - Find your service
   - Look for the public IP or load balancer URL

3. **Update API_BASE_URL Secret**:
   - Update the `API_BASE_URL` secret in GitHub
   - Use format: `https://[your-ecs-public-ip]` or ALB URL

## 🐛 Troubleshooting

### Common Issues:

1. **ECS Service Fails to Start**:
   - Check CloudWatch logs: `/ecs/bettercallsaul-api-production`
   - Verify environment variables in task definition
   - Check Docker image exists in ECR

2. **Frontend Build Fails**:
   - Check `API_BASE_URL` is correctly set
   - Verify Node.js version compatibility
   - Check for TypeScript errors

3. **S3 Deployment Fails**:
   - Verify S3 bucket name: `better-call-saul-frontend-production`
   - Check AWS credentials have S3 permissions

4. **CloudFront Not Updating**:
   - Invalidation should happen automatically
   - Check CloudFront distribution ID: `E31UK0DRGK6P2Y`

### Debug Commands:

```bash
# Check ECS service status
aws ecs describe-services --cluster bettercallsaul-cluster-production --services bettercallsaul-api --region us-east-1

# Check ECR repository
aws ecr describe-repositories --repository-names bettercallsaul-api --region us-east-1

# Check S3 bucket
aws s3 ls s3://better-call-saul-frontend-production

# Check CloudWatch logs
aws logs describe-log-streams --log-group-name /ecs/bettercallsaul-api-production --region us-east-1
```

## 🎯 Next Steps After Setup

1. **Configure NoSQL Connection**: Update production connection strings for DocumentDB
2. **Set up Monitoring**: Configure CloudWatch alarms and dashboards
3. **Security Review**: Rotate JWT secrets and review IAM permissions
4. **Performance Testing**: Load test your deployed application
5. **Backup Strategy**: Configure automated backups for your data

## 📞 Support

If you encounter issues:
1. Check GitHub Actions logs for detailed error messages
2. Review AWS CloudWatch logs for runtime errors
3. Verify all secrets are correctly configured
4. Ensure your AWS account has necessary permissions

---

**Your AWS infrastructure is ready for deployment! 🚀**

Infrastructure Stack: `better-call-saul-infrastructure`
Account ID: `946591677346`
Region: `us-east-1`