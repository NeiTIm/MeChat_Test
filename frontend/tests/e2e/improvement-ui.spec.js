// Workbook acceptance checks with synthetic data and HTTP simulation only.
import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'
import { settleMotion } from './review-helpers'

async function fixture(context, page, { presence = true, count = 2, failure = false, delay = false } = {}) {
  let read = false
  const calls = []
  const room = { id: 'room', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 0, updatedAt: '2026-11-07T00:00:00Z' }
  await accountFixture(context, page, async (route, me) => {
    const req = route.request(), path = new URL(req.url()).pathname
    if (path.startsWith('/api/auth/')) return false
    if (path === '/api/users/me' && req.method() === 'PATCH') { calls.push(path); if (failure) await route.fulfill({ status: 500, json: { message: 'private raw exception test-only' } }); else { Object.assign(me, req.postDataJSON()); await route.fulfill({ json: me }) } return true }
    if (path.endsWith('/read') || path.endsWith('/read-all')) { calls.push(path); read = true; await route.fulfill({ status: 204 }); return true }
    let data
    if (path === '/api/conversations') data = [room]
    else if (path === '/api/conversations/room') data = room
    else if (path === '/api/conversations/room/messages') data = { items: [], hasMore: false }
    else if (path === '/api/users/friend') data = { id: 'friend', displayName: 'Bạn kiểm thử', username: 'test' }
    else if (path.endsWith('/presence')) data = { online: presence, lastSeenAt: null }
    else if (path === '/api/notifications') {
      if (delay) await new Promise((resolve) => setTimeout(resolve, 700))
      if (failure) { await route.fulfill({ status: 500 }); return true }
      data = { items: count ? [{ id: 'notice', type: 'Message', referenceId: 'sample', createdAt: '2026-11-07T00:00:00Z', readAt: read ? '2026-11-07T00:05:00Z' : null, text: 'Thông báo kiểm thử không chứa dữ liệu riêng tư.' }]: [], hasMore: false }
    } else if (path === '/api/notifications/unread-count') data = { count: read ? 0 : count }
    else if (path === '/api/unread') data = { mainCount: 14, requestCount: 3, conversations: [{ conversationId: 'unrendered-private', type: 'Private', count: 8 }, { conversationId: 'unrendered-group', type: 'Group', count: 6 }], notificationCount: read ? 0 : count }
    else if (path === '/api/message-requests') data = []
    else if (path.endsWith('/unread-count')) data = { count: 3 }
    else if (path.endsWith('/unread')) data = { count: 0 }
    else if (path.endsWith('/settings')) data = { mutedUntil: null }
    else if (path === '/api/messages/sample') data = { id: 'sample', conversationId: room.id }
    if (data !== undefined) { await route.fulfill({ json: data }); return true }
    if (req.method() !== 'GET') { await route.fulfill({ status: 204 }); return true }
    return false
  })
  return calls
}

test('MC-002 counts use server projection including unrendered conversations and do not clear on navigation (client simulation)', async ({ context, page }) => {
  await fixture(context, page)
  await page.goto('/chat')
  const nav = page.locator('.rail-nav')
  await expect(nav.locator('a[href="/chat"] .nav-badge')).toHaveText('11')
  await expect(nav.locator('a[href="/groups"] .nav-badge')).toHaveText('6')
  await expect(nav.locator('a[href="/notifications"] .nav-badge')).toHaveText('2')
  await expect(nav.locator('a[href="/friends"] .nav-badge')).toHaveCount(0)
  await nav.locator('a[href="/notifications"]').click()
  await expect(nav.locator('a[href="/notifications"] .nav-badge')).toHaveText('2')
  await page.getByRole('button', { name: 'Đánh dấu tất cả đã đọc', exact: true }).click()
  await expect(nav.locator('a[href="/notifications"] .nav-badge')).toHaveCount(0)
})
for (const presence of [true, false, null]) test(`MC-008 privacy filtered presence ${presence} (client simulation)`, async ({ context, page }) => {
  await fixture(context, page, { presence })
  await page.goto('/chat/room')
  if (presence === null) await expect(page.locator('.presence-dot')).toHaveCount(0)
  else { await expect(page.locator(presence ? '.presence-online' : '.presence-offline')).toBeVisible(); await expect(page.locator('.thread-heading')).toContainText(presence ? 'Đang trực tuyến' : 'Đang ngoại tuyến') }
})
test('MC-009 local toast preference retains inline success and persistent safe errors (client simulation)', async ({ context, page }) => {
  await fixture(context, page)
  await page.goto('/account')
  const form = page.locator('form').filter({ has: page.getByLabel('Tên hiển thị', { exact: true }) })
  await page.getByLabel('Tên hiển thị', { exact: true }).fill('Tên kiểm thử')
  await form.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(page.locator('.app-toast')).toContainText('Đã lưu hồ sơ.')
  await page.getByRole('button', { name: 'Đóng thông báo nhanh', exact: true }).click()
  await expect(page.locator('.app-toast')).toHaveCount(0)
  await page.goto('/account/interface')
  await page.getByRole('checkbox').uncheck()
  await page.reload()
  await expect(page.getByRole('checkbox')).not.toBeChecked()
  await page.goto('/account')
  await page.getByLabel('Tên hiển thị', { exact: true }).fill('Lưu với phản hồi thường')
  await form.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(form.locator('.form-feedback')).toContainText('Đã lưu hồ sơ.')
  await expect(page.locator('.app-toast')).toHaveCount(0)
})
test('QA-040 important errors remain readable after toast duration and never expose raw server details (client simulation)', async ({ context, page }) => {
  await fixture(context, page, { failure: true })
  await page.goto('/account')
  const form = page.locator('form').filter({ has: page.getByLabel('Tên hiển thị', { exact: true }) })
  await page.getByLabel('Tên hiển thị', { exact: true }).fill('Tên kiểm thử')
  await form.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(form.getByRole('alert')).toBeVisible()
  await page.waitForTimeout(2200)
  await expect(form.getByRole('alert')).toBeVisible()
  await expect(page.locator('body')).not.toContainText('private raw exception')
})
for (const state of ['loading', 'empty', 'error']) test(`MC-007 preview ${state} state and Escape (client simulation)`, async ({ context, page }) => {
  await fixture(context, page, { count: state === 'empty' ? 0 : 2, failure: state === 'error', delay: state === 'loading' })
  await page.goto('/chat/room')
  await page.getByRole('button', { name: /^Xem nhanh thông báo/ }).click()
  const tray = page.locator('.notification-preview')
  if (state === 'loading') await expect(tray.getByRole('status')).toBeVisible()
  if (state === 'empty') await expect(tray).toContainText('Những cập nhật mới sẽ xuất hiện ở đây.')
  if (state === 'error') await expect(tray.getByRole('alert')).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(tray).not.toBeVisible()
  await expect(page.getByRole('button', { name: /^Xem nhanh thông báo/ })).toBeFocused()
})

