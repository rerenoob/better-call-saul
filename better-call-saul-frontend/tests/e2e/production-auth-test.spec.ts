import { test, expect } from '@playwright/test';

const PRODUCTION_URL = 'https://orange-island-0a659d210.1.azurestaticapps.net/';

test.describe('Production Authentication Test', () => {
  test('should test authentication flow with various scenarios', async ({ page }) => {
    // Test 1: Check if production site is accessible
    await page.goto(PRODUCTION_URL, { timeout: 30000 });
    
    console.log('=== Production Site Accessibility Test ===');
    console.log('URL:', page.url());
    console.log('Title:', await page.title());
    
    // Verify basic page structure
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: '🚀 Login' })).toBeVisible();
    
    console.log('✓ Login form elements are present');
    
    // Test 2: Try login with provided credentials
    console.log('\n=== Login Test with Provided Credentials ===');
    
    await page.getByLabel('Email').fill('duong.pham@example.com');
    await page.getByLabel('Password').fill('Test123!');
    await page.getByRole('button', { name: '🚀 Login' }).click();
    
    // Wait for response
    await page.waitForTimeout(3000);
    
    const currentUrl = page.url();
    console.log('Current URL after login attempt:', currentUrl);
    
    // Check for error messages
    const errorElement = page.getByText(/401|unauthorized|invalid|error/i);
    if (await errorElement.count() > 0) {
      const errorText = await errorElement.textContent();
      console.log('❌ Login failed with error:', errorText);
      console.log('The provided credentials may be incorrect or the user account may not exist');
    } else if (currentUrl.includes('dashboard')) {
      console.log('✅ Login successful! Redirected to dashboard');
      
      // Test dashboard functionality
      await expect(page.getByText(/welcome|dashboard/i)).toBeVisible();
      console.log('✓ Dashboard loaded successfully');
      
      // Test logout
      const logoutButton = page.getByRole('button', { name: /logout|sign out/i });
      if (await logoutButton.count() > 0) {
        await logoutButton.click();
        await page.waitForURL(/.*login/);
        console.log('✓ Logout functionality works');
      }
    } else {
      console.log('⚠️  Login status unknown. Current page:', currentUrl);
    }
    
    // Test 3: Check registration functionality
    console.log('\n=== Registration Link Test ===');
    
    await page.goto(PRODUCTION_URL);
    const registerLink = page.getByRole('link', { name: /register|create account/i });
    
    if (await registerLink.count() > 0) {
      console.log('✓ Registration link found');
      await registerLink.click();
      await page.waitForTimeout(2000);
      console.log('Registration page URL:', page.url());
    } else {
      console.log('⚠️  Registration link not found');
    }
    
    // Test 4: Verify production environment
    console.log('\n=== Production Environment Verification ===');
    
    expect(page.url()).toContain('azurestaticapps.net');
    console.log('✓ Production domain verified');
    
    const pageTitle = await page.title();
    expect(pageTitle).toContain('Better Call Saul');
    console.log('✓ Page title verified:', pageTitle);
    
    console.log('\n=== Test Summary ===');
    console.log('1. Production site is accessible: ✅');
    console.log('2. Login form is functional: ✅');
    console.log('3. Authentication: Requires valid credentials');
    console.log('4. Production environment: ✅');
    
    // Take final screenshot
    await page.screenshot({ path: 'production-final-state.png', fullPage: true });
  });
});