# Better Call Saul - Quick Start Guide

## Get Started in 5 Minutes

### 1. Prerequisites
- .NET 8 SDK installed
- Node.js 18+ installed
- Git installed

### 2. Clone and Setup
```bash
# Clone the repository
git clone <your-repo-url>
cd better-call-saul

# Restore backend dependencies
dotnet restore

# Setup frontend
cd better-call-saul-frontend
npm install
```

### 3. Database Setup
```bash
# Run migrations to create database
dotnet ef database update --project BetterCallSaul.Infrastructure --startup-project BetterCallSaul.API
```

### 4. Start Development Servers

#### Terminal 1 - Backend API
```bash
# From project root
dotnet watch --project BetterCallSaul.API
# API will be available at: https://localhost:7191
```

#### Terminal 2 - Frontend
```bash
# From frontend directory
cd better-call-saul-frontend
npm run dev
# Frontend will be available at: http://localhost:5173
```

### 5. First Login

1. Open http://localhost:5173 in your browser
2. Use the default admin credentials:
   - **Email**: admin@example.com
   - **Password**: Admin123!
3. Or register a new user with a registration code

## Next Steps

### Explore Features
- Create your first case
- Upload sample documents
- Run case analysis
- Try legal research

### Run Tests
```bash
# Backend tests
dotnet test

# Frontend tests
cd better-call-saul-frontend
npm test
```

### Check Documentation
- Read [API Documentation](./API_DOCUMENTATION.md)
- Review [Development Guide](./DEVELOPMENT_GUIDE.md)
- Explore [Architecture](./ARCHITECTURE.md)

## Need Help?
- Check the [User Guide](./USER_GUIDE.md)
- Review [Troubleshooting](./DEVELOPMENT_GUIDE.md#troubleshooting)
- Examine existing test cases for examples