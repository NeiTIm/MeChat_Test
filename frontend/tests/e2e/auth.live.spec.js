import { setAuthInputs } from './secure-auth-input';
import { test, expect } from '@playwright/test';
test('real account login, reload recovery and logout', async ({
  browser
}) => {
  test.skip(!process.env.MECHAT_E2E_IDENTIFIER || !process.env.MECHAT_E2E_PASSWORD, 'Live credentials were not provided in process environment.');
  // Close the manual context before runner failure artifacts can capture private page content.
  const context = await browser.newContext({
    baseURL: 'http://127.0.0.1:5173'
  });
  try {
    const page = await context.newPage();
    await page.goto('/login');
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).waitFor();
    await setAuthInputs(page, process.env.MECHAT_E2E_IDENTIFIER, process.env.MECHAT_E2E_PASSWORD);
    await page.getByRole('button', {
      name: "Đăng nhập",
      exact: true
    }).click();
    await expect(page).toHaveURL(/\/chat$/);
    await expect(page.getByRole('heading', {
      name: "Tin nhắn."
    })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', {
      name: "Tin nhắn."
    })).toBeVisible();
    await page.getByRole('button', {
      name: "Đăng xuất",
      exact: true
    }).filter({
      visible: true
    }).first().click();
    await expect(page).toHaveURL(/\/login$/);
  } finally {
    await context.close();
  }
});
