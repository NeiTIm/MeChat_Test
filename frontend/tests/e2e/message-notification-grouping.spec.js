import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { accountFixture } from './account-client-fixture'

// Consumes an already-grouped server projection. This does not prove SQL grouping.
async function fixture(context, page) {
  const calls = []
  const room = {
    id: 'room',
    type: 'Private',
    status: 'Active',
    otherUserId: 'peer',
    lastSequence: 37,
  }
  let read = false
  const rows = [
    {
      id: 'private-first',
      type: 'Message',
      referenceId: 'first-message',
      createdAt: '2026-11-16T01:00:00Z',
      readAt: null,
    },
    {
      id: 'group-first',
      type: 'GroupMessage',
      referenceId: 'first-group-message',
      createdAt: '2026-11-16T01:00:00Z',
      readAt: null,
    },
  ]
  await accountFixture(context, page, async (route) => {
    const request = route.request(),
      path = new URL(request.url()).pathname
    calls.push({ method: request.method(), path })
    let data
    if (path === '/api/notifications')
      data = {
        items: rows.map((row, index) => ({
          ...row,
          readAt: read && !index ? row.createdAt : null,
        })),
        hasMore: true,
        nextCursor: rows[1].createdAt,
        nextCursorId: rows[1].id,
      }
    else if (path === '/api/notifications/unread-count') data = { count: read ? 2 : 3 }
    else if (path === '/api/notifications/private-first/read') {
      read = true
      await route.fulfill({ status: 204 })
      return true
    } else if (path === '/api/unread')
      data = {
        mainCount: 37,
        requestCount: 0,
        notificationCount: read ? 2 : 3,
        conversations: [{ conversationId: 'room', type: 'Private', count: 37 }],
      }
    else if (path === '/api/conversations') data = [room]
    else if (path === '/api/conversations/room') data = room
    else if (path === '/api/conversations/room/messages') data = { items: [], hasMore: false }
    else if (path === '/api/message-requests') data = []
    else if (path === '/api/users/peer') data = { id: 'peer', displayName: 'An' }
    else if (path.endsWith('/presence')) data = { online: null, lastSeenAt: null }
    if (data === undefined) return false
    await route.fulfill({ json: data })
    return true
  })
  return calls
}

for (const width of [390, 1366])
  for (const theme of ['light', 'dark'])
    for (const preview of [false, true]) {
      test(`server grouped ${preview ? 'preview' : 'center'} ${width}px ${theme}`, async ({
        context,
        page,
      }) => {
        await page.setViewportSize({ width, height: 850 })
        await page.addInitScript((value) => localStorage.setItem('mechat-theme', value), theme)
        const calls = await fixture(context, page)
        await page.goto(preview ? '/chat/room' : '/notifications')
        const notificationBadge = page.locator('a[href="/notifications"] .nav-badge').first()
        await expect(notificationBadge).toHaveText('3')
        if (preview) await page.locator('.notification-preview-trigger').click()
        const root = page.locator(preview ? '.notification-preview' : '#main-content')
        await expect(root.getByText('3 thông báo chưa đọc', { exact: true })).toBeVisible()
        await expect(
          root.locator(preview ? '.notification-preview-list > li' : '.notification-card'),
        ).toHaveCount(2)
        // A third unread item exists outside this page; neither UI derives totals from rendered rows.
        await root.getByRole('button', { name: 'Đánh dấu đã đọc', exact: true }).first().click()
        await expect(notificationBadge).toHaveText('2')
        await expect(root.getByText('2 thông báo chưa đọc', { exact: true })).toBeVisible()
        await expect(page.locator('a[href="/chat"] .nav-badge').first()).toHaveText('37')
        expect(calls.filter((call) => call.path.endsWith('/private-first/read'))).toHaveLength(1)
        expect(calls.some((call) => /\/(context|receipts)$/.test(call.path))).toBe(false)
        expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
          true,
        )
        expect(
          (await new AxeBuilder({ page }).withRules(['color-contrast']).analyze()).violations,
        ).toEqual([])
      })
    }
