# Better Call Saul - AI Lawyer Application

An AI-powered legal document analysis application built with .NET 8 Blazor Server that helps users analyze legal documents and get AI-generated insights.

## Project Overview

Better Call Saul is a web application that allows users to:
- Upload legal documents (PDF, DOCX, DOC, TXT, images)
- Extract text from uploaded documents
- Analyze documents using Azure OpenAI GPT-4
- Manage legal cases with AI-generated insights
- Track case status and analysis results

## Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (for production) or **SQLite** (for development)
- **Azure OpenAI Service** account with GPT-4 deployment
- **Node.js** (optional, for frontend tooling)

## Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd better-call-saul
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure environment variables**
   
   Create or update `appsettings.json` with your configuration:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BetterCallSaul;Trusted_Connection=true;MultipleActiveResultSets=true"
     },
     "AzureOpenAI": {
       "Endpoint": "https://your-resource.openai.azure.com/",
       "ApiKey": "your-api-key-here",
       "DeploymentName": "gpt-4",
       "MaxTokens": 1000,
       "Temperature": 0.3
     }
   }
   ```

## Database Setup

### Development (SQLite)
1. The application uses SQLite by default in development
2. Database file will be created automatically at `BetterCallSaul.db`
3. Apply migrations:
   ```bash
   dotnet ef database update
   ```

### Production (SQL Server)
1. Update connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=your-server;Database=BetterCallSaul;User Id=your-user;Password=your-password;"
   }
   ```
2. Apply migrations:
   ```bash
   dotnet ef database update
   ```

## Azure OpenAI Setup

1. **Create Azure OpenAI Resource**
   - Create an Azure OpenAI resource in Azure Portal
   - Deploy a GPT-4 model (recommended: "gpt-4")

2. **Configure API Settings**
   - Get your endpoint URL and API key from Azure Portal
   - Update `appsettings.json` with your credentials:
     ```json
     "AzureOpenAI": {
       "Endpoint": "https://your-resource.openai.azure.com/",
       "ApiKey": "your-api-key",
       "DeploymentName": "gpt-4",
       "MaxTokens": 1000,
       "Temperature": 0.3
     }
     ```

## Running the Application

### Development Mode
```bash
dotnet run
# or with hot reload
dotnet watch
```

The application will be available at:
- **HTTP**: http://localhost:5173
- **HTTPS**: https://localhost:7191

### Production Mode
```bash
dotnet publish -c Release
cd bin/Release/net8.0/publish
dotnet better-call-saul.dll
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |
| `ASPNETCORE_URLS` | Server URLs | `http://localhost:5173;https://localhost:7191` |

### App Settings

Key configuration sections in `appsettings.json`:

- **ConnectionStrings**: Database connection strings
- **FileStorage**: File upload settings (path, size limits, extensions)
- **AzureOpenAI**: AI service configuration
- **Logging**: Log level configuration

#### Azure OpenAI Configuration Details

The Azure OpenAI service requires the following configuration:

```json
"AzureOpenAI": {
  // REQUIRED: Azure OpenAI endpoint URL from Azure Portal
  // Format: https://{your-resource-name}.openai.azure.com/
  "Endpoint": "https://your-resource.openai.azure.com/",
  
  // REQUIRED: API key from Azure Portal (Keys and Endpoint section)
  "ApiKey": "your-api-key-here",
  
  // REQUIRED: Deployment name of your model
  // Create this in Azure Portal under "Model deployments"
  "DeploymentName": "gpt-4",
  
  // OPTIONAL: Maximum tokens for AI responses (default: 1000)
  "MaxTokens": 1000,
  
  // OPTIONAL: Temperature for response creativity (0.0-1.0, default: 0.3)
  "Temperature": 0.3
}
```

#### Environment Variables for Production

For production deployment, use environment variables instead of appsettings.json:

