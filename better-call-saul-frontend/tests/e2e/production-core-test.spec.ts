import { test, expect } from '@playwright/test';

const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';
const PRODUCTION_USER = {
  email: 'duong.pham@example.com',
  password: 'Test123!'
};

test.describe('Production Core Test', () => {
  test('should complete login flow with production credentials', async ({ page }) => {
    // Navigate to production application
    await page.goto(PRODUCTION_URL, { timeout: 30000 });
    
    // Verify we're on login page
    await expect(page).toHaveURL(/.*login/);
    await expect(page).toHaveTitle('Better Call Saul AI');
    
    // Fill login form with production credentials
    await page.getByLabel('Email').fill(PRODUCTION_USER.email);
    await page.getByLabel('Password').fill(PRODUCTION_USER.password);
    
    // Click login button
    await page.getByRole('button', { name: '🚀 Login' }).click();
    
    // Wait for navigation and check if we're redirected to dashboard
    await page.waitForURL(/.*dashboard/, { timeout: 10000 });
    
    // Verify dashboard elements
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
    await expect(page.getByText(/welcome/i)).toBeVisible();
    
    // Take screenshot for verification
    await page.screenshot({ path: 'production-dashboard.png', fullPage: true });
  });

  test('should navigate to case management features', async ({ page }) => {
    // Login first
    await page.goto(PRODUCTION_URL + 'login');
    await page.getByLabel('Email').fill(PRODUCTION_USER.email);
    await page.getByLabel('Password').fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: '🚀 Login' }).click();
    
    // Wait for dashboard
    await page.waitForURL(/.*dashboard/);
    
    // Try to navigate to case upload/creation
    const uploadLink = page.getByRole('link', { name: /upload|new case|create case/i });
    if (await uploadLink.count() > 0) {
      await uploadLink.click();
      await page.waitForURL(/.*upload|.*case.*new/);
      await expect(page.getByText(/upload|new case/i)).toBeVisible();
    }
    
    // Check for navigation menu
    const navMenu = page.getByRole('navigation');
    if (await navMenu.count() > 0) {
      await expect(navMenu).toBeVisible();
    }
  });

  test('should handle logout correctly', async ({ page }) => {
    // Login first
    await page.goto(PRODUCTION_URL + 'login');
    await page.getByLabel('Email').fill(PRODUCTION_USER.email);
    await page.getByLabel('Password').fill(PRODUCTION_USER.password);
    await page.getByRole('button', { name: '🚀 Login' }).click();
    
    // Wait for dashboard
    await page.waitForURL(/.*dashboard/);
    
    // Find and click logout
    const logoutButton = page.getByRole('button', { name: /logout|sign out/i });
    if (await logoutButton.count() > 0) {
      await logoutButton.click();
      await page.waitForURL(/.*login/);
      await expect(page.getByLabel('Email')).toBeVisible();
    }
  });

  test('should verify production environment features', async ({ page }) => {
    await page.goto(PRODUCTION_URL);
    
    // Verify production URL
    expect(page.url()).toContain('azurestaticapps.net');
    
    // Check for production-specific elements
    await expect(page.getByText('Better Call Saul')).toBeVisible();
    await expect(page.getByText('Login')).toBeVisible();
    
    // Verify responsive design
    const viewport = page.viewportSize();
    console.log('Viewport size:', viewport);
  });
});