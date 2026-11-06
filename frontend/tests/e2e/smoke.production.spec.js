import { test, expect } from '@playwright/test'
test('built production routes load Vietnamese UI and exclude development design routes', async ({ page }) => {
  const errors = []
  page.on('pageerror', (error) => errors.push(error.message))
  await page.goto('/login')
  await expect(page.getByRole('button', { name: 'Đăng nhập', exact: true })).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('lang', 'vi')
  await page.goto('/register')
  await expect(page.getByRole('heading', { name: 'Tạo tài khoản MeChat', exact: true })).toBeVisible()
  await page.goto('/design/private')
  await expect(page.getByRole('heading', { name: 'Không tìm thấy trang', exact: true })).toBeVisible()
  await expect(page.locator('.review-toolbar')).toHaveCount(0)
  expect(errors).toEqual([])
})
