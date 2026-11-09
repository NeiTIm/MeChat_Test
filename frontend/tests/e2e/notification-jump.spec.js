import { test, expect } from '@playwright/test'

// Isolated HTTP/SignalR simulation; never writes to the running MeChat backend.
async function fixture(page, context, { group = false, loaded = false, unavailable = false, recalled = false, denied = false, login = false, delayed = false, delayedTarget = false } = {}) {
  const base = group ? '/groups' : '/chat', api = group ? '/api/groups' : '/api/conversations'
  const room = { id: 'room', type: group ? 'Group' : 'Private', name: 'Nhóm thử', otherUserId: 'peer', status: 'Active', myRole: 'Member', lastSequence: 1000 }
  const target = { id: 'target', conversationId: 'room', senderUserId: 'peer', sequence: loaded ? 985 : 10, createdAt: '2026-11-09T00:00:00Z', content: recalled ? null : 'Tin đích tổng hợp', recalled }
  const row = n => ({ id: `m${n}`, conversationId: 'room', senderUserId: 'peer', sequence: n, createdAt: target.createdAt, content: `Tin tổng hợp ${n}` })
  const latest = Array.from({ length: 30 }, (_, i) => loaded && i + 971 === target.sequence ? target : row(i + 971))
  const older = [...Array.from({ length: 9 }, (_, i) => row(i + 1)), target]
  const calls = [], cursors = [], reads = [], sockets = []
  let authenticated = !login
  let targetFinished = false
  await context.addCookies([{ name: 'webchat_csrf', value: 'test-only', url: 'http://127.0.0.1:5184' }])
  await page.routeWebSocket(/\/hubs\/chat(?:\?|$)/, socket => {
    sockets.push(socket)
    socket.onMessage(raw => { for (const part of String(raw).split('\x1e').filter(Boolean)) { const m = JSON.parse(part)
      if (m.protocol) socket.send('{}\x1e')
      else if (m.type === 6) socket.send('{"type":6}\x1e')
      else if (m.invocationId) socket.send(JSON.stringify({ type: 3, invocationId: m.invocationId, result: null }) + '\x1e')
    } })
  })
  await page.route('**/hubs/chat/negotiate?*', route => route.fulfill({ json: { negotiateVersion: 1, connectionId: 'test', connectionToken: 'test', availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] } }))
  await page.route('**/api/**', async route => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    calls.push(path)
    let data
    if (path === '/api/auth/login') authenticated = true
    if (path === '/api/auth/refresh' || path === '/api/auth/login') return authenticated ? route.fulfill({ json: { accessToken: 'test-only', userId: 'actor', username: 'fixture', expiresAt: '2099-01-01T00:00:00Z' } }) : route.fulfill({ status: 401 })
    if (req.method() !== 'GET') { if (path.endsWith('/read-cursor')) reads.push(req.postDataJSON().lastReadSequence); return route.fulfill({ status: 204 }) }
    if (path === '/api/users/me') data = { id: 'actor', displayName: 'Tôi', systemRole: 'User' }
    else if (path === '/api/users/peer') data = { id: 'peer', displayName: 'Bạn thử' }
    else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, notificationCount: 1, conversations: [] }
    else if (path === '/api/message-requests') data = []
    else if (path === '/api/notifications') data = { items: [{ id: 'notice', type: group ? 'GroupMessage' : 'Message', referenceId: 'target', createdAt: target.createdAt, readAt: null }], hasMore: false }
    else if (path.endsWith('/unread-count') || path.endsWith('/unread')) data = { count: 0 }
    else if (path === '/api/messages/target') {
      if (delayedTarget) await new Promise(resolve => setTimeout(resolve, 1200))
      targetFinished = true
      return unavailable ? route.fulfill({ status: 404 }) : route.fulfill({ json: target })
    }
    else if (path === api) data = [room, { ...room, id: 'other', name: 'Nhóm khác' }]
    else if (path === `${api}/room`) return denied ? route.fulfill({ status: 403 }) : route.fulfill({ json: room })
    else if (path === `${api}/other`) data = { ...room, id: 'other' }
    else if (path === `${api}/room/messages`) {
      const before = url.searchParams.get('beforeSequence'); cursors.push(before)
      if (denied) return route.fulfill({ status: 403 })
      if (delayed && before) await new Promise(resolve => setTimeout(resolve, 1200))
      data = { items: before ? unavailable ? [] : older.slice().reverse() : latest.slice().reverse(), hasMore: false }
    } else if (path === `${api}/other/messages`) data = { items: [], hasMore: false }
    else if (path.endsWith('/members')) data = [{ userId: 'peer', displayName: 'Bạn thử', role: 'Member' }]
    else if (path.endsWith('/context')) data = { reply: null, reactions: [] }
    else if (path.endsWith('/presence')) data = { online: null, lastSeenAt: null }
    return data === undefined ? route.fulfill({ status: 404 }) : route.fulfill({ json: data })
  })
  const url = `${base}/room?at=${target.sequence}&message=target`
  return { base, url, calls, cursors, reads, sockets, targetFinished: () => targetFinished, incoming: () => sockets[0].send(JSON.stringify({ type: 1, target: 'MessageCreated', arguments: [{ eventId: crypto.randomUUID(), conversationId: 'room', messageId: 'new', sequence: 1001 }] }) + '\x1e') }
}

