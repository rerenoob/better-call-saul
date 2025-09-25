import { test, expect } from '@playwright/test';

// Basic MCP server test focusing on core authentication
test.describe('Basic MCP Authentication Test', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API endpoints
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token',
          refreshToken: 'mock-refresh-token',
          user: {
            id: 'user-001',
            email: 'test.attorney@example.com',
            firstName: 'Test',
            lastName: 'Attorney',
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
            title: 'Test Case',
            caseNumber: 'TEST-001',
            status: 'New',
            successProbability: 0.65,
          },
        ]),
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

  test('MCP: basic login and dashboard access', async ({ page }) => {
    // Test: Navigation to application
    await page.goto('http://localhost:5173');

    // Verification: Redirect to login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Action: Fill and submit login form
    await page.getByLabel(/email/i).fill('test.attorney@example.com');
    await page.getByLabel(/password/i).fill('Password123!');
    await page.getByRole('button', { name: /login/i }).click();

    // Verification: Successful navigation to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verification: Cases are loaded
    await expect(page.getByText('Test Case')).toBeVisible();
    await expect(page.getByText('TEST-001')).toBeVisible();
  });

  test('MCP: authentication error handling', async ({ page }) => {
    // Mock authentication failure
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Invalid credentials',
          code: 'AUTH_ERROR',
        }),
      });
    });

    await page.goto('http://localhost:5173/login');
    await page.getByLabel(/email/i).fill('wrong@example.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.getByRole('button', { name: /login/i }).click();

    // Should remain on login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();
  });

  test('MCP: route protection', async ({ page }) => {
    // Mock unauthorized access
    await page.route('**/api/auth/profile', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Authentication required',
          code: 'UNAUTHORIZED',
        }),
      });
    });

    // Attempt to access protected routes
    await page.goto('http://localhost:5173/dashboard');
    await expect(page).toHaveURL(/.*login/);

    await page.goto('http://localhost:5173/cases/case-001');
    await expect(page).toHaveURL(/.*login/);
  });
});
