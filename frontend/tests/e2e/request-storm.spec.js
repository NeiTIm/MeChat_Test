// Entire HTTP and SignalR transport are test-only simulations; no live MeChat data/SQL.
import { test, expect } from '@playwright/test'
import { mkdir, writeFile } from 'node:fs/promises'

const phase = process.env.REQUEST_STORM_PHASE || 'after'
const output = []
test.afterAll(async () => {
  await mkdir('../docs/frontend', { recursive: true })
  await writeFile(`../docs/frontend/request-storm-${phase}.json`, JSON.stringify({ capturedAt: new Date().toISOString(), phase,
    scope: 'Synthetic 30 messages, actual browser consumers + mocked HTTP/SignalR, 1366x900, 60 seconds wall-clock idle. No SQL/live business acceptance.', tests: output }, null, 2) + '\n')
})

async function fixture(page, context, { throttled = false } = {}) {
  const calls = [], sockets = []
  const room = { id: 'storm-room', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 30, updatedAt: '2026-11-09T00:00:00Z' }
  let seen = false, reaction = false
  const messages = Array.from({ length: 30 }, (_, i) => ({ id: `m${i + 1}`, conversationId: room.id,
    senderUserId: (i + 1) % 2 === 0 ? 'actor' : 'friend', sequence: i + 1, content: `Tin tổng hợp ${i + 1}`,
    createdAt: '2026-11-09T00:00:00Z', replyToMessageId: (i + 1) % 6 === 0 ? 'm1' : null }))
  await context.addCookies([{ name: 'webchat_csrf', value: 'test-only', url: 'http://127.0.0.1:5185' }])
  const emit = (name, payload) => sockets.at(-1)?.send(JSON.stringify({ type: 1, target: name, arguments: [{ eventId: crypto.randomUUID(), conversationId: room.id, ...payload }] }) + '\x1e')
  await page.routeWebSocket(/\/hubs\/chat(?:\?|$)/, socket => {
    sockets.push(socket)
    socket.onMessage(raw => {
      for (const part of String(raw).split('\x1e').filter(Boolean)) {
        const message = JSON.parse(part)
        if (message.protocol) socket.send('{}\x1e')
        else if (message.type === 6) socket.send('{"type":6}\x1e')
        else if (message.invocationId) socket.send(JSON.stringify({ type: 3, invocationId: message.invocationId, result: null }) + '\x1e')
      }
    })
  })
  await page.route('**/hubs/chat/negotiate?*', route => route.fulfill({ json: { negotiateVersion: 1, connectionId: crypto.randomUUID(), connectionToken: crypto.randomUUID(), availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] } }))
  await page.route('**/api/**', async route => {
    const req = route.request(), path = new URL(req.url()).pathname, method = req.method()
    calls.push({ method, path })
    let data
    if (path === '/api/auth/refresh') data = { accessToken: 'test-only', expiresAt: '2099-01-01T00:00:00Z', userId: 'actor', username: 'test' }
    else if (method === 'GET') {
      if (path === '/api/users/me') data = { id: 'actor', username: 'test', displayName: 'Người kiểm thử', systemRole: 'User' }
      else if (path === '/api/conversations') data = [room]
      else if (path === '/api/conversations/storm-room') data = room
      else if (path === '/api/conversations/storm-room/messages') data = { items: [...messages].reverse(), hasMore: false }
      else if (path === '/api/users/friend') data = { id: 'friend', username: 'fixture', displayName: 'Bạn kiểm thử' }
      else if (path.endsWith('/presence')) data = { online: null, lastSeenAt: null }
      else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, notificationCount: 0, conversations: [] }
      else if (path.endsWith('/unread') || path.endsWith('/unread-count')) data = { count: 0 }
      else if (path === '/api/message-requests') data = []
      else if (path.endsWith('/context')) {
        const m = messages.find(m => path === `/api/messages/${m.id}/context`)
        data = { reply: m?.replyToMessageId ? { available: true, content: 'Trích dẫn tổng hợp' } : null, reactions: reaction && m?.id === 'm30' ? [{ emoji: '👍', count: 1 }] : [], myReaction: null }
      } else if (path.endsWith('/receipts')) {
        if (throttled) return route.fulfill({ status: 429, headers: { 'Retry-After': '60' } })
        data = { status: seen ? 'Seen' : 'Delivered', deliveredCount: 1, seenCount: seen ? 1 : 0 }
      } else if (path.startsWith('/api/messages/')) data = messages.find(m => path === `/api/messages/${m.id}`)
      else if (path.endsWith('/settings')) data = { mutedUntil: null }
    } else if (path === '/api/conversations/storm-room/messages' && method === 'POST') {
      const m = { ...req.postDataJSON(), id: 'sent', conversationId: room.id, senderUserId: 'actor', sequence: 31, createdAt: '2026-11-09T00:01:00Z' }
      messages.push(m); room.lastSequence = 31; data = m
    } else {
      if (path.endsWith('/delivery-acks')) for (const messageId of req.postDataJSON().messageIds) emit('MessageDelivered', { messageId })
      if (path.endsWith('/read-cursor')) emit('MessageSeen', { userId: 'actor', lastReadSequence: req.postDataJSON().lastReadSequence })
      return route.fulfill({ status: 204 })
    }
    return data === undefined ? route.fulfill({ status: 404 }) : route.fulfill({ json: data })
  })
  const snapshot = () => ({ total: calls.length, context: calls.filter(c => c.path.endsWith('/context')).length,
    receipts: calls.filter(c => c.path.endsWith('/receipts')).length, history: calls.filter(c => c.method === 'GET' && c.path === '/api/conversations/storm-room/messages').length })
  await page.goto('/chat/storm-room')
  await expect(page.locator('.message-entry')).toHaveCount(30)
  await expect.poll(() => sockets.length).toBeGreaterThan(0)
  await page.waitForTimeout(2000)
  return { calls, emit, snapshot, sockets, setSeen: () => { seen = true }, setReaction: () => { reaction = true }, receive: () => { messages.push({ id: 'received', conversationId: room.id, senderUserId: 'friend', sequence: 32, content: 'Tin đến tổng hợp', createdAt: '2026-11-09T00:01:00Z' }); room.lastSequence = 32; emit('MessageCreated', { messageId: 'received', sequence: 32 }) } }
}

