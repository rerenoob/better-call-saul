# Admin Panel - Testing Strategy

## Overview
This document outlines the comprehensive testing strategy for the BetterCallSaul Admin Panel, covering unit testing, integration testing, end-to-end testing, security testing, and performance testing approaches.

## Testing Pyramid

```
          E2E Tests (10%)
         /           \
Integration Tests (20%)
       /               \
   Unit Tests (70%)
```

## 1. Unit Testing

### Backend Unit Tests (.NET)

#### Test Structure
```csharp
[Fact]
public async Task GetDashboardMetrics_ReturnsCorrectMetrics()
{
    // Arrange
    var mockContext = new Mock<BetterCallSaulContext>();
    var mockUserManager = new Mock<UserManager<User>>();
    
    // Setup mocks
    mockUserManager.Setup(m => m.Users.CountAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(100);
    
    var controller = new AdminController(mockContext.Object, mockUserManager.Object);

    // Act
    var result = await controller.GetDashboardMetrics();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var metrics = Assert.IsType<DashboardMetrics>(okResult.Value);
    Assert.Equal(100, metrics.TotalUsers);
}
```

#### Key Test Cases
- **AdminController Tests**:
  - Dashboard metrics calculation
  - User pagination logic
  - User status update validation
  - System health monitoring
  - Audit log retrieval
  - Case statistics aggregation

- **Service Layer Tests**:
  - Business logic validation
  - Error handling
  - Data transformation

#### Testing Tools
- **Framework**: xUnit
- **Mocking**: Moq
- **Assertions**: FluentAssertions
- **Coverage**: Coverlet + ReportGenerator

### Frontend Unit Tests (React + TypeScript)

#### Test Structure
```typescript
describe('useAdminDashboard', () => {
  it('should return loading state initially', () => {
    // Mock the adminService
    jest.spyOn(adminService, 'getDashboardMetrics').mockResolvedValue(mockMetrics);
    
    const { result } = renderHook(() => useAdminDashboard());
    
    expect(result.current.isLoading).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('should return metrics after successful fetch', async () => {
    jest.spyOn(adminService, 'getDashboardMetrics').mockResolvedValue(mockMetrics);
    
    const { result, waitForNextUpdate } = renderHook(() => useAdminDashboard());
    
    await waitForNextUpdate();
    
    expect(result.current.isLoading).toBe(false);
    expect(result.current.metrics.totalUsers).toBe(100);
  });
});
```

#### Key Test Cases
- **Custom Hooks**:
  - useAdminDashboard hook
  - Data fetching and state management
  - Error handling

- **Components**:
  - AdminDashboard rendering
  - UserManagement functionality
  - SystemHealth monitoring
  - AuditLogs display
  - MetricCard components

- **Services**:
  - adminService API calls
  - Data transformation
  - Error handling

#### Testing Tools
- **Framework**: Jest + React Testing Library
- **Mocking**: jest.mock()
- **Utilities**: @testing-library/react-hooks
- **Coverage**: Jest coverage reports

## 2. Integration Testing

### Backend Integration Tests

#### Test Structure
```csharp
[Collection("Database")]
public class AdminControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboardMetrics_ReturnsData_WithAuthenticatedAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await client.GetAsync("/api/admin/dashboard/metrics");

        // Assert
        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetrics>();
        Assert.NotNull(metrics);
    }
}
```

#### Key Test Cases
- **API Endpoint Integration**:
  - Authentication and authorization
  - Database integration
  - Error response handling
  - Pagination behavior

- **Database Integration**:
  - EF Core query performance
  - Transaction handling
  - Data consistency

#### Testing Tools
- **Framework**: xUnit
- **Test Database**: SQLite in-memory or TestContainers
- **HTTP Client**: HttpClient with authentication

### Frontend Integration Tests

#### Test Structure
```typescript
describe('UserManagement Integration', () => {
  it('should load and display users', async () => {
    // Mock API responses
    mockServer.use(
      rest.get('/api/admin/users', (req, res, ctx) => {
        return res(ctx.json(mockUsersResponse));
      })
    );

    render(<UserManagement />);

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });

    // Verify user data is displayed correctly
    expect(screen.getByText('john.doe@example.com')).toBeInTheDocument();
  });
});
```

#### Key Test Cases
- **Component Integration**:
  - API data fetching and display
  - User interactions
  - State management
  - Error handling

- **Routing Integration**:
  - Navigation between admin pages
  - Protected route behavior
  - URL parameter handling

#### Testing Tools
- **Framework**: Jest + React Testing Library
- **API Mocking**: MSW (Mock Service Worker)
- **Routing**: MemoryRouter for testing

## 3. End-to-End Testing

### Test Structure
```typescript
describe('Admin Panel E2E Flow', () => {
  it('should allow admin to view dashboard and manage users', async () => {
    // Login as admin
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@example.com');
    await page.fill('input[name="password"]', 'admin123');
    await page.click('button[type="submit"]');

    // Navigate to admin panel
    await page.goto('/admin/dashboard');

    // Verify dashboard metrics
    await expect(page.locator('text=Total Users')).toBeVisible();

    // Navigate to user management
    await page.click('text=User Management');
    await expect(page.locator('text=Users')).toBeVisible();

    // Test user status toggle
    const firstUser = page.locator('tr').nth(1);
    await firstUser.locator('button:has-text("Deactivate")').click();
    await expect(firstUser.locator('text=Inactive')).toBeVisible();
  });
});
```

