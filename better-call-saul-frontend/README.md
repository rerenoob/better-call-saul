# Better Call Saul - Frontend

Modern React TypeScript frontend for the Better Call Saul AI legal assistant platform.

## Technology Stack
- **React 18** with TypeScript for type safety
- **Vite** for fast development and building
- **Tailwind CSS** for responsive styling
- **React Query** for server state management
- **React Router** for navigation
- **Recharts** for data visualization
- **ESLint & Prettier** for code quality

## Features
- 🔐 JWT-based authentication with automatic token refresh
- 📊 Interactive dashboard with case metrics and analytics
- 📄 Document viewer with annotation support
- 🔍 AI-powered case analysis interface
- 📈 Report generation and export capabilities
- 🎨 Modern, responsive UI with dark/light mode support
- ⚡ Real-time updates via SignalR integration

## Quick Start

### Development
```bash
npm install           # Install dependencies
npm run dev          # Start development server (http://localhost:5173)
npm run type-check   # TypeScript type checking
npm run lint         # ESLint code quality check
```

### Production
```bash
npm run build        # Build for production
npm run preview      # Preview production build
```

### Testing
```bash
npx playwright test  # Run E2E tests
```

## Project Structure
```
src/
├── components/      # Reusable UI components
├── pages/          # Page-level components
├── services/       # API integration and HTTP client
├── hooks/          # Custom React hooks
├── types/          # TypeScript type definitions
├── utils/          # Utility functions and helpers
└── styles/         # Global styles and Tailwind config
```

## Environment Configuration
- `.env.development` - Development environment variables
- `.env.production` - Production environment variables

## API Integration
The frontend communicates with the .NET Web API backend via:
- RESTful API endpoints for data operations
- SignalR for real-time updates during AI analysis
- JWT tokens for secure authentication
- TypeScript-generated client types for type safety

For backend API documentation, visit `/swagger` when the API is running.
