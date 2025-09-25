import { test, expect, Page } from '@playwright/test';

// Mock data for testing
const testUser = {
  email: 'test@example.com',
  password: 'Password123!',
  firstName: 'Test',
  lastName: 'User',
  barNumber: '123456',
};

const mockCasesResponse = [
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

// Setup mock API responses
async function setupCaseMocks(page: Page) {
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

  await page.route('**/api/cases*', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockCasesResponse),
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

  await page.route('**/api/cases/1', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockCasesResponse[0]),
    });
  });
}

test.describe('Case Management', () => {
  test.beforeEach(async ({ page }) => {
    await setupCaseMocks(page);
  });

  test('should display cases on dashboard', async ({ page }) => {
    // Login directly
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Verify dashboard loads
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verify cases are displayed
    await expect(page.getByText(mockCasesResponse[0].title)).toBeVisible();
    await expect(page.getByText(mockCasesResponse[0].caseNumber)).toBeVisible();
    await expect(page.getByText(mockCasesResponse[1].title)).toBeVisible();
    await expect(page.getByText(mockCasesResponse[1].caseNumber)).toBeVisible();

    // Verify case status and priority are visible
    await expect(page.getByText(mockCasesResponse[0].status)).toBeVisible();
    await expect(page.getByText(mockCasesResponse[0].priority)).toBeVisible();
    await expect(page.getByText(`${mockCasesResponse[0].successProbability}%`)).toBeVisible();
  });

  test('should navigate to case details', async ({ page }) => {
    // Login directly
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Click on first case
    await page.getByText(mockCasesResponse[0].title).click();

    // Should navigate to case details page
    await expect(page).toHaveURL(/.*cases\/1/);
    await expect(page.getByText(mockCasesResponse[0].title)).toBeVisible();
    await expect(page.getByText(mockCasesResponse[0].caseNumber)).toBeVisible();

    // Navigate back to dashboard
    await page.getByRole('link', { name: /back to dashboard/i }).click();
    await expect(page).toHaveURL(/.*dashboard/);
  });

  test('should handle empty case list', async ({ page }) => {
    // Mock empty cases response
    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    // Login
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Verify dashboard loads
    await expect(page).toHaveURL(/.*dashboard/);

    // Should show empty state message
    await expect(page.getByText(/no cases/i)).toBeVisible();
  });

  test('should handle case loading errors', async ({ page }) => {
    // Mock cases API error
    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal server error' }),
      });
    });

    // Login
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Verify dashboard loads
    await expect(page).toHaveURL(/.*dashboard/);

    // Should show error state
    await expect(page.getByText(/error/i)).toBeVisible();
  });
});