```bash
# Azure OpenAI Configuration
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
AzureOpenAI__ApiKey=your-actual-api-key
AzureOpenAI__DeploymentName=gpt-4
AzureOpenAI__MaxTokens=1000
AzureOpenAI__Temperature=0.3

# Database Configuration
ConnectionStrings__DefaultConnection=Server=your-server;Database=BetterCallSaul;User Id=user;Password=pass;

# File Storage
FileStorage__BasePath=/app/uploads
FileStorage__MaxFileSize=10000000
```

#### Getting Azure OpenAI Credentials

1. **Create Azure OpenAI Resource**:
   - Go to Azure Portal → Create Resource → Azure OpenAI
   - Choose your subscription, resource group, and region
   - Select appropriate pricing tier

2. **Get Endpoint and API Key**:
   - Navigate to your Azure OpenAI resource
   - Go to "Keys and Endpoint" section
   - Copy the endpoint URL and one of the API keys

3. **Create Model Deployment**:
   - Go to "Model deployments" section
   - Click "Create new deployment"
   - Select model (GPT-4 recommended)
   - Give it a deployment name (used in configuration)

#### Security Best Practices

- Never commit API keys to version control
- Use environment variables or Azure Key Vault in production
- Rotate API keys regularly
- Monitor usage and set up alerts for unusual activity

## Testing

### Running Tests
```bash
dotnet test
```

### Manual Testing
1. Register a new user account
2. Upload sample legal documents
3. Create test cases
4. Verify AI analysis results
5. Test all user flows

## Deployment

For comprehensive deployment instructions, please refer to the [DEPLOYMENT.md](DEPLOYMENT.md) file which includes detailed guides for:

- **Quick Start Guide** - Minimum viable deployment setup
- **Production Configuration** - Environment variables and settings
- **Database Setup** - SQL Server and Azure SQL configuration
- **Azure OpenAI Configuration** - Production setup and security
- **Multiple Deployment Methods**:
  - 🐳 Docker and Docker Compose
  - ☁️ Azure App Service
  - 🪟 IIS (Windows Server)
  - 🐧 Linux with Nginx
- **SSL Configuration** - HTTPS setup with Let's Encrypt
- **Monitoring & Logging** - Application Insights and health checks
- **Scaling** - Vertical and horizontal scaling strategies
- **Backup & Recovery** - Database and file backup procedures
- **Troubleshooting** - Common issues and solutions
- **Security Checklist** - Pre-deployment security review
- **Performance Optimization** - Database and application tuning

### Quick Deployment Options

**Docker Compose (Development)**
```bash
./deploy.sh docker
```

**Production Deployment**
```bash
./deploy.sh production
```

**Database Migrations**
```bash
./deploy.sh migrate
```

**Health Check**
```bash
./deploy.sh health
```

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify connection string format
   - Check SQL Server is running
   - Ensure database exists

2. **Azure OpenAI Errors**
   - Verify endpoint URL and API key
   - Check deployment name matches Azure portal
   - Ensure region compatibility

3. **File Upload Issues**
   - Check `wwwroot/uploads` directory exists and is writable
   - Verify file size limits in configuration

4. **Migration Errors**
   ```bash
   # Recreate database
   dotnet ef database drop
   dotnet ef database update
   ```

### Logs

- Application logs are written to console and debug output
- Check Azure App Service logs for production issues
- Enable detailed logging in `appsettings.Development.json`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes and add tests
4. Submit a pull request

### Development Guidelines
- Follow existing code style and patterns
- Add appropriate logging
- Include error handling
- Update documentation for new features

## Known Limitations

- Maximum file upload size: 10MB
- Supported file types: PDF, DOCX, DOC, TXT, JPG, JPEG, PNG
- Azure OpenAI rate limits apply
- SQLite recommended for development only
- No built-in email service (uses console output)

## Support

For issues and questions:
1. Check troubleshooting section above
2. Review application logs
3. Verify Azure OpenAI configuration
4. Ensure database connectivity

---

**Better Call Saul** - Making legal document analysis accessible to everyone through AI technology.