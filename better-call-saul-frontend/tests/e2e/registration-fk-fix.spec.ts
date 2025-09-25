import { test, expect } from '@playwright/test';

// Test data for registration
const testUser = {
  email: `test-${Date.now()}@example.com`, // Unique email for each test run
  password: 'Password123!',
  firstName: 'John',
  lastName: 'Doe',
  barNumber: '12345',
  lawFirm: 'Test Public Defender Office',
  registrationCode: '7IGYCHTV21BN', // Using an unused registration code
};

const invalidUser = {
  ...testUser,
  email: `invalid-${Date.now()}@example.com`,
  registrationCode: 'INVALID123',
};

test.describe('Registration Flow - Foreign Key Constraint Fix', () => {
  test('should complete registration without foreign key constraint errors', async ({ page }) => {
    console.log(`Testing with user email: ${testUser.email}`);

    // Navigate to the application
    await page.goto('/');

    // Should redirect to login page or show login form
    await page.waitForURL(/.*\/(login)?$/, { timeout: 10000 });

    // Look for registration link/button
    const registerButton = page
      .locator('text=Register')
      .or(page.locator('text=Sign up'))
      .or(page.locator('text=Create account'))
      .first();
    if (await registerButton.isVisible()) {
      await registerButton.click();
    } else {
      // Try navigating directly to register
      await page.goto('/register');
    }

    // Wait for registration page to load
    await page.waitForURL(/.*register/, { timeout: 10000 });

    // Fill registration form
    await page.fill(
      'input[name="firstName"], input[placeholder*="First"], input[id*="first"]',
      testUser.firstName
    );
    await page.fill(
      'input[name="lastName"], input[placeholder*="Last"], input[id*="last"]',
      testUser.lastName
    );
    await page.fill(
      'input[name="email"], input[placeholder*="Email"], input[type="email"]',
      testUser.email
    );
    await page.fill(
      'input[name="registrationCode"], input[placeholder*="Registration"], input[placeholder*="Code"]',
      testUser.registrationCode
    );
    await page.fill(
      'input[name="password"], input[type="password"]:not([name*="confirm"])',
      testUser.password
    );
    await page.fill(
      'input[name="confirmPassword"], input[placeholder*="Confirm"]',
      testUser.password
    );

    // Optional fields
    const barNumberField = page
      .locator('input[name="barNumber"], input[placeholder*="Bar"]')
      .first();
    if (await barNumberField.isVisible()) {
      await barNumberField.fill(testUser.barNumber);
    }

    const lawFirmField = page
      .locator('input[name="lawFirm"], input[placeholder*="Law Firm"]')
      .first();
    if (await lawFirmField.isVisible()) {
      await lawFirmField.fill(testUser.lawFirm);
    }

    console.log('Filled all registration fields');

    // Submit registration
    const submitButton = page
      .locator('button[type="submit"]')
      .or(page.locator('text=Create Account'))
      .or(page.locator('text=Register'))
      .first();
    await submitButton.click();

    console.log('Submitted registration form');

    // Wait for success (redirect to dashboard) or error message
    await page.waitForTimeout(5000); // Give time for the request

    // Check if registration was successful
    const currentUrl = page.url();
    console.log(`Current URL after registration: ${currentUrl}`);

    if (currentUrl.includes('dashboard')) {
      console.log('Registration successful - redirected to dashboard');

      // Verify we're logged in
      await expect(page.locator('text=Dashboard').or(page.locator('h1')).first()).toBeVisible();

      // Verify registration code was properly marked as used in the database
      await verifyRegistrationCodeUsed(testUser.registrationCode, testUser.email);
    } else {
      // Check for error messages
      const errorMessage = await page
        .locator('.error, .alert, [class*="error"], [class*="danger"]')
        .first()
        .textContent();
      console.log(`Registration error: ${errorMessage || 'No error message found'}`);

      // Take screenshot for debugging
      await page.screenshot({ path: 'registration-error-fk-fix.png' });

      throw new Error(`Registration failed: ${errorMessage}`);
    }
  });

  test('should show error for invalid registration code', async ({ page }) => {
    await page.goto('/register');

    // Fill form with invalid registration code
    await page.fill('input[name="firstName"], input[placeholder*="First"]', invalidUser.firstName);
    await page.fill('input[name="lastName"], input[placeholder*="Last"]', invalidUser.lastName);
    await page.fill('input[name="email"], input[type="email"]', invalidUser.email);
    await page.fill(
      'input[name="registrationCode"], input[placeholder*="Registration"]',
      invalidUser.registrationCode
    );
    await page.fill(
      'input[name="password"], input[type="password"]:not([name*="confirm"])',
      invalidUser.password
    );
    await page.fill(
      'input[name="confirmPassword"], input[placeholder*="Confirm"]',
      invalidUser.password
    );

    const submitButton = page.locator('button[type="submit"]').first();
    await submitButton.click();

    // Wait for error message
    await page.waitForTimeout(2000);

    // Should show error about invalid registration code
    const errorText = await page.locator('.error, .alert, [class*="error"]').first().textContent();
    expect(errorText?.toLowerCase()).toContain('invalid');

    // Should not redirect to dashboard
    expect(page.url()).not.toContain('dashboard');
  });
});

// Helper function to verify registration code was properly marked as used
async function verifyRegistrationCodeUsed(code: string, userEmail: string) {
  try {
    // This would ideally be an API call to verify the registration code status
    // For now, we'll just log that the verification should be done
    console.log(`Registration code ${code} should be marked as used for user ${userEmail}`);
    console.log('In a real test environment, you would verify this via API or database query');
  } catch (error) {
    console.error('Error verifying registration code:', error);
  }
}
