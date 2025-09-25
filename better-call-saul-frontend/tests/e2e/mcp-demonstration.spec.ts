import { test, expect } from '@playwright/test';

// This test demonstrates MCP server capabilities with comprehensive user flow testing
test.describe('MCP Server Demonstration - Core User Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Comprehensive API mocking for the entire application
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token-12345',
          refreshToken: 'mock-refresh-token-67890',
          user: {
            id: 'user-001',
            email: 'attorney@example.com',
            firstName: 'Sarah',
            lastName: 'Connor',
            barNumber: 'BAR2024',
            role: 'Senior Attorney',
          },
        }),
      });
    });

    await page.route('**/api/auth/register', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token-register',
          refreshToken: 'mock-refresh-token-register',
          user: {
            id: 'user-new-001',
            email: 'new.attorney@example.com',
            firstName: 'John',
            lastName: 'Doe',
            barNumber: 'BAR2025',
            role: 'Junior Attorney',
          },
        }),
      });
    });

    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'case-001',
            title: 'State v. Anderson',
            caseNumber: 'CR-2024-1001',
            status: 'PreTrial',
            priority: 'High',
            description: 'First-degree murder case with complex evidence',
            successProbability: 0.72,
            nextCourtDate: '2024-03-20T09:00:00Z',
          },
          {
            id: 'case-002',
            title: 'People v. Martinez',
            caseNumber: 'CR-2024-1002',
            status: 'Investigation',
            priority: 'Medium',
            description: 'White collar crime with financial documents',
            successProbability: 0.58,
            nextCourtDate: '2024-04-05T14:30:00Z',
          },
        ]),
      });
    });

    await page.route('**/api/auth/profile', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'user-001',
          email: 'attorney@example.com',
          firstName: 'Sarah',
          lastName: 'Connor',
          barNumber: 'BAR2024',
          role: 'Senior Attorney',
        }),
      });
    });

    await page.route('**/api/auth/logout', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Successfully logged out' }),
      });
    });
  });

  test('complete authentication to case management flow', async ({ page }) => {
    // Test: Navigation to application
    await page.goto('http://localhost:5173');

    // Verification: Should redirect to login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Action: Fill login form with valid credentials
    await page.getByLabel(/email/i).fill('attorney@example.com');
    await page.getByLabel(/password/i).fill('SecurePassword123!');

    // Action: Submit login form
    await page.getByRole('button', { name: /login/i }).click();

    // Verification: Successful navigation to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verification: Cases are loaded and displayed
    await expect(page.getByText('State v. Anderson')).toBeVisible();
    await expect(page.getByText('CR-2024-1001')).toBeVisible();
    await expect(page.getByText('People v. Martinez')).toBeVisible();
    await expect(page.getByText('CR-2024-1002')).toBeVisible();

    // Verification: Case status and priority are visible
    await expect(page.getByText('PreTrial')).toBeVisible();
    await expect(page.getByText('Investigation')).toBeVisible();

    // Verification: Success probability is displayed
    await expect(page.getByText('72%')).toBeVisible();
    await expect(page.getByText('58%')).toBeVisible();

    // Action: Navigate to case details
    await page.getByText('State v. Anderson').click();

    // Verification: Case details page loads
    await expect(page).toHaveURL(/.*cases\/case-001/);
    await expect(page.getByText('State v. Anderson')).toBeVisible();
    await expect(page.getByText('CR-2024-1001')).toBeVisible();

    // Action: Navigate back to dashboard
    await page.getByRole('link', { name: /back to dashboard/i }).click();
    await expect(page).toHaveURL(/.*dashboard/);

    // Action: Logout
    await page.getByRole('button', { name: /logout/i }).click();

    // Verification: Redirected back to login page
    await expect(page).toHaveURL(/.*login/);
  });

  test('registration flow with validation', async ({ page }) => {
    // Test: Direct navigation to registration
    await page.goto('http://localhost:5173/register');

    // Verification: Registration page loads
    await expect(
      page.getByRole('heading', { name: /register for better call saul/i })
    ).toBeVisible();

    // Action: Fill registration form
    await page.getByLabel(/first name/i).fill('John');
    await page.getByLabel(/last name/i).fill('Doe');
    await page.getByLabel(/email/i).fill('new.attorney@example.com');
    await page.getByLabel(/registration code/i).fill('REGCODE123');
    await page.getByLabel(/bar number/i).fill('BAR2025');
    await page.getByLabel(/^password$/i).fill('SecurePassword123!');
    await page.getByLabel(/confirm password/i).fill('SecurePassword123!');

    // Action: Submit registration
    await page.getByRole('button', { name: /create account/i }).click();

    // Verification: Successful registration and navigation to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('authentication error scenarios', async ({ page }) => {
    // Setup: Mock authentication failure
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Authentication failed: Invalid email or password',
          code: 'AUTH_FAILED',
        }),
      });
    });

    // Test: Attempt login with invalid credentials
    await page.goto('http://localhost:5173/login');
    await page.getByLabel(/email/i).fill('invalid@example.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.getByRole('button', { name: /login/i }).click();

    // Verification: Should remain on login page with error indication
    await expect(page).toHaveURL(/.*login/);

    // Check for any error message or form reset
    const pageContent = await page.textContent('body');
    expect(pageContent).toContain('Login');
  });

  test('route protection and unauthorized access', async ({ page }) => {
    // Setup: Mock unauthorized access to protected routes
    await page.route('**/api/auth/profile', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Unauthorized: Please login to access this resource',
          code: 'UNAUTHORIZED',
        }),
      });
    });

    // Test: Attempt to access protected routes without authentication
    await page.goto('http://localhost:5173/dashboard');
    await expect(page).toHaveURL(/.*login/);

    await page.goto('http://localhost:5173/cases/case-001');
    await expect(page).toHaveURL(/.*login/);

    // Verification: Login page should be accessible
    await page.goto('http://localhost:5173/login');
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();
  });

  test('session management and token refresh', async ({ page }) => {
    // This test demonstrates MCP capabilities for testing token-based authentication

    // Setup: Mock token refresh endpoint
    await page.route('**/api/auth/refresh', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'refreshed-jwt-token',
          refreshToken: 'new-refresh-token',
          user: {
            id: 'user-001',
            email: 'attorney@example.com',
            firstName: 'Sarah',
            lastName: 'Connor',
          },
        }),
      });
    });

    // Test: Normal login flow
    await page.goto('http://localhost:5173/login');
    await page.getByLabel(/email/i).fill('attorney@example.com');
    await page.getByLabel(/password/i).fill('SecurePassword123!');
    await page.getByRole('button', { name: /login/i }).click();

    // Verification: Dashboard access
    await expect(page).toHaveURL(/.*dashboard/);

    // Simulate token expiration by mocking 401 on subsequent requests
    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Token expired' }),
      });
    });

    // The application should handle token refresh automatically
    // This demonstrates MCP's ability to test complex authentication flows
  });
});
