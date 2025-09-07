import { test, expect } from '@playwright/test';

// Simple test that doesn't require the full app to be running
test.describe('Basic Navigation', () => {
  test('should load the application and show login page', async ({ page }) => {
    // Mock the API responses to avoid real network calls
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({})
      });
    });

    // Navigate to the application
    await page.goto('http://localhost:5173');
    
    // Check that we're on a page that contains React content
    await expect(page.locator('body')).not.toBeEmpty();
    
    // Check for basic React app structure
    await expect(page.locator('#root')).toBeVisible();
    
    // The app should have some text content
    const pageText = await page.textContent('body');
    expect(pageText).toContain('Login');
  });

  test('should have working navigation between pages', async ({ page }) => {
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({})
      });
    });

    await page.goto('http://localhost:5173/login');
    
    // Check login page elements
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /login/i })).toBeVisible();

    // Navigate to register page via button/link
    const registerLinks = await page.getByText(/register/i).all();
    if (registerLinks.length > 0) {
      await registerLinks[0].click();
      await expect(page).toHaveURL(/.*register/);
    }
  });

  test('should protect authenticated routes', async ({ page }) => {
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Unauthorized' })
      });
    });

    // Try to access protected routes
    await page.goto('http://localhost:5173/dashboard');
    // Should redirect to login
    await expect(page).toHaveURL(/.*login/);

    await page.goto('http://localhost:5173/cases/1');
    await expect(page).toHaveURL(/.*login/);
  });
});