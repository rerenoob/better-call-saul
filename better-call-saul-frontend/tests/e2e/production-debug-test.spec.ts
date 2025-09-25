import { test, expect } from '@playwright/test';

const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';
const PRODUCTION_USER = {
  email: 'duong.pham@example.com',
  password: 'Test123!',
};

test.describe('Production Debug Test', () => {
  test('should debug login process', async ({ page }) => {
    // Navigate to production application
    await page.goto(PRODUCTION_URL, { timeout: 30000 });

    console.log('Page URL:', page.url());
    console.log('Page title:', await page.title());

    // Take screenshot before login
    await page.screenshot({ path: 'debug-before-login.png', fullPage: true });

    // Check for login form elements
    const emailField = page.getByLabel('Email');
    const passwordField = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: '🚀 Login' });

    console.log('Email field visible:', await emailField.isVisible());
    console.log('Password field visible:', await passwordField.isVisible());
    console.log('Login button visible:', await loginButton.isVisible());

    // Fill login form
    await emailField.fill(PRODUCTION_USER.email);
    await passwordField.fill(PRODUCTION_USER.password);

    // Take screenshot after filling form
    await page.screenshot({ path: 'debug-form-filled.png', fullPage: true });

    // Click login button
    await loginButton.click();

    // Wait for any navigation or changes
    await page.waitForTimeout(5000);

    // Take screenshot after login attempt
    await page.screenshot({ path: 'debug-after-login.png', fullPage: true });

    console.log('URL after login attempt:', page.url());
    const pageContent = (await page.textContent('body')) || '';
    console.log('Page content snippet:', pageContent.substring(0, 200));

    // Check for error messages
    const errorMessages = page.getByText(/error|invalid|incorrect|failed/i);
    if ((await errorMessages.count()) > 0) {
      const errorText = await errorMessages.textContent();
      console.log('Error message:', errorText);
    }

    // Check for success indicators
    const successIndicators = page.getByText(/welcome|dashboard|success/i);
    if ((await successIndicators.count()) > 0) {
      const successText = await successIndicators.textContent();
      console.log('Success indicator:', successText);
    }

    // Basic assertion that we're still on a valid page
    expect(page.url()).toContain('azurestaticapps.net');
  });
});
