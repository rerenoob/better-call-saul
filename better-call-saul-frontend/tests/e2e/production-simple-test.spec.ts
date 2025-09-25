import { test, expect } from '@playwright/test';

const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';

test.describe('Production Simple Test', () => {
  test('should access production site and check basic functionality', async ({ page }) => {
    // Navigate to production application with longer timeout
    await page.goto(PRODUCTION_URL, { timeout: 30000 });

    // Check that the page loads and has expected content
    const pageTitle = await page.title();
    console.log('Page title:', pageTitle);

    const pageContent = await page.textContent('body');
    console.log('Page content snippet:', pageContent?.substring(0, 200));

    // Take screenshot for manual inspection
    await page.screenshot({ path: 'production-inspection.png', fullPage: true });

    // Check if we can find any login-related elements
    const loginElements = await page.$$('input[type="email"], input[type="password"], button');
    console.log('Found elements:', loginElements.length);

    // Try to find email field by various selectors
    const emailField = await page.$(
      'input[type="email"], input[name*="email"], [data-testid*="email"]'
    );
    const passwordField = await page.$(
      'input[type="password"], input[name*="password"], [data-testid*="password"]'
    );
    const loginButton = await page.$(
      'button[type="submit"], button:has-text("Login"), button:has-text("Sign")'
    );

    console.log('Email field found:', !!emailField);
    console.log('Password field found:', !!passwordField);
    console.log('Login button found:', !!loginButton);

    // If we found the login form elements, try to login
    if (emailField && passwordField && loginButton) {
      await emailField.fill('duong.pham@example.com');
      await passwordField.fill('Test123!');
      await loginButton.click();

      // Wait for navigation
      await page.waitForTimeout(5000);

      // Take screenshot after login attempt
      await page.screenshot({ path: 'production-after-login.png', fullPage: true });

      console.log('Current URL after login attempt:', page.url());
    }

    // Basic assertion that the page loaded
    expect(page.url()).toContain('azurestaticapps.net');
  });
});
