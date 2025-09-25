import { test, expect } from '@playwright/test';

test.describe('Admin Panel', () => {
  test('should redirect non-admin users to dashboard', async ({ page }) => {
    // Mock login as regular user
    await page.goto('/login');

    // Fill in login form with regular user credentials
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'test123');
    await page.click('button[type="submit"]');

    // Wait for navigation to complete
    await page.waitForURL('/dashboard');

    // Try to access admin panel
    await page.goto('/admin');

    // Should be redirected to dashboard
    await page.waitForURL('/dashboard');
    expect(page.url()).toContain('/dashboard');
  });

  test('should allow admin users to access admin panel via admin login', async ({ page }) => {
    // Mock login as admin user using admin login page
    await page.goto('/admin-login');

    // Fill in login form with admin credentials
    await page.fill('input[type="email"]', 'admin@bettercallsaul.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');

    // Wait for navigation to complete - should go directly to admin dashboard
    await page.waitForURL('/admin/dashboard');
    expect(page.url()).toContain('/admin/dashboard');

    // Check if admin dashboard content is visible
    await expect(page.getByText('Admin Dashboard')).toBeVisible();
    await expect(page.getByText('System overview and recent activity')).toBeVisible();
  });

  test('should show admin navigation menu', async ({ page }) => {
    // Mock login as admin user using admin login page
    await page.goto('/admin-login');
    await page.fill('input[type="email"]', 'admin@bettercallsaul.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/admin/dashboard');

    // Check if navigation menu is present
    await expect(page.getByText('Dashboard')).toBeVisible();
    await expect(page.getByText('User Management')).toBeVisible();
    await expect(page.getByText('System Health')).toBeVisible();
    await expect(page.getByText('Audit Logs')).toBeVisible();
  });

  test('should navigate to user management page', async ({ page }) => {
    // Mock login as admin user using admin login page
    await page.goto('/admin-login');
    await page.fill('input[type="email"]', 'admin@bettercallsaul.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/admin/dashboard');

    // Go to user management
    await page.goto('/admin/users');

    // Check if user management page loads
    await expect(page.getByText('User Management')).toBeVisible();
    await expect(page.getByText('Manage system users and their permissions')).toBeVisible();
  });

  test('should navigate to system health page', async ({ page }) => {
    // Mock login as admin user using admin login page
    await page.goto('/admin-login');
    await page.fill('input[type="email"]', 'admin@bettercallsaul.com');
    await page.fill('input[type="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/admin/dashboard');

    // Go to system health
    await page.goto('/admin/health');

    // Check if system health page loads
    await expect(page.getByText('System Health')).toBeVisible();
    await expect(page.getByText('Monitor system performance and health metrics')).toBeVisible();
  });

  test('should have admin login page with proper styling and links', async ({ page }) => {
    // Visit admin login page
    await page.goto('/admin-login');

    // Check if admin login page has proper styling
    await expect(page.getByText('🔐 Admin Login')).toBeVisible();
    await expect(
      page.getByText('Access administrative controls and system management')
    ).toBeVisible();

    // Check if admin-specific styling is applied
    await expect(page.locator('button[type="submit"]')).toHaveClass(/bg-red-600/);

    // Check if user login link is present
    await expect(page.getByText('Regular user?')).toBeVisible();
    await expect(page.getByText('User login here')).toBeVisible();
  });

  test('should redirect non-admin users from admin panel to admin login', async ({ page }) => {
    // Mock login as regular user
    await page.goto('/login');

    // Fill in login form with regular user credentials
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'test123');
    await page.click('button[type="submit"]');

    // Wait for navigation to complete
    await page.waitForURL('/dashboard');

    // Try to access admin panel
    await page.goto('/admin');

    // Should be redirected to admin login page (not regular dashboard)
    await page.waitForURL('/admin-login');
    expect(page.url()).toContain('/admin-login');
  });
});