test('open chat and idle for one minute', async ({ page, context }) => {
  const f = await fixture(page, context), opened = f.snapshot()
  await page.waitForTimeout(60000)
  const idle = f.snapshot(); output.push({ name: 'idle60s', opened, idle })
  if (phase === 'after') { expect(opened.context + opened.receipts).toBeLessThanOrEqual(20); expect(idle.context + idle.receipts - opened.context - opened.receipts).toBeLessThanOrEqual(20) }
})

test('send, receive, receipt and reaction hints are scoped; reconnect repairs state', async ({ page, context }) => {
  const f = await fixture(page, context), opened = f.snapshot()
  await page.getByRole('textbox', { name: 'Tin nhắn', exact: true }).fill('Tin gửi tổng hợp')
  await page.getByRole('button', { name: 'Gửi tin nhắn', exact: true }).click()
  await expect(page.locator('.message-entry')).toHaveCount(31)
  f.receive(); await expect(page.locator('.message-entry')).toHaveCount(32)
  await page.waitForTimeout(1000); const sentReceived = f.snapshot()
  f.setSeen(); for (let i = 0; i < 6; i++) f.emit('MessageSeen', { userId: 'friend', lastReadSequence: 32 })
  f.setReaction(); f.emit('MessageReactionChanged', { messageId: 'm30' })
  await page.waitForTimeout(1200)
  await expect(page.locator('[data-message-id="sent"] .message-time')).toContainText('Đã xem')
  await expect(page.locator('[data-message-id="m30"] .message-reactions')).toContainText('👍')
  const hints = f.snapshot()
  const socketCount = f.sockets.length; f.sockets.at(-1).close({ code: 1012, reason: 'test-only reconnect' })
  await expect.poll(() => f.sockets.length).toBeGreaterThan(socketCount)
  await page.waitForTimeout(1000); const reconnected = f.snapshot()
  output.push({ name: 'sendReceiveHintsReconnect', opened, sentReceived, hints, reconnected })
  if (phase === 'after') {
    expect(hints.history).toBe(sentReceived.history)
    expect(hints.context - sentReceived.context).toBeLessThanOrEqual(1)
    expect(hints.receipts - sentReceived.receipts).toBeLessThanOrEqual(10)
    expect(reconnected.history).toBeGreaterThan(hints.history)
  }
})

test('429 metadata GET does not retry in a loop or blame the user', async ({ page, context }) => {
  const f = await fixture(page, context, { throttled: true }), opened = f.snapshot()
  for (let i = 0; i < 3; i++) { f.emit('MessageDelivered', { messageId: 'm30' }); await page.waitForTimeout(200) }
  await page.waitForTimeout(1000); const hints = f.snapshot()
  output.push({ name: '429', opened, hints })
  if (phase === 'after') {
    expect(hints.receipts).toBe(opened.receipts)
    await expect(page.getByText('Bạn thao tác quá nhanh', { exact: false })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Đồng bộ tạm hoãn. Vui lòng thử lại sau.', exact: true }).first()).toBeVisible()
  }
})
