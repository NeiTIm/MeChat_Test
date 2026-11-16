import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { accountFixture } from './account-client-fixture'

// HTTP simulation only: no user credentials, live API or SQL mutations.
async function fixture(context, page, { missing = false, denied = 0, deferred = false } = {}) {
  const calls = []
  let read = false
  let release
  const gate = new Promise((resolve) => {
    release = resolve
  })
  const rows = Array.from({ length: 25 }, (_, index) => ({
    id: `request-${index}`,
    senderUserId: `sender-${index}`,
    receiverUserId: 'actor',
    status: 'Pending',
    seenAt: null,
    createdAt: '2026-11-16T07:00:00Z',
  }))
  const notification = {
    id: 'friend-notice',
    type: 'FriendRequest',
    referenceId: 'request-24',
    createdAt: '2026-11-16T07:00:00Z',
    readAt: null,
    text: 'An đã gửi cho bạn một lời mời kết bạn.',
  }
  await accountFixture(context, page, async (route) => {
    const req = route.request(),
      path = new URL(req.url()).pathname
    calls.push({ method: req.method(), path, body: req.postDataJSON() })
    if (path === '/api/notifications') {
      await route.fulfill({
        json: {
          items: [{ ...notification, readAt: read ? notification.createdAt : null }],
          hasMore: false,
        },
      })
      return true
    }
    if (path === '/api/notifications/unread-count') {
      await route.fulfill({ json: { count: read ? 0 : 1 } })
      return true
    }
    if (path === '/api/notifications/friend-notice/read') {
      read = true
      await route.fulfill({ status: 204 })
      return true
    }
    if (path === '/api/friends/requests') {
      if (deferred) await gate
      await route.fulfill(
        denied
          ? { status: denied, json: {} }
          : { json: { incoming: missing ? rows.slice(0, 24) : rows, outgoing: [] } },
      )
      return true
    }
    if (path === '/api/friends/requests/unread-count') {
      await route.fulfill({ json: { unreadCount: rows.filter((row) => !row.seenAt).length } })
      return true
    }
    if (path === '/api/friends/requests/mark-seen') {
      for (const row of rows)
        if (req.postDataJSON().requestIds.includes(row.id)) row.seenAt = notification.createdAt
      await route.fulfill({ status: 204 })
      return true
    }
    if (path === '/api/friends' || path === '/api/blocks') {
      await route.fulfill({ json: [] })
      return true
    }
    if (path.startsWith('/api/users/sender-')) {
      await route.fulfill({
        json: { id: path.split('/').at(-1), username: 'an', displayName: 'An' },
      })
      return true
    }
    if (path === '/api/unread') {
      await route.fulfill({ json: { requestCount: 0, conversations: [] } })
      return true
    }
    return false
  })
  return { calls, rows, release, isRead: () => read }
}

