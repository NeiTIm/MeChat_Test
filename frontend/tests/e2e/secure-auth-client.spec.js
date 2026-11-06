import { test, expect } from '@playwright/test'
import { setAuthInputs } from './secure-auth-input'
test('safe live-input helper reaches the actual login form without logging input arguments (client simulation)', async ({ page }) => {
  let input
  await page.route('**/api/auth/login', async (route) => { input = route.request().postDataJSON(); await route.fulfill({ status: 401, json: { code: 'INVALID_CREDENTIALS_OR_ACCOUNT_STATE' } }) })
  await page.goto('/login')
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).waitFor()
  await setAuthInputs(page, 'synthetic-test-user', 'synthetic-test-password')
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
  await expect.poll(() => input).toEqual({ identifier: 'synthetic-test-user', password: 'synthetic-test-password', deviceName: 'MeChat Web' })
  await expect(page).toHaveURL(/\/login$/)
})