for (const group of [false, true]) test(`notification jumps directly to older ${group ? 'group' : 'private'} message and survives refresh/realtime`, async ({ page, context }) => {
  if (group) {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.addInitScript(() => localStorage.setItem('mechat-theme', 'dark'))
    await page.emulateMedia({ reducedMotion: 'reduce' })
  }
  const f = await fixture(page, context, { group })
  await page.goto('/notifications'); await page.getByRole('button', { name: 'Xem nội dung', exact: true }).click()
  await expect(page).toHaveURL(new RegExp(`${f.base}/room\\?at=10&message=target`))
  const target = page.locator('[data-message-id="target"]')
  await expect(target).toBeFocused(); await expect(target).toHaveClass(/message-highlighted/)
  await expect(target).toBeInViewport()
  expect(f.cursors.every(cursor => cursor === '11')).toBe(true)
  expect(f.calls.filter(path => path === '/api/messages/target')).toHaveLength(1)
  const scroll = page.locator('.message-scroll'), before = await scroll.evaluate(el => el.scrollTop)
  const count = f.cursors.length; f.incoming()
  await expect.poll(() => f.cursors.length).toBeGreaterThan(count)
  expect(await scroll.evaluate(el => el.scrollTop)).toBe(before)
  await expect(target).not.toHaveClass(/message-highlighted/, { timeout: 4000 })
  await expect.poll(() => f.reads.length).toBeGreaterThan(0)
  expect(f.reads.every(sequence => sequence <= 10)).toBe(true)
  expect(f.sockets).toHaveLength(1)
  await page.reload(); await expect(target).toBeFocused(); await expect(target).toHaveClass(/message-highlighted/)
})

test('uses already loaded history and scrolls only the message container', async ({ page, context }) => {
  const f = await fixture(page, context, { loaded: true })
  await page.goto('/chat/room'); await expect(page.locator('.message-entry')).toHaveCount(30)
  await page.locator('.notification-preview-trigger').click()
  await page.getByRole('button', { name: 'Xem nội dung', exact: true }).click()
  const target = page.locator('[data-message-id="target"]')
  await expect(target).toBeFocused(); await expect(target).toBeInViewport()
  expect(f.cursors.every(cursor => cursor === null)).toBe(true)
  expect(await page.evaluate(() => window.scrollY)).toBe(0)
  await expect(target).not.toHaveClass(/message-highlighted/, { timeout: 4000 })
  await page.locator('.message-scroll').evaluate(el => { el.scrollTop = 0; el.dispatchEvent(new Event('scroll')) })
  await page.locator('.notification-preview-trigger').click()
  await page.getByRole('button', { name: 'Xem nội dung', exact: true }).click()
  await expect(target).toHaveClass(/message-highlighted/)
  await expect(target).toBeInViewport()
})