for (const width of [320, 390, 768, 1366, 1440]) for (const theme of ['light', 'dark']) test(`MC-001/003/007/009 ${width}px ${theme} alignment, preview geometry and accessibility (client simulation)`, async ({ context, page }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  // Login is public. No credentials entered.
  await page.goto('/login')
  const alignment = await page.locator('.login-submit').evaluate((button) => {
    const text = [...button.childNodes].find((node) => node.nodeType === Node.TEXT_NODE && node.textContent.trim())
    const range = document.createRange(); range.selectNodeContents(text)
    const label = range.getBoundingClientRect(), rect = button.getBoundingClientRect(), form = button.closest('form').getBoundingClientRect()
    return { delta: Math.abs(label.x + label.width / 2 - rect.x - rect.width / 2), edges: Math.abs(rect.x - form.x) + Math.abs(rect.width - form.width) }
  })
  expect(alignment.delta).toBeLessThan(2)
  expect(alignment.edges).toBeLessThan(2)
  await mkdir('../docs/frontend/screenshots/vi/improvements', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/improvements/login-${width}-${theme}.png`, fullPage: true })
  const recoveryLink = page.getByRole('link', { name: 'Quên mật khẩu?', exact: true })
  await recoveryLink.hover()
  expect(await recoveryLink.evaluate((node) => getComputedStyle(node).textDecorationLine)).toContain('underline')
  await recoveryLink.focus()
  expect(await recoveryLink.evaluate((node) => parseFloat(getComputedStyle(node).outlineWidth))).toBeGreaterThanOrEqual(2)
  await fixture(context, page)
  await page.goto('/chat/room')
  await page.getByRole('button', { name: /^Xem nhanh thông báo/ }).click()
  const tray = page.locator('.notification-preview')
  await expect(tray).toContainText('Thông báo kiểm thử không chứa dữ liệu riêng tư.')
  await settleMotion(page)
  const bounds = await tray.boundingBox(), composer = await page.locator('.composer').boundingBox()
  expect(bounds.x).toBeGreaterThanOrEqual(0)
  expect(bounds.x + bounds.width).toBeLessThanOrEqual(width)
  expect(bounds.y + bounds.height).toBeLessThanOrEqual(composer.y)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/improvements', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/improvements/preview-${width}-${theme}.png` })
  await page.keyboard.press('Escape')
  // Geometry of the global viewport, separate from mutation contract tests.
  await page.evaluate(async () => { const { showSuccess } = await import('/src/stores/ui-feedback-store.js'); showSuccess('Đã lưu thay đổi kiểm thử.') })
  await expect(page.locator('.app-toast')).toBeVisible()
  await settleMotion(page)
  const toast = await page.locator('.app-toast').boundingBox(), header = await page.locator('.thread-header').boundingBox()
  expect(toast.y).toBeGreaterThanOrEqual(header.y + header.height)
  expect(toast.y + toast.height).toBeLessThanOrEqual(composer.y)
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/improvements/toast-${width}-${theme}.png` })
  await page.getByRole('button', { name: 'Đóng thông báo nhanh', exact: true }).click()
  await page.goto('/account/interface')
  await expect(page.getByRole('checkbox')).toBeChecked()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
})