### Key Test Scenarios
- **Admin Authentication Flow**:
  - Login with admin credentials
  - Access control verification
  - Session management

- **Dashboard Functionality**:
  - Metrics display
  - Real-time updates
  - Navigation

- **User Management**:
  - User listing and pagination
  - Status toggling
  - Error handling

- **System Monitoring**:
  - Health status display
  - Auto-refresh behavior

- **Audit Logs**:
  - Log viewing
  - Pagination
  - Filtering

### Testing Tools
- **Framework**: Playwright or Cypress
- **Browser**: Chromium, Firefox, WebKit
- **Environment**: Test environment with seeded data
- **CI Integration**: GitHub Actions/Azure DevOps

## 4. Security Testing

### Authentication & Authorization Tests
```csharp
[Fact]
public async Task GetDashboardMetrics_ReturnsUnauthorized_ForNonAdminUser()
{
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", userToken);

    var response = await client.GetAsync("/api/admin/dashboard/metrics");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task GetDashboardMetrics_ReturnsUnauthorized_ForUnauthenticatedUser()
{
    var client = _factory.CreateClient();
    
    var response = await client.GetAsync("/api/admin/dashboard/metrics");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### Security Test Cases
- **Authentication**:
  - JWT token validation
  - Role-based access control
  - Session timeout

- **Authorization**:
  - Admin role requirement enforcement
  - API endpoint protection
  - UI element visibility based on roles

- **Data Protection**:
  - Sensitive data exposure
  - SQL injection prevention
  - XSS protection

### Security Testing Tools
- **Static Analysis**: SonarQube, Security Code Scan
- **Dynamic Analysis**: OWASP ZAP, Burp Suite
- **Dependency Scanning**: OWASP Dependency-Check, Snyk

## 5. Performance Testing

### Load Testing
```typescript
// Example using k6
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '1m', target: 10 },  // Ramp up to 10 users
    { duration: '3m', target: 10 },  // Stay at 10 users
    { duration: '1m', target: 0 },   // Ramp down to 0 users
  ],
};

export default function () {
  const headers = {
    'Authorization': 'Bearer ' + __ENV.ADMIN_TOKEN,
  };

  let res = http.get('http://localhost:5191/api/admin/dashboard/metrics', { headers });
  
  check(res, {
    'is status 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
```

### Performance Test Cases
- **API Response Times**:
  - Dashboard metrics: < 500ms
  - User listing: < 1000ms (with pagination)
  - Audit logs: < 1500ms (with large datasets)

- **Concurrency**:
  - Multiple admin users simultaneously
  - Mixed read/write operations
  - Peak load handling

- **Database Performance**:
  - Query optimization
  - Index effectiveness
  - Connection pooling

### Performance Testing Tools
- **Load Testing**: k6, Locust, JMeter
- **Monitoring**: Application Insights, Prometheus
- **Profiling**: dotTrace, Chrome DevTools

## 6. Accessibility Testing

### Test Cases
- **WCAG 2.1 AA Compliance**:
  - Keyboard navigation
  - Screen reader compatibility
  - Color contrast ratios
  - Form labeling
  - Error identification

- **Component Accessibility**:
  - ARIA attributes
  - Focus management
  - Semantic HTML
  - Responsive design

### Accessibility Testing Tools
- **Automated**: axe-core, Lighthouse
- **Manual**: Screen readers (NVDA, VoiceOver)
- **Visual**: Color contrast checkers

## 7. Test Environment Setup

### Test Data Management
```csharp
public class AdminTestDataSeeder
{
    public static void SeedTestData(BetterCallSaulContext context)
    {
        // Seed admin users
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            Roles = new[] { "Admin" },
            IsActive = true
        };

        // Seed regular users
        for (int i = 0; i < 100; i++)
        {
            context.Users.Add(new User
            {
                Email = $"user{i}@test.com",
                IsActive = i % 2 == 0
            });
        }

        context.SaveChanges();
    }
}
```

### Environment Configuration
- **Unit Tests**: In-memory database, mocked dependencies
- **Integration Tests**: Test database with seeded data
- **E2E Tests**: Staging environment with realistic data
- **Performance Tests**: Production-like environment

## 8. Test Automation

### CI/CD Pipeline Integration
```yaml
# GitHub Actions example
name: Admin Panel Tests

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      
    - name: Run backend tests
      run: dotnet test BetterCallSaul.Tests/BetterCallSaul.Tests.csproj
      
    - name: Run frontend tests
      run: |
        cd better-call-saul-frontend
        npm install
        npm test -- --coverage
        
    - name: Run E2E tests
      run: |
        cd better-call-saul-frontend
        npm run test:e2e
```

### Test Reporting
- **Coverage Reports**: HTML coverage reports for both frontend and backend
- **Test Results**: JUnit XML reports for CI integration
- **Performance Metrics**: Performance test results and trends
- **Security Reports**: Security scan results and vulnerabilities

## 9. Test Metrics and Quality Gates

### Quality Metrics
- **Test Coverage**: > 80% for critical paths
- **API Response Time**: < 500ms P95
- **Error Rate**: < 0.1% of requests
- **Security Vulnerabilities**: Zero critical vulnerabilities

### Quality Gates
- ✅ All unit tests passing
- ✅ Integration tests passing
- ✅ E2E tests passing
- ✅ Security scans clean
- ✅ Performance targets met
- ✅ Accessibility requirements satisfied

---

*Last Updated: [Current Date]*
*Version: 1.0*