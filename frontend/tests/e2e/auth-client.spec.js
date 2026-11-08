import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'

test('public auth forms validate in Vietnamese and preserve request contracts (client simulation)', async ({ page }) => {
  const requests = []
  await page.route('**/api/auth/**', async (route) => {
    requests.push({ path: new URL(route.request().url()).pathname, data: route.request().postDataJSON() })
    return route.fulfill({ status: 200, json: { message: 'Server copy must not be shown' } })
  })
  await page.goto('/register')
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click()
  await expect(page.getByText('Vui lòng nhập thông tin này.')).toBeVisible()
  expect(requests).toHaveLength(0)
  await page.getByLabel('Email', { exact: true }).fill('minhanh@example.test')
  await page.getByLabel('Tên đăng nhập', { exact: true }).fill('minhanh')
  await page.getByLabel('Mật khẩu', { exact: true }).fill(' test-only-1234 ')
  await page.getByLabel('Xác nhận mật khẩu', { exact: true }).fill('test-only-1234')
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click()
  await expect(page.getByText('Mật khẩu xác nhận chưa trùng khớp.')).toBeVisible()
  await page.getByLabel('Xác nhận mật khẩu', { exact: true }).fill(' test-only-1234 ')
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click()
  await expect(page.getByRole('status')).toContainText('Đã tiếp nhận đăng ký')
  expect(requests.at(-1)).toEqual({ path: '/api/auth/register', data: { email: 'minhanh@example.test', username: 'minhanh', password: ' test-only-1234 ' } })
  await expect(page.getByText('Server copy must not be shown')).toHaveCount(0)
  for (const [path, fieldValues, submit, expected] of [
    ['/verify-email', { 'Mã xác nhận': 'test-token' }, 'Xác minh email', { token: 'test-token' }],
    ['/resend-verification', { Email: 'minhanh@example.test' }, 'Gửi lại mã xác minh', { email: 'minhanh@example.test' }],
    ['/forgot-password', { Email: 'minhanh@example.test' }, 'Gửi hướng dẫn', { email: 'minhanh@example.test' }],
    ['/reset-password', { 'Mã xác nhận': 'test-token', 'Mật khẩu mới': 'test-only-1234', 'Xác nhận mật khẩu': 'test-only-1234' }, 'Đặt lại mật khẩu', { token: 'test-token', newPassword: 'test-only-1234' }],
    ['/reactivate', { 'Email hoặc tên đăng nhập': 'minhanh', 'Mật khẩu': 'test-only-1234' }, 'Kích hoạt lại', { identifier: 'minhanh', password: 'test-only-1234' }],
  ]) {
    await page.goto(path)
    for (const [label, value] of Object.entries(fieldValues)) await page.getByLabel(label, { exact: true }).fill(value)
    await page.getByRole('button', { name: submit, exact: true }).click()
    await expect(page.getByRole('status')).toBeVisible()
    expect(requests.at(-1)).toEqual({ path: `/api/auth${path}`, data: expected })
  }
})

for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) {
  test(`Vietnamese register/account ${width}px ${theme}: geometry and accessibility (client simulation)`, async ({ page, context }) => {
    await page.setViewportSize({ width, height: 900 })
    await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
    await accountFixture(context, page)
    await mkdir('../docs/frontend/screenshots/vi/auth', { recursive: true })
    for (const [route, name] of [['/register', 'register'], ['/account', 'profile']]) {
      await page.goto(route)
      await expect(page.getByRole('heading', { name: name === 'register' ? 'Tạo tài khoản MeChat' : 'Thông tin cá nhân', exact: true })).toBeVisible()
      await expect(page.locator('html')).toHaveAttribute('lang', 'vi')
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
      expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
      await page.screenshot({ path: `../docs/frontend/screenshots/vi/auth/${name}-${width}-${theme}.png`, fullPage: true })
    }
  })
}

test('profile/privacy/session actions use DTOs, confirmation and safe errors (client simulation)', async ({ page, context }) => {
  const requests = []
  let sessions = [{ id: 'other-session', deviceName: 'Thiết bị thử nghiệm', createdAt: '2026-11-06T07:30:00Z', lastActivityAt: '2026-11-06T07:30:00Z', isCurrent: false }]
  await accountFixture(context, page, async (route, me) => {
    const req = route.request(), path = new URL(req.url()).pathname
    if (req.method() !== 'GET' && !path.endsWith('/refresh')) {
      requests.push({ path, data: req.postDataJSON() })
      if (path === '/api/users/me') { Object.assign(me, req.postDataJSON()); await route.fulfill({ json: me }); return true }
      if (path.includes('sessions/')) sessions = []
      await route.fulfill({ status: 204 }); return true
    }
    if (path.endsWith('/privacy')) { await route.fulfill({ json: { showOnline: true, showLastSeen: true, allowStrangerMessages: false, allowGroupInvites: true } }); return true }
    if (path.endsWith('/sessions')) { await route.fulfill({ json: sessions }); return true }
    return false
  })
  await page.goto('/account')
  await page.getByLabel('Tên hiển thị', { exact: true }).fill('Minh Anh Nguyễn')
  await page.locator('form').filter({ has: page.getByLabel('Tên hiển thị', { exact: true }) }).getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(page.locator('.form-feedback').filter({ hasText: 'Đã lưu hồ sơ.' })).toBeVisible()
  expect(requests.at(-1).data).toEqual({ displayName: 'Minh Anh Nguyễn', bio: '' })
  await page.goto('/account/privacy')
  await page.getByRole('checkbox', { name: /Hiển thị trạng thái trực tuyến/ }).uncheck()
  await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(page.locator('.form-feedback').filter({ hasText: 'Đã lưu quyền riêng tư.' })).toBeVisible()
  expect(requests.at(-1).data).toEqual({ showOnline: false, showLastSeen: true, allowStrangerMessages: false, allowGroupInvites: true })
  await page.goto('/account/sessions')
  await expect(page.getByText('Hoạt động gần nhất: 06/11/2026 · 14:30')).toBeVisible()
  await page.getByRole('button', { name: 'Thu hồi phiên', exact: true }).click()
  expect(requests.some((r) => r.path.endsWith('other-session'))).toBe(false)
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByText('Không có phiên đăng nhập')).toBeVisible()
  expect(requests.at(-1).path).toBe('/api/auth/sessions/other-session')
})
