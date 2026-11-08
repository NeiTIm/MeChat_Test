// Synthetic images and HTTP simulation only. Never authenticated Backend acceptance.
import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'
import { settleMotion } from './review-helpers'

async function fixture(context, page, { mime = 'image/png', corrupt = false, status = 200, network = false, expireOnce = false } = {}) {
  const bytes = await page.evaluate((mime) => {
    const canvas = document.createElement('canvas')
    canvas.width = 400; canvas.height = 240
    const ctx = canvas.getContext('2d')
    ctx.fillStyle = '#4f46e5'; ctx.fillRect(0, 0, 400, 240)
    ctx.fillStyle = '#fff'; ctx.font = '24px sans-serif'; ctx.fillText('MeChat · Kiểm thử', 30, 130)
    return canvas.toDataURL(mime).split(',')[1]
  }, mime.startsWith('image/') ? mime : 'image/png')
  let response = { mime, corrupt, status, network }
  const requests = []
  const file = { id: 'synthetic-image', fileName: `anh-kiem-thu.${mime === 'image/jpeg' ? 'jpg' : mime === 'image/webp' ? 'webp' : 'png'}`, contentType: mime.startsWith('image/') ? mime : 'image/png', sizeBytes: 1200 }
  const conversation = { id: 'media-room', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 1, updatedAt: '2026-11-07T00:00:00Z' }
  await accountFixture(context, page, async (route) => {
    const req = route.request(), path = new URL(req.url()).pathname
    if (path === `/api/files/${file.id}/preview` || path === `/api/files/${file.id}/download`) {
      requests.push({ path, authorized: req.headers().authorization === 'Bearer test-only' })
      if (response.network) { await route.abort('failed'); return true }
      await route.fulfill({ status: expireOnce && requests.length === 1 ? 401 : response.status, contentType: response.mime, body: response.corrupt ? Buffer.from('broken image for test') : Buffer.from(bytes, 'base64') })
      return true
    }
    const data = path === '/api/conversations' ? [conversation]
      : path === `/api/conversations/${conversation.id}` ? conversation
        : path === `/api/conversations/${conversation.id}/messages` ? { items: [{ id: 'photo-message', conversationId: conversation.id, senderUserId: 'friend', sequence: 1, createdAt: '2026-11-07T00:00:00Z', attachments: [file] }], hasMore: false }
          : path === '/api/users/friend' ? { id: 'friend', displayName: 'Người kiểm thử', username: 'test' }
            : path.endsWith('/presence') ? { online: false, lastSeenAt: null }
              : path === '/api/unread' ? { mainCount: 0, requestCount: 0, notificationCount: 0, conversations: [] }
                : path === '/api/message-requests' ? []
                  : path.endsWith('/unread') || path.endsWith('/unread-count') ? { count: 0 }
                    : path.endsWith('/context') ? { reactions: [], reply: null, myReaction: null }
                      : path.endsWith('/receipts') ? { status: 'Sent', deliveredCount: 0, seenCount: 0 } : undefined
    if (data !== undefined) { await route.fulfill({ json: data }); return true }
    if (req.method() !== 'GET' && !path.startsWith('/api/auth/')) { await route.fulfill({ status: 204 }); return true }
    return false
  })
  return { file, requests, respond: (next) => { response = { ...response, ...next } } }
}

for (const mime of ['image/png', 'image/jpeg', 'image/webp']) test(`MC-011 authorized ${mime} decodes, zooms and releases object URLs (client simulation)`, async ({ page, context }) => {
  const { requests } = await fixture(context, page, { mime })
  await page.addInitScript(() => {
    const create = URL.createObjectURL.bind(URL), revoke = URL.revokeObjectURL.bind(URL)
    window.__testBlobCounts = { created: 0, revoked: 0 }
    URL.createObjectURL = (blob) => { window.__testBlobCounts.created++; return create(blob) }
    URL.revokeObjectURL = (url) => { window.__testBlobCounts.revoked++; return revoke(url) }
  })
  const errors = []
  page.on('pageerror', (error) => errors.push(error.message))
  await page.goto('/chat/media-room')
  await page.locator('.attachment-card').click()
  const picture = page.locator('.file-preview-dialog .attachment-image')
  await expect(picture).toBeVisible()
  await expect.poll(() => picture.evaluate((img) => img.complete && img.naturalWidth)).toBe(400)
  expect(await picture.getAttribute('src')).toMatch(/^blob:/)
  await page.getByRole('button', { name: 'Phóng to ảnh', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Vừa khung', exact: true })).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).not.toBeVisible()
  await expect.poll(() => page.evaluate(() => window.__testBlobCounts.revoked)).toBeGreaterThan(0)
  for (let round = 0; round < 2; round++) {
    await page.locator('.attachment-card').click()
    await expect(page.locator('.file-preview-dialog .attachment-image')).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(page.getByRole('dialog')).not.toBeVisible()
  }
  await page.goto('/account/interface')
  await expect.poll(() => page.evaluate(() => window.__testBlobCounts.created - window.__testBlobCounts.revoked)).toBe(0)
  expect(requests.length).toBeGreaterThan(0)
  expect(requests.every((r) => r.authorized)).toBe(true)
  expect(errors).toEqual([])
})

for (const response of [{ corrupt: true }, { mime: 'application/json' }, { status: 403 }, { status: 404 }, { network: true }]) test(`MC-011/012 image failure has safe retry and download fallback ${JSON.stringify(response)} (client simulation)`, async ({ page, context }) => {
  const { respond } = await fixture(context, page, response)
  await page.setViewportSize({ width: 320, height: 740 })
  await page.goto('/chat/media-room')
  await page.locator('.attachment-card').click()
  await expect(page.getByRole('dialog').getByRole('alert')).toBeVisible()
  await expect(page.getByRole('dialog').getByRole('button', { name: 'Tải về', exact: true })).toBeVisible()
  respond({ mime: 'image/png', status: 200, corrupt: false, network: false })
  await page.getByRole('dialog').getByRole('button', { name: 'Thử lại', exact: true }).click()
  await expect.poll(() => page.locator('.file-preview-dialog img').evaluate((img) => img.complete && img.naturalWidth)).toBe(400)
})

test('MC-011 expired access refresh retries the authorized image consumer (client simulation)', async ({ page, context }) => {
  const { requests } = await fixture(context, page, { expireOnce: true })
  await page.goto('/chat/media-room')
  await page.locator('.attachment-card').click()
  await expect.poll(() => page.locator('.file-preview-dialog img').evaluate((img) => img.complete && img.naturalWidth)).toBe(400)
  expect(requests.length).toBeGreaterThanOrEqual(2)
  expect(requests.every((request) => request.authorized)).toBe(true)
})

for (const width of [320, 390, 768, 1366, 1440]) for (const theme of ['light', 'dark']) test(`MC-012 image ${width}px ${theme} geometry and a11y (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  await page.goto('/chat/media-room')
  await page.locator('.attachment-card').click()
  const picture = page.locator('.file-preview-dialog img')
  await expect.poll(() => picture.evaluate((img) => img.complete && img.naturalWidth)).toBe(400)
  await settleMotion(page)
  const rect = await picture.boundingBox()
  expect(rect.x).toBeGreaterThanOrEqual(0)
  expect(rect.x + rect.width).toBeLessThanOrEqual(width)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/improvements', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/improvements/media-${width}-${theme}.png` })
})
