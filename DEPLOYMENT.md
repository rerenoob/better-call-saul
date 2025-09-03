# Deployment Guide - Better Call Saul AI Legal Assistant

This comprehensive guide covers production deployment of the Better Call Saul AI Lawyer application, including setup instructions, configuration requirements, and troubleshooting.

## Table of Contents
- [Quick Start](#quick-start)
- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [Database Configuration](#database-configuration)
- [Azure OpenAI Configuration](#azure-openai-configuration)
- [File Storage Configuration](#file-storage-configuration)
- [Deployment Methods](#deployment-methods)
- [SSL Configuration](#ssl-configuration)
- [Monitoring & Logging](#monitoring--logging)
- [Scaling](#scaling)
- [Backup & Recovery](#backup--recovery)
- [Troubleshooting](#troubleshooting)
- [Security Checklist](#security-checklist)
- [Performance Optimization](#performance-optimization)

## Quick Start

### Minimum Viable Deployment

1. **Set up database**: SQL Server with `BetterCallSaul` database
2. **Configure Azure OpenAI**: Create resource and get API credentials
3. **Deploy application**: Choose one method below
4. **Configure environment**: Set required environment variables
5. **Test deployment**: Verify health endpoints and functionality

### Environment Variables (Required)

```bash
# Database
ConnectionStrings__DefaultConnection=Server=your-db-server;Database=BetterCallSaul;User Id=user;Password=password;

# Azure OpenAI
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
AzureOpenAI__ApiKey=your-api-key
AzureOpenAI__DeploymentName=your-deployment-name

# File Storage
FileStorage__BasePath=/app/uploads
FileStorage__MaxFileSize=10000000

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443
```

## Prerequisites

### Hardware Requirements

| Resource | Minimum | Recommended | Notes |
|----------|---------|-------------|-------|
| **CPU** | 2 cores | 4 cores | For AI processing and document analysis |
| **RAM** | 4GB | 8GB+ | 8GB recommended for production workloads |
| **Storage** | 10GB | 50GB+ | Depends on document storage volume |
| **Network** | 100Mbps | 1Gbps | Stable connection for Azure OpenAI API calls |

### Software Requirements

- **.NET 8.0 Runtime** - Required for running the application
- **Database**:
  - SQL Server 2019+ (recommended for production)
  - Azure SQL Database (cloud alternative)
  - SQLite (development only, not for production)
- **Web Server**:
  - IIS (Windows Server)
  - Nginx (Linux)
  - Kestrel (built-in, with reverse proxy)
- **SSL Certificate** - Required for HTTPS in production
- **File System** - Read/write permissions for upload directory

### Azure Services Required

- **Azure OpenAI Service** - For AI document analysis
- **Azure Application Insights** (optional) - For monitoring and logging
- **Azure Key Vault** (recommended) - For secure secret management

## Environment Setup

### Production Configuration Files

#### appsettings.Production.json

This file provides production defaults but sensitive values should be set via environment variables:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore.DataProtection": "Warning",
      "better_call_saul": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=production-db-server;Database=BetterCallSaul;User Id=sa;Password=your-strong-password;TrustServerCertificate=true;"
  },
  "FileStorage": {
    "BasePath": "/app/uploads",
    "MaxFileSize": 10000000,
    "AllowedExtensions": [".pdf", ".docx", ".doc", ".txt", ".jpg", ".jpeg", ".png"]
  },
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "DeploymentName": "gpt-4",
    "MaxTokens": 1000,
    "Temperature": 0.3
  },
  "AllowedHosts": "*"
}
```

#### Environment Variables (Production)

For security, use environment variables instead of hardcoded values:

```bash
# Database Configuration
export ConnectionStrings__DefaultConnection="Server=db-server;Database=BetterCallSaul;User Id=appuser;Password=secure-password-123;TrustServerCertificate=true;"

# Azure OpenAI Configuration
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__ApiKey="your-actual-api-key-here"
export AzureOpenAI__DeploymentName="gpt-4"
export AzureOpenAI__MaxTokens="1000"
export AzureOpenAI__Temperature="0.3"

# File Storage Configuration
export FileStorage__BasePath="/app/uploads"
export FileStorage__MaxFileSize="10000000"

# ASP.NET Core Configuration
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://+:80;https://+:443"

# Optional: Application Insights
export ApplicationInsights__ConnectionString="InstrumentationKey=your-key"
```

### Environment Variables

Set these environment variables in production:

```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443

# Database
ConnectionStrings__DefaultConnection=Server=db-server;Database=BetterCallSaul;User Id=user;Password=pass;

# Azure OpenAI
AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
AzureOpenAI__ApiKey=your-api-key
AzureOpenAI__DeploymentName=gpt-4
AzureOpenAI__MaxTokens=1000
AzureOpenAI__Temperature=0.3

# File Storage
FileStorage__BasePath=/app/uploads
FileStorage__MaxFileSize=10000000
```

## Database Configuration

### Database Configuration

#### SQL Server Production Setup

1. **Create Production Database**
   ```sql
   -- Create database with appropriate file sizes
   CREATE DATABASE BetterCallSaul 
   ON PRIMARY 
   (NAME = BetterCallSaul_Data, 
    FILENAME = '/var/opt/mssql/data/BetterCallSaul.mdf',
    SIZE = 100MB, 
    MAXSIZE = UNLIMITED, 
    FILEGROWTH = 64MB)
   LOG ON 
   (NAME = BetterCallSaul_Log,
    FILENAME = '/var/opt/mssql/data/BetterCallSaul.ldf',
    SIZE = 50MB,
    MAXSIZE = 2GB,
    FILEGROWTH = 64MB);
   GO
   
   USE BetterCallSaul;
   GO
   
   -- Create dedicated application user (least privilege principle)
   CREATE LOGIN bettercallsaul WITH PASSWORD = 'StrongPassword123!';
   CREATE USER bettercallsaul FOR LOGIN bettercallsaul;
   
   -- Grant minimum required permissions
   ALTER ROLE db_datareader ADD MEMBER bettercallsaul;
   ALTER ROLE db_datawriter ADD MEMBER bettercallsaul;
   ALTER ROLE db_ddladmin ADD MEMBER bettercallsaul;
   
   -- Additional permissions for EF migrations if needed
   -- GRANT ALTER ON SCHEMA::dbo TO bettercallsaul;
   GO
   ```

2. **Apply Database Migrations**
   
   **Option A: Using Entity Framework Tools**
   ```bash
   # Install EF tools globally
   dotnet tool install --global dotnet-ef
   
   # Apply migrations with connection string
   dotnet ef database update --connection "Server=db-server;Database=BetterCallSaul;User Id=bettercallsaul;Password=StrongPassword123!;TrustServerCertificate=true;"
   ```
   
   **Option B: Generate SQL Script**
   ```bash
   # Generate migration SQL script
   dotnet ef migrations script --output migrations.sql
   
   # Execute script against production database
   sqlcmd -S db-server -d BetterCallSaul -U bettercallsaul -P "StrongPassword123!" -i migrations.sql
   ```
   
   **Option C: Programmatic Migration (for container deployments)**
   ```csharp
   // In Program.cs, ensure this line exists:
   app.MigrateDatabase();
   ```

3. **Verify Database Setup**
   ```sql
   -- Check that tables were created
   USE BetterCallSaul;
   SELECT name FROM sys.tables WHERE type = 'U';
   
   -- Verify user permissions
   EXEC sp_helprolemember 'db_datareader';
   EXEC sp_helprolemember 'db_datawriter';
   ```

#### Azure SQL Database Setup

1. **Create Azure SQL Database**
   - Choose appropriate service tier (Start with S1 for production)
   - Configure firewall rules to allow application server IP
   - Set connection policy to "Proxy" for better security

2. **Connection String Format**
   ```
   Server=tcp:your-server.database.windows.net,1433;Database=BetterCallSaul;User ID=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
   ```

3. **Security Best Practices**
   - Use Azure Active Directory authentication if possible
   - Enable Advanced Data Security
   - Configure threat detection
   - Set up auditing and monitoring

#### Database Performance Tuning

```sql
-- Create indexes for common queries
CREATE INDEX IX_Cases_CreatedAt ON Cases (CreatedAt);
CREATE INDEX IX_Cases_Status ON Cases (Status);
CREATE INDEX IX_Documents_CaseId ON Documents (CaseId);
CREATE INDEX IX_Documents_UploadedAt ON Documents (UploadedAt);

-- Set appropriate database settings
ALTER DATABASE BetterCallSaul SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE BetterCallSaul SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE BetterCallSaul SET QUERY_STORE = ON;
```

### Azure SQL Database

1. **Create Azure SQL Database**
   - Choose appropriate service tier
   - Configure firewall rules
   - Set connection policy

2. **Connection String Format**
   ```
   Server=tcp:your-server.database.windows.net,1433;Database=BetterCallSaul;User ID=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
   ```

## Azure OpenAI Configuration

### Production Setup

1. **Create Production Azure OpenAI Resource**
   - Use a dedicated resource for production
   - Choose appropriate region and pricing tier
   - Deploy GPT-4 or GPT-35-Turbo model
   - Go to Azure Portal → Create Resource → Azure OpenAI

2. **Get Configuration Values**
   - **Endpoint**: Found in "Keys and Endpoint" section (e.g., https://your-resource.openai.azure.com/)
   - **API Key**: One of the two keys in "Keys and Endpoint" section
   - **Deployment Name**: Name you gave your model deployment in "Model deployments" section

3. **Configure in Production**
   - Use environment variables for security:
     ```bash
     AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
     AzureOpenAI__ApiKey=your-actual-api-key-here
     AzureOpenAI__DeploymentName=your-deployment-name
     AzureOpenAI__MaxTokens=1000
     AzureOpenAI__Temperature=0.3
     ```
   - Or use Azure Key Vault for enhanced security

4. **Configure Network Security**
   - Set up virtual network if required
   - Configure private endpoint for enhanced security
   - Set up monitoring and alerts

5. **Rate Limiting and Quotas**
   - Monitor usage and adjust quotas as needed
   - Implement retry logic in application
   - Set up alerts for quota limits
   - Consider implementing client-side rate limiting

## Deployment Methods

### Method 1: Docker Deployment (Recommended)

#### Docker Quick Start

```bash
# Build and run with Docker Compose (development)
docker-compose up --build

# Or for production configuration
docker-compose -f docker-compose.yml -f docker-compose.production.yml up --build

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f app

# Stop services
docker-compose down
```

#### Docker Production Deployment

1. **Build the Docker image**
   ```bash
   docker build -t better-call-saul:latest .
   ```

2. **Run with environment variables**
   ```bash
   docker run -d \
     --name better-call-saul \
     -p 80:80 \
     -p 443:443 \
     -e ASPNETCORE_ENVIRONMENT=Production \
     -e ConnectionStrings__DefaultConnection="Server=db-server;Database=BetterCallSaul;User Id=user;Password=pass;" \
     -e AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
     -e AzureOpenAI__ApiKey="your-api-key" \
     -e AzureOpenAI__DeploymentName="gpt-4" \
     -v uploads-volume:/app/uploads \
     better-call-saul:latest
   ```

3. **Docker Swarm/Kubernetes**
   ```yaml
   # docker-stack.yml for swarm
   version: '3.8'
   services:
     app:
       image: better-call-saul:latest
       ports:
         - "80:80"
         - "443:443"
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
         - AzureOpenAI__Endpoint=${AZURE_OPENAI_ENDPOINT}
         - AzureOpenAI__ApiKey=${AZURE_OPENAI_API_KEY}
       volumes:
         - uploads-volume:/app/uploads
       deploy:
         replicas: 3
         update_config:
           parallelism: 1
           delay: 10s
         restart_policy:
           condition: on-failure
   ```

#### Docker Health Checks

The Dockerfile includes health checks that verify:
- Application is responsive on port 80
- Health endpoint returns 200 OK
- Container can accept connections

### Method 2: Azure App Service Deployment

#### Azure CLI Deployment

```bash
# Create resource group
az group create --name better-call-saul-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name better-call-saul-plan \
  --resource-group better-call-saul-rg \
  --sku B1 \
  --is-linux

# Create web app
az webapp create \
  --name better-call-saul-app \
  --resource-group better-call-saul-rg \
  --plan better-call-saul-plan \
  --runtime "DOTNETCORE:8.0"

# Configure environment variables
az webapp config appsettings set \
  --name better-call-saul-app \
  --resource-group better-call-saul-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=db-server;Database=BetterCallSaul;User Id=user;Password=pass;" \
    AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
    AzureOpenAI__ApiKey="your-api-key"

# Deploy from local build
az webapp up \
  --name better-call-saul-app \
  --resource-group better-call-saul-rg \
  --plan better-call-saul-plan \
  --runtime "DOTNETCORE:8.0" \
  --html
```

#### GitHub Actions CI/CD

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal

    - name: Publish
      run: dotnet publish -c Release -o ${{env.DOTNET_ROOT}}/myapp

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'better-call-saul-app'
        slot-name: 'Production'
        publish-profile: ${{ secrets.AZUREAPPSERVICE_PUBLISHPROFILE }}
        package: ${{env.DOTNET_ROOT}}/myapp
```

### Method 3: IIS Deployment (Windows Server)

#### IIS Setup

1. **Install Requirements**
   ```powershell
   # Install .NET 8 Hosting Bundle
   Invoke-WebRequest -Uri "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.0-windows-hosting-bundle" -OutFile "dotnet-hosting.exe"
   .\dotnet-hosting.exe /install /quiet /norestart
   
   # Install IIS with ASP.NET Core module
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
   ```

2. **Publish Application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **IIS Configuration**
   
   **web.config**
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" 
                    arguments=".\better-call-saul.dll" 
                    stdoutLogEnabled="true" 
                    stdoutLogFile=".\logs\stdout" 
                    hostingModel="inprocess">
           <environmentVariables>
             <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
           </environmentVariables>
         </aspNetCore>
       </system.webServer>
     </location>
   </configuration>
   ```

4. **Application Pool Configuration**
   - Create application pool with "No Managed Code"
   - Set .NET CLR version to "No Managed Code"
   - Set identity to custom account with appropriate permissions
   - Set recycling conditions appropriately

### Method 4: Linux with Nginx Reverse Proxy

#### Ubuntu/Debian Setup

1. **Install .NET 8 Runtime**
   ```bash
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-8.0
   ```

2. **Deploy Application**
   ```bash
   # Create application directory
   sudo mkdir -p /var/www/better-call-saul
   sudo chown -R www-data:www-data /var/www/better-call-saul
   
   # Copy published application
   sudo cp -r ./publish/* /var/www/better-call-saul/
   
   # Create uploads directory
   sudo mkdir -p /var/www/better-call-saul/uploads
   sudo chown -R www-data:www-data /var/www/better-call-saul/uploads
   ```

3. **Systemd Service**
   
   **/etc/systemd/system/better-call-saul.service**
   ```ini
   [Unit]
   Description=Better Call Saul Application
   After=network.target
   
   [Service]
   WorkingDirectory=/var/www/better-call-saul
   ExecStart=/usr/bin/dotnet /var/www/better-call-saul/better-call-saul.dll
   Restart=always
   RestartSec=10
   KillSignal=SIGINT
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:5000
   Environment=ConnectionStrings__DefaultConnection=Server=db-server;Database=BetterCallSaul;User Id=user;Password=pass;
   Environment=AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/
   Environment=AzureOpenAI__ApiKey=your-api-key
   
   # File upload settings
   Environment=FileStorage__BasePath=/var/www/better-call-saul/uploads
   Environment=FileStorage__MaxFileSize=10000000
   
   # User and permissions
   User=www-data
   Group=www-data
   
   # Security
   UMask=0077
   
   [Install]
   WantedBy=multi-user.target
   ```

4. **Nginx Configuration**
   
   **/etc/nginx/sites-available/better-call-saul**
   ```nginx
   server {
       listen 80;
       server_name your-domain.com www.your-domain.com;
       
       # Security headers
       add_header X-Frame-Options DENY;
       add_header X-Content-Type-Options nosniff;
       add_header X-XSS-Protection "1; mode=block";
       add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
       
       # File upload size
       client_max_body_size 10M;
       
       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
           
           # Timeouts
           proxy_connect_timeout 30s;
           proxy_send_timeout 30s;
           proxy_read_timeout 30s;
       }
       
       # Static files
       location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
           expires 1y;
           add_header Cache-Control "public, immutable";
           proxy_pass http://localhost:5000;
       }
   }
   ```

5. **Enable and Start Services**
   ```bash
   # Enable Nginx site
   sudo ln -s /etc/nginx/sites-available/better-call-saul /etc/nginx/sites-enabled/
   sudo nginx -t && sudo systemctl reload nginx
   
   # Enable and start application service
   sudo systemctl enable better-call-saul.service
   sudo systemctl start better-call-saul.service
   sudo systemctl status better-call-saul.service
   ```

### Method 2: Azure App Service

1. **Create App Service Plan**
   - Choose appropriate tier (B1 minimum recommended)
   - Select region close to your users

2. **Configure Application**
   - Set environment variables in Configuration
   - Configure connection strings
   - Set up deployment slots for staging

3. **Deployment Options**
   - GitHub Actions CI/CD
   - Azure DevOps pipelines
   - FTP deployment
   - Zip deploy

### Method 3: IIS Deployment (Windows)

1. **Install Requirements**
   ```powershell
   # Install .NET 8 Hosting Bundle
   # Install IIS with ASP.NET Core module
   ```

2. **Publish Application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **IIS Configuration**
   - Create application pool with No Managed Code
   - Set physical path to publish folder
   - Configure web.config:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" 
                    arguments=".\better-call-saul.dll" 
                    stdoutLogEnabled="false" 
                    stdoutLogFile=".\logs\stdout" 
                    hostingModel="inprocess" />
       </system.webServer>
     </location>
   </configuration>
   ```

### Method 4: Linux with Nginx

1. **Install .NET 8 Runtime**
   ```bash
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-8.0
   ```

2. **Configure Nginx**
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;
       
       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

3. **Systemd Service**
   ```bash
   # /etc/systemd/system/better-call-saul.service
   [Unit]
   Description=Better Call Saul Application
   
   [Service]
   WorkingDirectory=/var/www/better-call-saul
   ExecStart=/usr/bin/dotnet /var/www/better-call-saul/better-call-saul.dll
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:5000
   
   [Install]
   WantedBy=multi-user.target
   ```

## SSL Configuration

### Let's Encrypt (Recommended)

```bash
# Install certbot
sudo apt-get install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d your-domain.com

# Auto-renewal
sudo crontab -e
# Add: 0 12 * * * /usr/bin/certbot renew --quiet
```

### Azure App Service SSL
- Use built-in SSL support
- Import custom certificates if needed
- Enable HTTPS Only setting

### IIS SSL
- Import certificate in IIS Manager
- Bind certificate to website
- Force HTTPS redirect

## Monitoring & Logging

### Application Insights

Add to `Program.cs`:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

Configure in `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "ConnectionString": "your-connection-string"
  }
}
```

### Health Checks

Add health endpoint:
```csharp
app.MapHealthChecks("/health");
```

### Log Aggregation

- **Azure**: Application Insights
- **AWS**: CloudWatch
- **Self-hosted**: ELK stack or Seq
- **Local**: File logging with log rotation

## Scaling

### Vertical Scaling
- Increase App Service plan tier
- Upgrade database service tier
- Add more CPU/RAM to VM

### Horizontal Scaling
- Multiple App Service instances
- Load balancer configuration
- Database read replicas
- Redis cache for session state

### Database Scaling
- Azure SQL auto-scaling
- Read-scale replicas
- Database sharding (if needed)

## Backup & Recovery

### Database Backups

**SQL Server**
```sql
-- Full backup daily
BACKUP DATABASE BetterCallSaul TO DISK = '/backups/BetterCallSaul_Full.bak';

-- Transaction log backups hourly
BACKUP LOG BetterCallSaul TO DISK = '/backups/BetterCallSaul_Log.trn';
```

**Azure SQL**
- Configure automated backups
- Set retention period (7-35 days)
- Enable point-in-time restore

### File Storage Backups

```bash
# Backup uploads directory
tar -czf /backups/uploads-$(date +%Y%m%d).tar.gz /app/uploads/

# Sync to cloud storage
aws s3 sync /backups/ s3://your-bucket/backups/
```

### Disaster Recovery

1. **Regular Backups**
   - Database: daily full + hourly log
   - Files: daily compressed archives
   - Configuration: version controlled

2. **Recovery Procedure**
   ```bash
   # Restore database
   RESTORE DATABASE BetterCallSaul FROM DISK = '/backups/BetterCallSaul_Full.bak';
   
   # Restore files
   tar -xzf /backups/uploads-latest.tar.gz -C /app/
   
   # Verify application
   curl -I https://your-domain.com/health
   ```

## Security Considerations

- Use managed identities where possible
- Rotate API keys regularly
- Enable database auditing
- Implement rate limiting
- Use HTTPS everywhere
- Regular security updates

## Performance Optimization

- Enable response compression
- Implement caching strategies
- Optimize database queries
- Use CDN for static assets
- Monitor and tune Azure OpenAI usage

## Troubleshooting

### Common Issues and Solutions

#### Database Connection Issues

**Symptoms**: Application fails to start, database errors in logs

**Solutions**:
- Verify connection string format
- Check database server is accessible
- Confirm user permissions
- Ensure TCP/IP is enabled in SQL Server
- Check firewall rules

```bash
# Test database connectivity
sqlcmd -S your-db-server -U your-username -P "your-password" -Q "SELECT 1"

# Check SQL Server status (Linux)
systemctl status mssql-server

# Check SQL Server status (Windows)
Get-Service -Name "MSSQLSERVER"
```

#### Azure OpenAI Connection Issues

**Symptoms**: Document analysis fails, 401/403 errors

**Solutions**:
- Verify API key is correct and not expired
- Check endpoint URL format
- Confirm deployment name exists
- Verify region compatibility
- Check network connectivity to Azure

```bash
# Test Azure OpenAI connectivity
curl -X POST "https://your-resource.openai.azure.com/openai/deployments/your-deployment-name/chat/completions?api-version=2024-02-01" \
  -H "Content-Type: application/json" \
  -H "api-key: your-api-key" \
  -d '{"messages":[{"role":"user","content":"test"}]}'
```

#### File Upload Issues

**Symptoms**: File uploads fail, permission errors

**Solutions**:
- Check upload directory permissions
- Verify disk space
- Confirm file size limits
- Check allowed file extensions

```bash
# Check directory permissions
ls -la /app/uploads/

# Check disk space
df -h

# Set correct permissions
chown -R www-data:www-data /app/uploads
chmod 755 /app/uploads
```

#### Application Startup Issues

**Symptoms**: Application crashes on startup

**Solutions**:
- Check environment variables are set
- Verify .NET 8 runtime is installed
- Review application logs
- Check database migrations

```bash
# View application logs
dotnet better-call-saul.dll

# Check .NET runtime
dotnet --list-runtimes

# Enable detailed logging
ASPNETCORE_DETAILEDERRORS=true dotnet better-call-saul.dll
```

### Logging and Debugging

#### Enable Detailed Logging

```bash
# Set environment variables for detailed logging
export ASPNETCORE_DETAILEDERRORS=true
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug

# Or in appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

#### Common Log Locations

- **Docker**: `docker logs better-call-saul`
- **IIS**: `C:\inetpub\logs\LogFiles\W3SVC1`
- **Linux**: `journalctl -u better-call-saul.service`
- **Azure App Service**: Application Insights or Kudu console

### Performance Issues

**Symptoms**: Slow response times, high CPU/memory usage

**Solutions**:
- Enable response compression
- Implement caching
- Optimize database queries
- Scale horizontally
- Monitor Azure OpenAI usage

```csharp
// In Program.cs - Enable response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

app.UseResponseCompression();
```

### Health Check Endpoints

The application provides these health endpoints:

- `GET /health` - Basic application health
- `GET /health/db` - Database connectivity check
- `GET /health/azure` - Azure OpenAI connectivity check

```bash
# Test health endpoints
curl http://localhost:5000/health
curl http://localhost:5000/health/db
curl http://localhost:5000/health/azure
```

## Security Checklist

### Pre-Deployment Security Review

- [ ] Use HTTPS in production
- [ ] Validate all environment variables are set
- [ ] Use strong database passwords
- [ ] Rotate API keys regularly
- [ ] Enable database auditing
- [ ] Configure proper file permissions
- [ ] Set up firewall rules
- [ ] Enable logging and monitoring
- [ ] Regular security updates
- [ ] Backup strategy in place

### Ongoing Security Maintenance

- [ ] Regular dependency updates
- [ ] Security patch management
- [ ] Access review and rotation
- [ ] Log analysis for anomalies
- [ ] Regular security scanning
- [ ] Incident response plan

## Performance Optimization

### Database Optimization

```sql
-- Create indexes for performance
CREATE INDEX IX_Cases_Status ON Cases (Status) INCLUDE (Title, CreatedAt);
CREATE INDEX IX_Documents_CaseId_UploadedAt ON Documents (CaseId, UploadedAt);
CREATE INDEX IX_AspNetUsers_Email ON AspNetUsers (Email);

-- Monitor query performance
SELECT TOP 10 
    total_worker_time/execution_count AS AvgCPU,
    total_elapsed_time/execution_count AS AvgDuration,
    execution_count,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1, 
    ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) 
    ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) AS QueryText
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
ORDER BY total_worker_time DESC;
```

### Application Optimization

- Enable response compression
- Implement output caching
- Use CDN for static assets
- Optimize Azure OpenAI usage
- Monitor memory usage
- Implement connection pooling

### Monitoring and Alerts

Set up alerts for:
- High CPU usage (>80%)
- High memory usage (>90%)
- Database connection errors
- Azure OpenAI rate limiting
- File system space (<10% free)
- HTTP 5xx errors

## Support and Maintenance

### Regular Maintenance Tasks

1. **Daily**:
   - Check application logs for errors
   - Verify backup completion
   - Monitor system resources

2. **Weekly**:
   - Review security logs
   - Check for dependency updates
   - Validate monitoring alerts

3. **Monthly**:
   - Rotate API keys and passwords
   - Review access permissions
   - Performance tuning review

### Getting Help

- **Application Logs**: Check `/logs` directory or application insights
- **Database Issues**: Check SQL Server error logs
- **Azure Issues**: Azure Portal → Your Resource → Diagnostics
- **Community Support**: GitHub Issues or Stack Overflow

## Version History

- **v1.0.0** - Initial production release
- **Future**: Regular security and feature updates

---

This deployment guide provides comprehensive instructions for production deployment. Always test changes in a staging environment before deploying to production. Regular maintenance and monitoring are essential for production systems.