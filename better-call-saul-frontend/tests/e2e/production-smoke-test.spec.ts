import { test, expect } from '@playwright/test';

const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';

test.describe('Production Smoke Test', () => {
  test('should load production site', async ({ page }) => {
    // Navigate to production application
    await page.goto(PRODUCTION_URL);
    
    // Check that the page loads
    await expect(page).toHaveTitle(/Better Call Saul/);
    
    // Check for login form elements
    await expect(page.getByRole('heading', { name: /login/i })).toBeVisible({ timeout: 10000 });
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
    
    // Check for registration link
    await expect(page.getByRole('link', { name: /create an account/i })).toBeVisible();
  });

  test('should verify production environment', async ({ page }) => {
    await page.goto(PRODUCTION_URL);
    
    // Verify we're on the production domain
    expect(page.url()).toContain('azurestaticapps.net');
    
    // Check for production-specific elements
    const pageContent = await page.textContent('body');
    expect(pageContent).toContain('Better Call Saul');
    
    // Take a screenshot for verification
    await page.screenshot({ path: 'production-homepage.png' });
  });
});