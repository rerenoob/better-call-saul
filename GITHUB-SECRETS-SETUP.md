# 🚀 Automated AWS Deployment Setup

## ✅ Complete CI/CD Pipeline Ready!

Your repository has **fully automated deployment** via GitHub Actions. No manual Docker commands needed!

## 🔐 Required Setup (One-Time)

### GitHub Repository Secrets
Go to your repository → **Settings** → **Secrets and variables** → **Actions**

Add these 2 secrets:
```
AWS_ACCESS_KEY_ID = your-aws-access-key-id
AWS_SECRET_ACCESS_KEY = your-aws-secret-access-key
```

That's it! 🎯

## What Happens Automatically on Push to Main

### **Backend Deployment** (`.github/workflows/deploy-backend-aws.yml`)
Triggers when you push changes to:
- `BetterCallSaul.API/**`
- `BetterCallSaul.Core/**`
- `BetterCallSaul.Infrastructure/**`
- `BetterCallSaul.Tests/**`
- `BetterCallSaul.sln`

**Process:**
1. ✅ Builds .NET application
2. ✅ Runs all tests
3. ✅ Builds Docker image
4. ✅ Pushes to ECR automatically (no manual login!)
5. ✅ Updates ECS task definition
6. ✅ Deploys to ECS cluster
7. ✅ Waits for service stability

### **Frontend Deployment** (`.github/workflows/deploy-frontend-aws.yml`)
Triggers when you push changes to:
- `better-call-saul-frontend/**`

**Process:**
1. ✅ Installs npm dependencies
2. ✅ Builds React app with production environment (`VITE_API_BASE_URL`)
3. ✅ Syncs to S3 bucket
4. ✅ Invalidates CloudFront cache
5. ✅ Frontend immediately available at https://d1c0215ar7cs56.cloudfront.net

## Current Deployment Status

### ✅ What's Already Working
- GitHub Actions workflows are configured and ready
- ECR repository exists and is accessible
- ECS cluster and ALB are running
- CloudFront distribution is active

### 🔧 What You Need to Do
1. **Add GitHub Secrets** (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
2. **Push to main branch** - deployment will happen automatically!

## How to Deploy Right Now

### Option 1: Automatic (Recommended) ✨
```bash
# Commit the latest fixes and push
git push origin main

# Watch the GitHub Actions in your repository's "Actions" tab
# Both backend and frontend will deploy automatically!
```

### Option 2: Manual Trigger
- Go to GitHub → Actions tab
- Select "Deploy Backend API to AWS" or "Deploy Frontend to AWS S3 + CloudFront"
- Click "Run workflow"

### Option 3: Manual Commands (if needed)
Follow the commands in `DEPLOY-COMMANDS.md` if you need to deploy manually.

## Monitoring Deployments

### GitHub Actions
- **Repository → Actions tab** - Monitor deployment progress
- **Real-time logs** for each step
- **Automatic rollback** if deployment fails

### AWS Resources
```bash
# Check backend deployment
aws ecs describe-services --cluster bettercallsaul-cluster-production --services bettercallsaul-api

# Check API health
curl http://bettercallsaul-alb-production-1289827668.us-east-1.elb.amazonaws.com/health

# View ECS logs
aws logs tail /ecs/bettercallsaul-api-production --follow
```

## Security Notes

- ✅ **ECR authentication** handled automatically by GitHub Actions
- ✅ **AWS credentials** stored securely in GitHub Secrets
- ✅ **No manual Docker login** required
- ✅ **Environment variables** injected at build time

## Next Steps

1. **Set up GitHub Secrets** (2 secrets needed)
2. **Push your changes** to main branch
3. **Watch automatic deployment** in GitHub Actions
4. **Verify application** at https://d1c0215ar7cs56.cloudfront.net

**That's it!** 🚀 Your deployment is now fully automated!