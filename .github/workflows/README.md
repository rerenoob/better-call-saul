# GitHub Actions Workflows

This directory contains automated CI/CD workflows for the Better Call Saul AI Lawyer application.

## Workflows

### 🔧 Build and Test (`build-and-test.yml`)
Validates code quality for both backend and frontend.

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` branch

**Purpose:**
- Backend .NET build and unit tests
- Frontend TypeScript type checking and build validation

### 🚀 Unified Deployment (`unified-deployment.yml`)
Deploys all components to AWS with intelligent path-based triggers.

**Triggers:**
- Push to `main` branch (detects which components changed)
- Manual dispatch with component selection

**Components:**
- Backend API to AWS ECS Fargate
- Frontend to AWS S3 + CloudFront
- Marketing site to AWS S3

**Requirements:**
- `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` secrets configured

### 🎨 Static Sites Deployment (`deploy-static-sites.yml`)
Deploys frontend and marketing sites with pull request previews.

**Triggers:**
- Push to `main` branch (frontend/marketing files changed)
- Pull requests (preview deployments)
- Manual dispatch

**Purpose:**
- Frontend to AWS S3 + CloudFront
- Marketing site to AWS S3
- Preview deployments for PRs

**Requirements:**
- `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` secrets configured

## Quick Setup

1. **Add AWS Secrets to GitHub Repository:**
   - Go to Settings → Secrets and variables → Actions
   - Add `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`

2. **Push to main branch:**
   ```bash
   git add .
   git commit -m "Set up GitHub Actions for automated deployment"
   git push origin main
   ```

3. **Monitor Deployments:**
   - Check the Actions tab in your GitHub repository
   - View deployment logs and status

## Manual Trigger

You can manually trigger deployments:
1. Go to Actions tab
2. Select the workflow
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

For detailed setup instructions, see [AWS_CONFIGURATION.md](../../AWS_CONFIGURATION.md).