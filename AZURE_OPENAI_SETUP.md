# Azure OpenAI Setup Guide

This guide explains how to configure Azure OpenAI for the Better Call Saul application.

## Prerequisites

- Azure account with access to OpenAI services
- Azure subscription with OpenAI quota approved

## Step 1: Create Azure OpenAI Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Azure OpenAI"
4. Click "Create"
5. Fill in the details:
   - **Subscription**: Your Azure subscription
   - **Resource group**: Create new or select existing
   - **Region**: Choose a region near your users
   - **Name**: Unique name for your resource
   - **Pricing tier**: Select appropriate tier
6. Click "Review + create" then "Create"

## Step 2: Get Configuration Values

1. Navigate to your Azure OpenAI resource
2. Go to "Keys and Endpoint" section
3. Copy:
   - **Endpoint**: The URL (e.g., `https://your-resource.openai.azure.com/`)
   - **API Key**: One of the two available keys

## Step 3: Create Model Deployment

1. Go to "Model deployments" section
2. Click "Create new deployment"
3. Select model:
   - Recommended: "gpt-4"
   - Alternative: "gpt-35-turbo"
4. Enter deployment name (e.g., "gpt-4")
5. Click "Create"

## Step 4: Configure Application

### Option A: Development (appsettings.Development.json)

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "MaxTokens": 1000,
    "Temperature": 0.3
  }
}
```

### Option B: Production (Environment Variables)

```bash
# Set these environment variables
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
AzureOpenAI__ApiKey=your-actual-api-key
AzureOpenAI__DeploymentName=gpt-4
AzureOpenAI__MaxTokens=1000
AzureOpenAI__Temperature=0.3
```

### Option C: Docker Compose

```yaml
environment:
  - AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
  - AzureOpenAI__ApiKey=your-api-key
  - AzureOpenAI__DeploymentName=gpt-4
```

## Step 5: Verify Configuration

1. Start the application: `dotnet run`
2. Check logs for Azure OpenAI initialization message
3. Upload a test document to verify AI analysis works

## Configuration Options

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| Endpoint | Azure OpenAI endpoint URL | - | Yes |
| ApiKey | Azure OpenAI API key | - | Yes |
| DeploymentName | Model deployment name | "gpt-4" | Yes |
| MaxTokens | Maximum response tokens | 1000 | No |
| Temperature | Response creativity (0.0-1.0) | 0.3 | No |

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Check API key and endpoint
2. **404 Not Found**: Verify deployment name exists
3. **429 Too Many Requests**: Rate limiting - wait and retry
4. **Service not available**: Check if configuration is loaded

### Debug Steps

1. Check application logs for configuration warnings
2. Verify environment variables are set correctly
3. Test Azure OpenAI access using curl:
   ```bash
   curl -X POST "https://your-resource.openai.azure.com/openai/deployments/gpt-4/chat/completions?api-version=2024-02-15-preview" \
     -H "api-key: YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     -d '{"messages":[{"role":"user","content":"Hello"}],"max_tokens":5}'
   ```

## Security Best Practices

- Never commit API keys to version control
- Use Azure Key Vault for production secrets
- Rotate API keys regularly
- Monitor usage and set up alerts
- Use private endpoints for enhanced security

## Cost Management

- Monitor usage in Azure Cost Management
- Set up budget alerts
- Use appropriate model tiers for your needs
- Consider caching responses for repeated queries

## Support

- Azure OpenAI Documentation: https://learn.microsoft.com/azure/ai-services/openai/
- Azure Support: https://azure.microsoft.com/support/
- Application Issues: Check project README.md

---

This configuration enables the AI document analysis features in Better Call Saul. The application will gracefully handle missing configuration by showing appropriate error messages to users.