# GitHub Actions Workflows

This directory contains automated deployment workflows for the Better Call Saul AI Lawyer application.

## Workflows

### 🔧 Backend API Deployment (`deploy-backend.yml`)
Deploys the .NET Web API to Azure App Service.

**Triggers:**
- Push to `main` branch (backend files changed)
- Manual dispatch

**Requirements:**
- `AZURE_CREDENTIALS` secret configured

### 🎨 Frontend Deployment (`deploy-frontend.yml`)
Deploys the React frontend to Azure Static Web Apps.

**Triggers:**
- Push to `main` branch (frontend files changed)
- Pull requests
- Manual dispatch

**Requirements:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN_ORANGE_ISLAND_0A659D210` secret configured

## Quick Setup

1. **Add Secrets to GitHub Repository:**
   - Go to Settings → Secrets and variables → Actions
   - Add the two required secrets (see DEPLOYMENT.md for values)

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

For detailed setup instructions, see [DEPLOYMENT.md](../../DEPLOYMENT.md).