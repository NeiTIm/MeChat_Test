// Synthetic 500-message history. This is neither live SQL load nor a Lighthouse certification.
import { test, expect } from '@playwright/test'
import { writeFile } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'

test('QA-012/034 paginated 500-message scroll anchor and throttled input profile (client simulation)', async ({ page, context }) => {
  test.setTimeout(120000)
  const room = { id: 'long-history', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 500, updatedAt: '2026-11-07T00:00:00Z' }
  const pages = []
  await accountFixture(context, page, async (route) => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    let data
    if (path === '/api/conversations') data = [room]
    else if (path === '/api/conversations/long-history') data = room
    else if (path === '/api/conversations/long-history/messages') {
      const end = Number(url.searchParams.get('beforeSequence') || 501) - 1, start = Math.max(1, end - 29)
      pages.push({ before: url.searchParams.get('beforeSequence'), limit: url.searchParams.get('limit') })
      data = { items: Array.from({ length: end - start + 1 }, (_, i) => ({ id: `long-${end - i}`, conversationId: room.id, senderUserId: 'friend', sequence: end - i, content: `Tin nhắn kiểm thử ${end - i}. ` + 'Nội dung tổng hợp để đo cuộn và giữ vị trí đọc. '.repeat(3), createdAt: '2026-11-07T00:00:00Z' })), nextCursor: start, hasMore: start > 1 }
    } else if (path === '/api/users/friend') data = { id: 'friend', displayName: 'Bạn kiểm thử' }
    else if (path === '/api/message-requests') data = []
    else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, notificationCount: 0, conversations: [] }
    else if (path.endsWith('/unread') || path.endsWith('/unread-count')) data = { count: 0 }
    else if (path.endsWith('/presence')) data = { online: null, lastSeenAt: null }
    else if (path.endsWith('/context')) data = { reply: null, reactions: [], myReaction: null }
    if (data !== undefined) { await route.fulfill({ json: data }); return true }
    if (req.method() !== 'GET' && !path.startsWith('/api/auth/')) { await route.fulfill({ status: 204 }); return true }
    return false
  })
  const errors = []
  page.on('pageerror', (error) => errors.push(error.message))
  await page.setViewportSize({ width: 390, height: 844 })
  const started = Date.now()
  await page.goto('/chat/long-history')
  await expect(page.locator('.message-entry')).toHaveCount(30)
  let maxAnchorShift = 0
  for (let batch = 1; batch <= 16; batch++) {
    const scroll = page.locator('.message-scroll')
    await scroll.evaluate((node) => { node.scrollTop = 0; node.dispatchEvent(new Event('scroll')) })
    const anchor = page.locator('.message-entry').first()
    const id = await anchor.getAttribute('data-message-id')
    // Follow message content, not the wrapper's date divider which moves to
    // the first older row after prepend.
    const before = await anchor.locator('.message-bubble').evaluate((node) => node.getBoundingClientRect().top)
    await page.getByRole('button', { name: 'Tải tin nhắn trước', exact: true }).click()
    await expect(page.locator('.message-entry')).toHaveCount(Math.min(500, 30 * (batch + 1)))
    const after = await page.locator(`[data-message-id="${id}"] .message-bubble`).evaluate((node) => node.getBoundingClientRect().top)
    maxAnchorShift = Math.max(maxAnchorShift, Math.abs(after - before))
  }
  expect(maxAnchorShift).toBeLessThanOrEqual(3)
  expect(pages).toHaveLength(17)
  expect(pages.every((page) => page.limit === '30')).toBe(true)
  const cdp = await context.newCDPSession(page)
  await cdp.send('Performance.enable')
  await cdp.send('Emulation.setCPUThrottlingRate', { rate: 4 })
  const inputTimes = []
  for (const text of ['Xin chào', 'Tiếng Việt có dấu', 'Nhiều dòng\nVẫn giữ nội dung']) {
    const start = Date.now()
    await page.getByRole('textbox', { name: 'Tin nhắn', exact: true }).fill(text)
    await page.evaluate(() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve))))
    inputTimes.push(Date.now() - start)
  }
  const metrics = await cdp.send('Performance.getMetrics')
  await cdp.send('Emulation.setCPUThrottlingRate', { rate: 1 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect(errors).toEqual([])
  await writeFile('../docs/frontend/improvement-client-performance.json', JSON.stringify({ capturedAt: new Date().toISOString(), scope: 'Synthetic 500 incoming messages, 17 real consumer cursor requests mocked in test. Chromium 390px, input sampling under 4x CPU throttle; includes automation overhead, not production load or Web Vitals.', messages: 500, pages: pages.length, maxAnchorShift, elapsedMs: Date.now() - started, inputSamplesMs: inputTimes, heapUsedBytes: metrics.metrics.find((metric) => metric.name === 'JSHeapUsedSize')?.value, errors }, null, 2) + '\n')
})
