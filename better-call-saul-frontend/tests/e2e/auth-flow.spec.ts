import { test, expect, Page } from '@playwright/test';

// Mock data for testing
const testUser = {
  email: 'test@example.com',
  password: 'Password123!',
  firstName: 'Test',
  lastName: 'User',
  barNumber: '123456',
};

const mockAuthResponse = {
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
};

// Setup mock API responses
async function setupAuthMocks(page: Page) {
  await page.route('**/api/auth/login', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAuthResponse),
    });
  });

  await page.route('**/api/auth/register', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAuthResponse),
    });
  });

  await page.route('**/api/auth/profile', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAuthResponse.user),
    });
  });

  await page.route('**/api/auth/logout', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({}),
    });
  });
}

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthMocks(page);
  });

  test('should complete full user registration and login flow', async ({ page }) => {
    // Navigate to the application
    await page.goto('/');

    // Should redirect to login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Navigate to registration
    await page.getByRole('link', { name: /create an account/i }).click();
    await expect(page).toHaveURL(/.*register/);
    await expect(page.getByRole('heading', { name: /create account/i })).toBeVisible();

    // Fill registration form
    await page.getByLabel(/first name/i).fill(testUser.firstName);
    await page.getByLabel(/last name/i).fill(testUser.lastName);
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/bar number/i).fill(testUser.barNumber);
    await page.getByLabel(/^password$/i).fill(testUser.password);
    await page.getByLabel(/confirm password/i).fill(testUser.password);

    // Submit registration
    await page.getByRole('button', { name: /create account/i }).click();

    // Should redirect to dashboard after successful registration
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verify user info is displayed
    await expect(page.getByText(testUser.firstName)).toBeVisible();
    await expect(page.getByText(testUser.lastName)).toBeVisible();

    // Logout
    await page.getByRole('button', { name: /logout/i }).click();

    // Should redirect back to login
    await expect(page).toHaveURL(/.*login/);

    // Login with existing credentials
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should redirect to dashboard again
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('should handle authentication errors', async ({ page }) => {
    // Mock login failure
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid credentials' }),
      });
    });

    await page.goto('/login');
    await page.getByLabel(/email/i).fill('wrong@example.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should show error message
    await expect(page.getByText(/invalid credentials/i)).toBeVisible();
    await expect(page).toHaveURL(/.*login/); // Should stay on login page
  });

  test('should protect authenticated routes', async ({ page }) => {
    // Try to access dashboard without login
    await page.goto('/dashboard');

    // Should redirect to login
    await expect(page).toHaveURL(/.*login/);

    // Try to access case details without login
    await page.goto('/cases/1');
    await expect(page).toHaveURL(/.*login/);
  });

  test('should handle logout correctly', async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(testUser.email);
    await page.getByLabel(/password/i).fill(testUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Verify logged in
    await expect(page).toHaveURL(/.*dashboard/);

    // Logout
    await page.getByRole('button', { name: /logout/i }).click();

    // Should redirect to login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Try to access protected route after logout
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/.*login/);
  });
});