test('unavailable/deleted notification target does not pretend to navigate successfully', async ({ page, context }) => {
  const f = await fixture(page, context, { unavailable: true })
  await page.goto('/notifications'); await page.getByRole('button', { name: 'Xem nội dung', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('Nội dung không khả dụng')
  await expect(page).toHaveURL(/\/notifications$/); expect(f.cursors).toHaveLength(0)
  await page.goto(f.url); await expect(page.getByText('Tin nhắn không còn trong phạm vi lịch sử bạn được xem.')).toBeVisible()
})

test('recalled target shows only the recalled placeholder', async ({ page, context }) => {
  const f = await fixture(page, context, { recalled: true })
  await page.goto(f.url)
  await expect(page.locator('[data-message-id="target"]')).toHaveClass(/message-highlighted/)
  await expect(page.locator('[data-message-id="target"]')).toContainText('đã được thu hồi')
  await expect(page.getByText('Tin đích tổng hợp', { exact: true })).toHaveCount(0)
})

test('changed group permissions show an error and never mark messages seen', async ({ page, context }) => {
  const f = await fixture(page, context, { group: true, denied: true })
  await page.goto(f.url); await expect(page.getByText('Bạn không có quyền thực hiện thao tác này.')).toBeVisible()
  expect(f.reads).toHaveLength(0); await expect(page.locator('.message-entry')).toHaveCount(0)
})

test('login redirect retains a reloadable exact-message target', async ({ page, context }) => {
  const f = await fixture(page, context, { login: true })
  await page.goto(f.url); await expect(page).toHaveURL(/\/login\?returnTo=/)
  await page.reload()
  await page.getByLabel('Email hoặc tên đăng nhập', { exact: true }).fill('fixture')
  await page.getByLabel('Mật khẩu', { exact: true }).fill('test-only')
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
  await expect(page.locator('[data-message-id="target"]')).toBeFocused()
  await expect(page).toHaveURL(/\/chat\/room\?at=10&message=target/)
})

test('switching conversation cancels a pending history jump', async ({ page, context }) => {
  const f = await fixture(page, context, { delayed: true })
  await page.goto(f.url)
  await expect(page.locator('.conversation-row')).toHaveCount(2)
  await page.locator('.conversation-row').last().click()
  await expect(page).toHaveURL(/\/chat\/other$/)
  await expect(page.getByText('Hãy là người đầu tiên gửi lời chào.')).toBeVisible()
  await expect(page.locator('[data-message-id="target"]')).toHaveCount(0)
})

test('login returnTo cannot navigate to an external origin', async ({ page, context }) => {
  await fixture(page, context, { login: true })
  await page.goto('/login?returnTo=//example.invalid/target')
  await page.getByLabel('Email hoặc tên đăng nhập', { exact: true }).fill('fixture')
  await page.getByLabel('Mật khẩu', { exact: true }).fill('test-only')
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
  await expect(page).toHaveURL('http://127.0.0.1:5184/chat')
})

test('realtime while target history is pending neither restarts it nor scrolls to the latest message', async ({ page, context }) => {
  const f = await fixture(page, context, { delayed: true })
  await page.goto(f.url)
  await expect.poll(() => f.cursors.length).toBeGreaterThan(0)
  await expect.poll(() => f.sockets.length).toBe(1)
  f.incoming()
  await expect(page.locator('[data-message-id="target"]')).toBeFocused()
  expect(f.cursors).toEqual(['11'])
  expect(f.reads.every(sequence => sequence <= 10)).toBe(true)
})

test('changing conversation cancels an in-flight notification target lookup', async ({ page, context }) => {
  const f = await fixture(page, context, { delayedTarget: true })
  await page.goto('/chat/room'); await expect(page.locator('.message-entry')).toHaveCount(30)
  await page.locator('.notification-preview-trigger').click()
  await page.getByRole('button', { name: 'Xem nội dung', exact: true }).click()
  await expect.poll(() => f.calls.includes('/api/messages/target')).toBe(true)
  await page.keyboard.press('Escape')
  await page.locator('.conversation-row').last().click()
  await expect.poll(f.targetFinished).toBe(true)
  await expect(page).toHaveURL(/\/chat\/other$/)
  await expect(page.locator('[data-message-id="target"]')).toHaveCount(0)
})
