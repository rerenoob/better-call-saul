# 🚀 Production Deployment Guide - Better Call Saul API

## ⚠️ **URGENT: Production API Configuration Required**

The production API at `https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net` is currently failing with 500 errors due to missing environment variables after security fixes were applied.

## 🔧 **Required Environment Variables**

The following environment variables **MUST** be configured in the Azure App Service:

### **1. JWT Authentication (REQUIRED)**
```bash
JWT_SECRET_KEY=YourProductionSecretKeyAtLeast32CharactersLongAndVerySecure!
```
- **Must be**: 32+ characters, cryptographically secure
- **Generate with**: `openssl rand -base64 32` or similar
- **Example**: `JWT_SECRET_KEY=8kM9nR2vP7qL5wX3cF6hJ4dS1aE0tY9uI8oK7mN5bV2cZ4x`

### **2. Azure OpenAI Service (REQUIRED for AI features)**
```bash
AZURE_OPENAI_ENDPOINT=https://your-openai-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-actual-azure-openai-api-key-here
```
- **Get from**: Azure Portal > Your OpenAI Resource > Keys and Endpoint
- **Format**: Full HTTPS URL for endpoint

### **3. Database Connection (Optional - uses default if not set)**
```bash
CONNECTION_STRING=Server=your-server;Database=BetterCallSaul;...
```

## 🏗️ **Azure App Service Configuration**

### **Method 1: Azure Portal**
1. Navigate to **Azure Portal** > **App Services** > **bettercallsaul-api-gphmb8cvc6h7g3fu**
2. Go to **Configuration** > **Application settings**
3. Add **New application setting** for each required variable:
   - `JWT_SECRET_KEY` = `[Your 32+ char secret]`
   - `AZURE_OPENAI_ENDPOINT` = `https://[your-resource].openai.azure.com/`
   - `AZURE_OPENAI_API_KEY` = `[Your API key]`
4. Click **Save** and **Restart** the app service

### **Method 2: Azure CLI**
```bash
# Set JWT Secret Key
az webapp config appsettings set \
  --resource-group [your-resource-group] \
  --name bettercallsaul-api-gphmb8cvc6h7g3fu \
  --settings JWT_SECRET_KEY="YourSecretKeyHere"

# Set Azure OpenAI Configuration  
az webapp config appsettings set \
  --resource-group [your-resource-group] \
  --name bettercallsaul-api-gphmb8cvc6h7g3fu \
  --settings AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"

az webapp config appsettings set \
  --resource-group [your-resource-group] \
  --name bettercallsaul-api-gphmb8cvc6h7g3fu \
  --settings AZURE_OPENAI_API_KEY="your-api-key-here"

# Restart the app service
az webapp restart \
  --resource-group [your-resource-group] \
  --name bettercallsaul-api-gphmb8cvc6h7g3fu
```

## 🔍 **Current Issues**

### **API Status**: ❌ FAILING
- **Error**: 500 Internal Server Error
- **Cause**: Missing required environment variables
- **Endpoints affected**: All API endpoints (`/api/auth/login`, `/api/case`, etc.)

### **Frontend Status**: ✅ WORKING  
- **URL**: https://orange-island-0a659d210.1.azurestaticapps.net
- **Issue**: Cannot connect to API due to API failures
- **CORS**: Already configured correctly in code

## 🚦 **Verification Steps**

After configuring environment variables:

1. **Test API Health**:
   ```bash
   curl https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net/api/case
   # Should return data or 401 (not 500)
   ```

2. **Test Authentication**:
   ```bash
   curl -X POST https://bettercallsaul-api-gphmb8cvc6h7g3fu.centralus-01.azurewebsites.net/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"test123"}'
   # Should return 401 "Invalid credentials" (not 500)
   ```

3. **Test Frontend**:
   - Visit: https://orange-island-0a659d210.1.azurestaticapps.net/login
   - Should show proper error messages instead of "Network Error"

## 🔒 **Security Notes**

- **Never commit** actual production secrets to version control
- Use **Azure Key Vault** for production secret management if possible
- **Rotate secrets** regularly
- **Monitor** application logs for security events

## 📋 **Deployment Checklist**

- [ ] JWT_SECRET_KEY configured (32+ chars)
- [ ] AZURE_OPENAI_ENDPOINT configured
- [ ] AZURE_OPENAI_API_KEY configured  
- [ ] App Service restarted
- [ ] API health check passes (not 500)
- [ ] Frontend can connect to API
- [ ] Authentication works properly
- [ ] CORS headers present in responses

## 🆘 **Troubleshooting**

### **Still getting 500 errors?**
1. Check **Azure App Service Logs**:
   - Azure Portal > App Service > Log stream
   - Look for startup errors or missing configuration

2. **Verify Environment Variables**:
   - Azure Portal > App Service > Configuration > Application settings
   - Ensure all variables are present and correct

3. **Check Application Insights** (if enabled):
   - Look for detailed error messages and stack traces

### **CORS errors?**
- Environment variables fix should resolve this
- API needs to start successfully first

---

**⏰ Priority**: **URGENT** - Production is currently non-functional  
**🔧 Action Required**: Configure environment variables in Azure App Service  
**📞 Contact**: DevOps/Azure administrator for environment variable configuration