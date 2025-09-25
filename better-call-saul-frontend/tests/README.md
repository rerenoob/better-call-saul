# Playwright E2E Tests

This directory contains organized end-to-end tests for the Better Call Saul frontend application.

## Test Organization

### Current Test Files

- **`auth-flow.spec.ts`** - Comprehensive authentication testing
  - User registration and login flow
  - Authentication error handling
  - Route protection and access control
  - Logout functionality

- **`case-management.spec.ts`** - Case management functionality
  - Displaying cases on dashboard
  - Navigating to case details
  - Handling empty case lists
  - Error handling for case loading

- **`admin-panel.spec.ts`** - Admin-specific functionality
  - Admin user authentication
  - Admin panel navigation
  - User management access
  - System health monitoring

- **`production.spec.ts`** - Production environment testing
  - Production URL verification
  - Real user credentials testing
  - Production-specific features
  - Environment-specific checks

- **`diagnostic.spec.ts`** - Debugging and diagnostic tests
  - Page content analysis
  - Form element inspection
  - Dashboard debugging
  - Screenshot generation for debugging

## Running Tests

### Development Environment
```bash
npm run test:e2e          # Run all tests
npm run test:e2e -- --ui  # Run with Playwright UI
```

### Production Environment
```bash
npm run test:e2e:production  # Run production tests
```

### Specific Test Files
```bash
npx playwright test auth-flow.spec.ts     # Run specific test file
npx playwright test --grep "login"        # Run tests matching pattern
```

## Test Structure

Each test file follows a consistent pattern:

1. **Mock Data** - Test-specific data definitions
2. **Mock Setup** - API response mocking functions
3. **Test Suites** - Logical grouping of related tests
4. **Individual Tests** - Specific test scenarios

## Best Practices

- Tests are isolated and independent
- Mock data is used to avoid external dependencies
- Tests verify both success and error scenarios
- Production tests use real production URLs
- Diagnostic tests help with debugging

## Removed Redundant Tests

The following redundant test files were removed during organization:

- `basic-mcp.spec.ts`
- `basic-navigation.spec.ts`
- `core-user-flow.spec.ts`
- `diagnostic.spec.ts` (replaced with improved version)
- `final-core-flow.spec.ts`
- `mcp-capabilities.spec.ts`
- `mcp-core-flow.spec.ts`
- `mcp-demonstration.spec.ts`
- `mcp-success.spec.ts`
- `production-auth-test.spec.ts`
- `production-core-flow.spec.ts`
- `production-core-test.spec.ts`
- `production-debug-test.spec.ts`
- `production-simple-test.spec.ts`
- `production-smoke-test.spec.ts`
- `production-validation.spec.ts`
- `registration-fk-fix.spec.ts`
- `registration-integration.spec.ts`
- `simple-flow.spec.ts`

## Cleaned Up Files

Removed temporary test files and screenshots:
- All `.png` screenshot files
- All `test-*.js` temporary test files

## Future Improvements

- Add more specific component tests
- Implement visual regression testing
- Add performance testing
- Include accessibility testing