for (const width of [390, 1366])
  for (const theme of ['light', 'dark']) {
    test(`friend notification exact target ${width}px ${theme}`, async ({ context, page }) => {
      await page.setViewportSize({ width, height: 850 })
      await page.addInitScript((value) => localStorage.setItem('mechat-theme', value), theme)
      const f = await fixture(context, page, { deferred: true })
      await page.goto('/notifications')
      await expect(page.getByRole('heading', { name: 'Bạn có lời mời kết bạn mới' })).toBeVisible()
      await expect(page.getByText('An đã gửi cho bạn một lời mời kết bạn.')).toBeVisible()
      expect(f.calls.filter((call) => call.path.endsWith('mark-seen'))).toHaveLength(0)
      await page.getByRole('button', { name: 'Xem lời mời', exact: true }).click()
      await expect(page).toHaveURL(/\/friends\/incoming\?request=request-24$/)
      expect(f.isRead()).toBe(true)
      expect(f.rows.every((row) => row.seenAt === null)).toBe(true)
      f.release()
      const target = page.locator('[data-friend-request-id="request-24"]')
      await expect(target).toBeFocused()
      await expect(target).toBeInViewport({ ratio: 0.25 })
      await expect(target).toHaveClass(/ring-primary/)
      await expect
        .poll(() =>
          f.calls.some(
            (call) =>
              call.path.endsWith('mark-seen') && call.body.requestIds.includes('request-24'),
          ),
        )
        .toBe(true)
      const marked = f.calls
        .filter((call) => call.path.endsWith('mark-seen'))
        .flatMap((call) => call.body.requestIds)
      expect(marked).not.toContain('request-0')
      expect(f.calls.some((call) => /\/(accept|reject|cancel)$/.test(call.path))).toBe(false)
      const focusScroll = await page.evaluate(() => scrollY)
      await expect(page.locator('[data-friend-request-id="request-24"]')).not.toHaveClass(
        /ring-primary/,
      )
      expect(Math.abs((await page.evaluate(() => scrollY)) - focusScroll)).toBeLessThan(3)
      // Each confirmed viewport batch legitimately revalidates the request list.
      const confirmations = f.calls.filter((call) => call.path.endsWith('mark-seen')).length
      await expect
        .poll(() => f.calls.filter((call) => call.path === '/api/friends/requests').length)
        .toBe(1 + confirmations)
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
        true,
      )
      expect(
        (await new AxeBuilder({ page }).withRules(['color-contrast']).analyze()).violations,
      ).toEqual([])
      await page.reload()
      await expect(target).toBeFocused()
    })
  }

test('missing or completed request reports unavailable and never focuses another row', async ({
  context,
  page,
}) => {
  const f = await fixture(context, page, { missing: true })
  await page.goto('/notifications')
  await page.getByRole('button', { name: 'Xem lời mời', exact: true }).click()
  await expect(page.getByText(/Lời mời này không còn khả dụng/)).toBeVisible()
  await expect(page.locator('[data-friend-request-id].ring-primary')).toHaveCount(0)
  expect(f.calls.some((call) => /\/(accept|reject|cancel)$/.test(call.path))).toBe(false)
})

for (const status of [403, 404])
  test(`incoming request ${status} is handled safely`, async ({ context, page }) => {
    await fixture(context, page, { denied: status })
    await page.goto('/friends/incoming?request=request-24')
    await expect(page.locator('.settings-content [role="alert"]').first()).toBeVisible()
    await expect(page.locator('[data-friend-request-id]')).toHaveCount(0)
  })

test('cached incoming data supports a loaded target without an extra target endpoint', async ({
  context,
  page,
}) => {
  const f = await fixture(context, page)
  await page.goto('/friends/incoming')
  await expect(page.locator('[data-friend-request-id="request-24"]')).toBeAttached()
  await page.locator('a[href="/notifications"]').first().click()
  await page.getByRole('button', { name: 'Xem lời mời', exact: true }).click()
  await expect(page.locator('[data-friend-request-id="request-24"]')).toBeFocused()
  expect(f.calls.some((call) => /^\/api\/friends\/requests\/request-24$/.test(call.path))).toBe(
    false,
  )
})

test('revalidates a fresh cache before focusing a request completed on another device', async ({
  context,
  page,
}) => {
  await page.addInitScript(() => {
    window.friendRequestFocus = []
    document.addEventListener('focusin', (event) => {
      if (event.target.dataset.friendRequestId)
        window.friendRequestFocus.push(event.target.dataset.friendRequestId)
    })
  })
  const f = await fixture(context, page)
  await page.goto('/friends/incoming')
  await expect(page.locator('[data-friend-request-id="request-24"]')).toBeAttached()
  await page.locator('a[href="/notifications"]').first().click()
  f.rows.pop()
  await page.getByRole('button', { name: 'Xem lời mời', exact: true }).click()
  await expect(page.getByText(/Lời mời này không còn khả dụng/)).toBeVisible()
  await expect(page.locator('[data-friend-request-id="request-24"]')).toHaveCount(0)
  expect(await page.evaluate(() => window.friendRequestFocus)).not.toContain('request-24')
})
