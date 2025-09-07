# Marketing Site

This is the marketing/landing page for Better Call Saul AI application.

## Deployment

This site is automatically deployed to Azure Static Web Apps via GitHub Actions when changes are pushed to the `main` branch.

### Setup Requirements

1. Create a new Azure Static Web App resource for the marketing site
2. Add the deployment token to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN_MARKETING_SITE`
3. Configure custom domain (optional)

### Local Development

Simply open `index.html` in a web browser or use a local server:

```bash
# Using Python
python -m http.server 8000

# Using Node.js
npx serve .

# Using PHP
php -S localhost:8000
```

### File Structure

- `index.html` - Main landing page
- `staticwebapp.config.json` - Azure Static Web Apps configuration
- Any additional assets (images, CSS, JS) should be placed in appropriate subdirectories