import { test, expect } from '@playwright/test';

// Production test credentials
const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';
const PRODUCTION_USER = {
  email: 'duong.pham@example.com',
  password: 'Test123!'
};

test.describe('Production Core User Flow', () => {
  test('should complete login and access dashboard', async ({ page }) => {
    // Navigate to production application
    await page.goto(PRODUCTION_URL);
    
    // Should be on login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Fill login form with production credentials
    await page.getByLabel(/email/i).fill(PRODUCTION_USER.email);
    await page.getByLabel(/password/i).fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should redirect to dashboard after successful login
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Verify user is logged in - check for user menu or profile
    await expect(page.getByRole('button', { name: /logout/i })).toBeVisible();

    // Check for basic dashboard elements
    await expect(page.getByText(/cases/i)).toBeVisible();
    await expect(page.getByText(/recent/i)).toBeVisible();
  });

  test('should navigate to case management features', async ({ page }) => {
    // Login first
    await page.goto(PRODUCTION_URL + 'login');
    await page.getByLabel(/email/i).fill(PRODUCTION_USER.email);
    await page.getByLabel(/password/i).fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Wait for dashboard to load
    await expect(page).toHaveURL(/.*dashboard/);

    // Try to navigate to case upload/creation
    await page.getByRole('link', { name: /new case/i }).click();
    
    // Should be on case upload page
    await expect(page).toHaveURL(/.*upload/);
    await expect(page.getByRole('heading', { name: /upload/i })).toBeVisible();

    // Check for file upload elements
    await expect(page.getByText(/drag.*drop/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /browse/i })).toBeVisible();
  });

  test('should access case list and details', async ({ page }) => {
    // Login first
    await page.goto(PRODUCTION_URL + 'login');
    await page.getByLabel(/email/i).fill(PRODUCTION_USER.email);
    await page.getByLabel(/password/i).fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Check if cases are displayed on dashboard
    const caseElements = await page.getByTestId('case-item').count();
    
    if (caseElements > 0) {
      // Click on first case if available
      await page.getByTestId('case-item').first().click();
      
      // Should navigate to case details
      await expect(page).toHaveURL(/.*cases\//);
      await expect(page.getByText(/case details/i)).toBeVisible();
    } else {
      // If no cases, verify empty state
      await expect(page.getByText(/no cases/i)).toBeVisible();
    }
  });

  test('should handle logout correctly', async ({ page }) => {
    // Login first
    await page.goto(PRODUCTION_URL + 'login');
    await page.getByLabel(/email/i).fill(PRODUCTION_USER.email);
    await page.getByLabel(/password/i).fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Verify logged in
    await expect(page).toHaveURL(/.*dashboard/);

    // Logout
    await page.getByRole('button', { name: /logout/i }).click();

    // Should redirect to login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible();

    // Try to access protected route after logout
    await page.goto(PRODUCTION_URL + 'dashboard');
    await expect(page).toHaveURL(/.*login/);
  });

  test('should verify production environment features', async ({ page }) => {
    // Check production-specific features
    await page.goto(PRODUCTION_URL);
    
    // Verify production URL
    expect(page.url()).toContain('azurestaticapps.net');
    
    // Check for production-specific elements (like analytics, etc.)
    await expect(page).toHaveTitle(/Better Call Saul/);
    
    // Verify responsive design elements
    const viewport = page.viewportSize();
    if (viewport && viewport.width < 768) {
      // Mobile view checks
      await expect(page.getByRole('button', { name: /menu/i })).toBeVisible();
    }
  });
});