import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { settleMotion } from './review-helpers'

// Keep G2 visual capture available; fast targeted runs can explicitly avoid PNG output.
const captureScreenshots = process.env.BCR_CAPTURE_SCREENSHOTS !== 'false'

// All HTTP is intercepted. These results NEVER establish SQL/real JWT/SignalR acceptance.
async function fixture(page, context, { emptyScan = false, failDelete = false, seenFailure = false, rows = 1 } = {}) {
  await context.addCookies([{ name: 'webchat_csrf', value: 'test-only', url: 'http://127.0.0.1:5183' }])
  const calls = [], now = '2026-11-08T10:00:00.1234567Z'
  let notifications = [{ id: 'unread', type: 'AdminWarning', createdAt: now, readAt: null }, { id: 'read', type: 'AdminWarning', createdAt: now, readAt: now }], trash = []
  const incoming = Array.from({ length: rows }, (_, i) => ({ id: `request-${i}`, senderUserId: `sender-${i}`, receiverUserId: 'actor', status: 'Pending', createdAt: now, seenAt: null }))
  await page.route('**/hubs/**', route => route.fulfill({ status: 503 }))
  await page.route('**/api/**', async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname, method = request.method()
    let data
    if (method === 'GET' || path === '/api/auth/refresh') {
      if (path === '/api/auth/refresh') data = { accessToken: 'test-only', expiresAt: '2099-01-01T00:00:00Z', userId: 'actor', username: 'test' }
      else if (path === '/api/users/me') data = { id: 'actor', displayName: 'Minh Anh', username: 'test', systemRole: 'User' }
      else if (path === '/api/unread') data = { requestCount: 7, notificationCount: 1, conversations: [] }
      else if (path === '/api/friends/requests/unread-count') data = { unreadCount: incoming.filter(r => !r.seenAt).length }
      else if (path === '/api/friends/requests') data = { incoming, outgoing: [] }
      else if (path === '/api/friends' || path === '/api/blocks') data = []
      else if (path.startsWith('/api/users/sender-')) data = { id: path.split('/').at(-1), displayName: 'Người gửi thử nghiệm', username: 'fixture' }
      else if (path === '/api/notifications/unread-count') data = { count: notifications.filter(n => !n.readAt).length }
      else if (path === '/api/notifications') data = { items: notifications, hasMore: false }
      else if (path === '/api/notifications/trash') {
        calls.push({ path, before: url.searchParams.get('before'), beforeId: url.searchParams.get('beforeId') })
        data = emptyScan && !url.searchParams.get('before') ? { items: [], hasMore: true, nextCursor: now, nextCursorId: 'raw-last' } : { items: trash, hasMore: false }
      }
      if (data !== undefined) return route.fulfill({ json: data })
      return route.fulfill({ status: 404 })
    }
    calls.push({ path, method, body: request.postDataJSON() })
    if (failDelete && method === 'DELETE') return route.fulfill({ status: 404, json: { code: 'NOTIFICATION_UNAVAILABLE' } })
    if (path === '/api/notifications/read') {
      trash.push(...notifications.filter(n => n.readAt).map(n => ({ ...n, deletedAt: now })))
      notifications = notifications.filter(n => !n.readAt)
    } else if (method === 'DELETE' && path.startsWith('/api/notifications/')) {
      const id = path.split('/')[3]; trash.push(...notifications.filter(n => n.id === id).map(n => ({ ...n, deletedAt: now }))); notifications = notifications.filter(n => n.id !== id)
    } else if (path.endsWith('/restore')) {
      const id = path.split('/')[3]; notifications.push(...trash.filter(n => n.id === id).map(n => ({ ...n, deletedAt: null }))); trash = trash.filter(n => n.id !== id)
    } else if (path === '/api/friends/requests/mark-seen') {
      if (seenFailure) return route.fulfill({ status: 500 })
      for (const row of incoming) if (request.postDataJSON().requestIds.includes(row.id)) row.seenAt = now
    }
    return route.fulfill({ status: 204 })
  })
  return calls
}

test('delete unread and restore preserves unread state; bulk affects read only (HTTP simulation)', async ({ page, context }) => {
  const calls = await fixture(page, context)
  await page.goto('/notifications')
  await page.locator('.notification-card').filter({ hasText: 'Chưa đọc' }).getByRole('button', { name: 'Xóa', exact: true }).click()
  await expect(page.locator('.notification-card')).toHaveCount(1)
  await page.getByRole('button', { name: 'Thùng rác', exact: true }).click()
  await expect(page.locator('.notification-card')).toHaveCount(1)
  await page.getByRole('button', { name: 'Khôi phục', exact: true }).click()
  await expect(page.getByText('Thùng rác trống')).toBeVisible()
  await page.getByRole('button', { name: 'Thông báo hiện tại', exact: true }).click()
  await expect(page.locator('.notification-card.unread')).toHaveCount(1)
  await page.getByRole('button', { name: 'Xóa thông báo đã đọc', exact: true }).click()
  await page.getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByRole('dialog')).toHaveCount(0)
  await expect(page.locator('.notification-card')).toHaveCount(1)
  expect(calls.filter(c => c.method === 'DELETE').map(c => c.path)).toEqual(['/api/notifications/unread', '/api/notifications/read'])
})

