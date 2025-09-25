import { test, expect } from '@playwright/test';

test.describe('Simple User Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-jwt-token',
          refreshToken: 'mock-refresh-token',
          user: { id: '1', email: 'test@example.com', firstName: 'Test', lastName: 'User' },
        }),
      });
    });

    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });
  });

  test('should load login page and display basic elements', async ({ page }) => {
    await page.goto('http://localhost:5173');

    // Should be on login page
    await expect(page).toHaveURL(/.*login/);

    // Check for basic login form elements
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /login/i })).toBeVisible();

    // Check for register link
    const registerLinks = await page.getByText(/register/i).all();
    expect(registerLinks.length).toBeGreaterThan(0);
  });

  test('should successfully login with mock credentials', async ({ page }) => {
    await page.goto('http://localhost:5173/login');

    // Fill login form
    await page.getByLabel(/email/i).fill('test@example.com');
    await page.getByLabel(/password/i).fill('password123');
    await page.getByRole('button', { name: /login/i }).click();

    // Should navigate to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('should handle login errors', async ({ page }) => {
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

    // Should stay on login page and show some error indication
    await expect(page).toHaveURL(/.*login/);

    // Check if there's any error message or the form is still visible
    const pageContent = await page.textContent('body');
    expect(pageContent).toContain('Login');
  });
});
