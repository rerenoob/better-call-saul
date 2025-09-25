import { test, expect } from '@playwright/test';

// Test data
const testUser = {
  email: 'test.attorney@example.com',
  password: 'SecurePassword123!',
  firstName: 'Test',
  lastName: 'Attorney',
  barNumber: 'BAR123456',
};

const mockCases = [
  {
    id: '1',
    title: 'State v. Johnson',
    caseNumber: 'CR-2024-001',
    status: 'Pending',
    priority: 'High',
    nextCourtDate: '2024-03-15T10:00:00Z',
    successProbability: 65,
  },
  {
    id: '2',
    title: 'People v. Smith',
    caseNumber: 'CR-2024-002',
    status: 'In Progress',
    priority: 'Medium',
    nextCourtDate: '2024-03-20T14:30:00Z',
    successProbability: 42,
  },
];

test.describe('MCP Core User Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Mock all API endpoints
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token',
          refreshToken: 'mock-refresh-token',
          user: {
            id: '1',
            email: testUser.email,
            firstName: testUser.firstName,
            lastName: testUser.lastName,
            barNumber: testUser.barNumber,
            role: 'Attorney',
          },
        }),
      });
    });

    await page.route('**/api/auth/register', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token',
          refreshToken: 'mock-refresh-token',
          user: {
            id: '1',
            email: testUser.email,
            firstName: testUser.firstName,
            lastName: testUser.lastName,
            barNumber: testUser.barNumber,
            role: 'Attorney',
          },
        }),
      });
    });

    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCases),
      });
    });

    await page.route('**/api/auth/profile', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '1',
          email: testUser.email,
          firstName: testUser.firstName,
          lastName: testUser.lastName,
          barNumber: testUser.barNumber,
          role: 'Attorney',
        }),
      });
    });

    await page.route('**/api/auth/logout', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({}),
      });
    });
  });

  test('complete authentication and case management flow', async ({ page }) => {
    // Navigate to application
    await page.goto('http://localhost:5173');

    // Verify login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Fill login form
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /login/i }).click();

    // Verify dashboard navigation
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verify dashboard is loaded
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verify cases are loaded
    await expect(page.getByText(mockCases[0].title)).toBeVisible();
    await expect(page.getByText(mockCases[0].caseNumber)).toBeVisible();
    await expect(page.getByText(mockCases[1].title)).toBeVisible();

    // Verify case details
    await expect(page.getByText(mockCases[0].status)).toBeVisible();
    await expect(page.getByText(mockCases[0].priority)).toBeVisible();
    await expect(page.getByText(`${mockCases[0].successProbability}%`)).toBeVisible();

    // Navigate to case details
    await page.getByText(mockCases[0].title).click();
    await expect(page).toHaveURL(/.*cases\/1/);
    await expect(page.getByText(mockCases[0].title)).toBeVisible();
    await expect(page.getByText(mockCases[0].caseNumber)).toBeVisible();

    // Navigate back to dashboard
    await page.getByRole('link', { name: /back to dashboard/i }).click();
    await expect(page).toHaveURL(/.*dashboard/);

    // Logout
    await page.getByRole('button', { name: /logout/i }).click();
    await expect(page).toHaveURL(/.*login/);
  });

  test('registration flow', async ({ page }) => {
    await page.goto('http://localhost:5173/register');

    // Verify registration page
    await expect(
      page.getByRole('heading', { name: /register for better call saul/i })
    ).toBeVisible();

    // Fill registration form
    await page.getByLabel(/first name/i).fill(testUser.firstName);
    await page.getByLabel(/last name/i).fill(testUser.lastName);
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/bar number/i).fill(testUser.barNumber);
    await page.getByLabel(/^password$/i).fill(testUser.password);
    await page.getByLabel(/confirm password/i).fill(testUser.password);

    // Submit registration
    await page.getByRole('button', { name: /create account/i }).click();

    // Verify successful registration and navigation to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('authentication error handling', async ({ page }) => {
    // Mock login failure
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid credentials' }),
      });
    });

    await page.goto('http://localhost:5173/login');
    await page.getByLabel(/email/i).fill('wrong@example.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.getByRole('button', { name: /login/i }).click();

    // Verify error message is displayed
    await expect(page.getByText(/invalid credentials/i)).toBeVisible();
    await expect(page).toHaveURL(/.*login/);
  });

  test('route protection', async ({ page }) => {
    // Mock unauthorized access
    await page.route('**/api/auth/profile', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unauthorized' }),
      });
    });

    // Try to access protected routes
    await page.goto('http://localhost:5173/dashboard');
    await expect(page).toHaveURL(/.*login/);

    await page.goto('http://localhost:5173/cases/1');
    await expect(page).toHaveURL(/.*login/);
  });
});