test('empty scan page keeps precise cursor and has a usable continuation', async ({ page, context }) => {
  const calls = await fixture(page, context, { emptyScan: true })
  await page.goto('/notifications'); await page.getByRole('button', { name: 'Thùng rác', exact: true }).click()
  await expect(page.getByText('Chưa có thông báo hiển thị ở trang này')).toBeVisible()
  await page.getByRole('button', { name: 'Xem thêm', exact: true }).click()
  await expect(page.getByText('Thùng rác trống')).toBeVisible()
  expect(calls.filter(c => c.path.endsWith('/trash')).at(-1)).toMatchObject({ before: '2026-11-08T10:00:00.1234567Z', beforeId: 'raw-last' })
})

test('delete error remains readable and does not fake-remove notification', async ({ page, context }) => {
  await fixture(page, context, { failDelete: true }); await page.goto('/notifications')
  await page.locator('.notification-card').first().getByRole('button', { name: 'Xóa', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Thông báo không còn khả dụng')
  await expect(page.locator('.notification-card')).toHaveCount(2)
})

test('friend badge uses dedicated server count; visible rows only are marked', async ({ page, context }) => {
  const calls = await fixture(page, context, { rows: 12 })
  await page.setViewportSize({ width: 390, height: 700 }); await page.goto('/friends/list')
  await page.waitForTimeout(650)
  expect(calls.filter(c => c.path.endsWith('/mark-seen'))).toHaveLength(0)
  await expect(page.locator('a[href="/friends"] .nav-badge').first()).toHaveText('12')
  await page.locator('a[href="/friends/incoming"]').click()
  await expect.poll(() => calls.filter(c => c.path.endsWith('/mark-seen')).length).toBeGreaterThan(0)
  const marked = calls.filter(c => c.path.endsWith('/mark-seen')).flatMap(c => c.body.requestIds)
  expect(marked.length).toBeLessThan(12); expect(marked).not.toContain('request-11')
  // Pending rows remain in the list; mark-seen never accepts/rejects them.
  await expect(page.locator('.person-row')).toHaveCount(12)
})

for (const width of [320, 390, 768, 1366, 1440]) for (const dark of [false, true]) {
  test(`notification lifecycle responsive ${width}px ${dark ? 'dark' : 'light'} + keyboard/a11y`, async ({ page, context }) => {
    await fixture(page, context)
    await page.setViewportSize({ width, height: 900 })
    await page.addInitScript(value => localStorage.setItem('mechat-theme', value), dark ? 'dark' : 'light')
    const errors = []; page.on('pageerror', error => errors.push(error.message))
    await page.goto('/notifications'); await expect(page.locator('.notification-card')).toHaveCount(2)
    if (dark) await page.evaluate(() => document.documentElement.classList.add('dark'))
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    if (captureScreenshots) await mkdir('test-results/bcr/screenshots', { recursive: true })
    await settleMotion(page)
    if (captureScreenshots) await page.screenshot({ path: `test-results/bcr/screenshots/active-${width}-${dark ? 'dark' : 'light'}.png`, fullPage: true, animations: 'disabled' })
    const bulk = page.getByRole('button', { name: 'Xóa thông báo đã đọc', exact: true })
    await bulk.focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('dialog')).toBeVisible()
    await settleMotion(page)
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    if (captureScreenshots) await page.screenshot({ path: `test-results/bcr/screenshots/confirm-${width}-${dark ? 'dark' : 'light'}.png`, fullPage: true, animations: 'disabled' })
    await page.keyboard.press('Escape'); await expect(bulk).toBeFocused()
    await page.getByRole('button', { name: 'Thùng rác', exact: true }).click()
    await expect(page.getByText('Thùng rác trống')).toBeVisible()
    await settleMotion(page)
    const audit = await new AxeBuilder({ page }).analyze(); expect(audit.violations).toEqual([])
    if (captureScreenshots) await page.screenshot({ path: `test-results/bcr/screenshots/trash-${width}-${dark ? 'dark' : 'light'}.png`, fullPage: true, animations: 'disabled' })
    expect(errors).toEqual([])
  })
}
