import { test, expect } from '@playwright/test';

test.describe('Diagnostic Tests', () => {
  test('debug page content', async ({ page }) => {
    await page.goto('http://localhost:5173/login');

    // Take a screenshot for debugging
    await page.screenshot({ path: 'debug-login.png', fullPage: true });

    // Get all text content
    const pageText = await page.textContent('body');
    console.log('Page text content:', pageText);

    // Get all visible elements
    const allElements = await page.$$('*');
    const visibleElements = [];

    for (const element of allElements) {
      const isVisible = await element.isVisible();
      if (isVisible) {
        const tagName = await element.evaluate(el => el.tagName);
        const text = await element.textContent();
        visibleElements.push({ tagName, text: text?.trim() });
      }
    }

    console.log(
      'Visible elements:',
      visibleElements.filter(el => el.text && el.text.length > 0)
    );

    // Basic checks that should always work
    await expect(page.locator('body')).not.toBeEmpty();
    await expect(page.locator('#root')).toBeVisible();
  });

  test('check registration form elements', async ({ page }) => {
    await page.goto('http://localhost:5173/register');

    // Take screenshot
    await page.screenshot({ path: 'debug-register.png', fullPage: true });

    // List all form elements
    const inputs = await page.$$('input');
    console.log('Input fields found:', inputs.length);

    for (const input of inputs) {
      const type = await input.getAttribute('type');
      const name = await input.getAttribute('name');
      const placeholder = await input.getAttribute('placeholder');
      console.log(`Input: type=${type}, name=${name}, placeholder=${placeholder}`);
    }

    // Check for headings
    const headings = await page.$$('h1, h2, h3, h4, h5, h6');
    for (const heading of headings) {
      const text = await heading.textContent();
      console.log('Heading:', text?.trim());
    }
  });

  test('check dashboard after login', async ({ page }) => {
    // Mock successful login
    await page.route('**/api/auth/login', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'mock-token',
          refreshToken: 'mock-refresh',
          user: { id: '1', email: 'test@example.com' },
        }),
      });
    });

    await page.route('**/api/cases*', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: '1',
            title: 'Test Case',
            caseNumber: 'TEST-001',
            status: 'New',
            successProbability: 0.5,
          },
        ]),
      });
    });

    await page.goto('http://localhost:5173/login');
    await page.getByLabel(/email/i).fill('test@example.com');
    await page.getByLabel(/password/i).fill('password');
    await page.getByRole('button', { name: /login/i }).click();

    await page.waitForURL(/.*dashboard/);
    await page.screenshot({ path: 'debug-dashboard.png', fullPage: true });

    const dashboardText = await page.textContent('body');
    console.log('Dashboard text:', dashboardText);
  });
});
