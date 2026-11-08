import { test, expect } from '@playwright/test'

// Test-only HTTP/WebSocket simulation; the isolated config proxies to port 9.
for (const [group, width, dark] of [[false, 320, false], [false, 1366, true], [true, 390, true], [true, 1440, false]]) {
  test(`typing above composer: ${group ? 'group' : 'private'} ${width}px ${dark ? 'dark' : 'light'}`, async ({ page, context }) => {
    await page.setViewportSize({ width, height: 900 })
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.addInitScript(value => localStorage.setItem('mechat-theme', value), dark ? 'dark' : 'light')
    await context.addCookies([{ name: 'webchat_csrf', value: 'test-only', url: 'http://127.0.0.1:5184' }])
    const room = { id: 'typing-room', name: 'Nhóm kiểm thử', otherUserId: group ? undefined : 'peer', status: 'Active', myRole: 'Member', lastSequence: 40 }
    const messages = Array.from({ length: 40 }, (_, i) => ({ id: `m${i}`, conversationId: room.id, senderUserId: 'peer', content: `Tin tổng hợp ${i}`, sequence: i + 1, createdAt: '2026-11-09T00:00:00Z' }))
    const members = ['peer', 'second', 'third'].map((userId, i) => ({ userId, displayName: ['tienAdmin', 'Bình', 'Chi'][i], role: 'Member' }))
    const sockets = [], errors = []
    page.on('pageerror', error => errors.push(error.message))
    await page.routeWebSocket(/\/hubs\/chat(?:\?|$)/, socket => {
      sockets.push(socket)
      socket.onMessage(raw => {
        for (const part of String(raw).split('\x1e').filter(Boolean)) {
          const data = JSON.parse(part)
          if (data.protocol) socket.send('{}\x1e')
          else if (data.type === 6) socket.send('{"type":6}\x1e')
          else if (data.invocationId) socket.send(JSON.stringify({ type: 3, invocationId: data.invocationId, result: null }) + '\x1e')
        }
      })
    })
    await page.route('**/hubs/chat/negotiate?*', route => route.fulfill({ json: { negotiateVersion: 1, connectionId: 'test-only', connectionToken: 'test-only', availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] } }))
    await page.route('**/api/**', route => {
      const req = route.request(), path = new URL(req.url()).pathname
      let data
      if (path === '/api/auth/refresh') data = { accessToken: 'test-only', expiresAt: '2099-01-01T00:00:00Z', userId: 'actor', username: 'fixture' }
      else if (req.method() !== 'GET') return route.fulfill({ status: 204 })
      else if (path === '/api/users/me') data = { id: 'actor', displayName: 'Tôi', systemRole: 'User' }
      else if (path === '/api/users/peer') data = { id: 'peer', displayName: 'tienAdmin' }
      else if (path === '/api/conversations' || path === '/api/groups') data = [room]
      else if (path === `/api/conversations/${room.id}` || path === `/api/groups/${room.id}`) data = room
      else if (path.endsWith('/messages')) data = { items: messages.slice().reverse(), hasMore: false }
      else if (path.endsWith('/members')) data = members
      else if (path.endsWith('/presence')) data = { online: width === 320 ? null : false, lastSeenAt: null }
      else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, notificationCount: 0, conversations: [] }
      else if (path === '/api/message-requests') data = []
      else if (path.endsWith('/context')) data = { reply: null, reactions: [] }
      else if (path.endsWith('/unread') || path.endsWith('/unread-count')) data = { count: 0 }
      return data === undefined ? route.fulfill({ status: 404 }) : route.fulfill({ json: data })
    })
    await page.goto(`/${group ? 'groups' : 'chat'}/${room.id}`)
    await expect(page.locator('.message-entry')).toHaveCount(40)
    await expect.poll(() => sockets.length).toBe(1)
    await expect(page.locator('.thread-heading h1')).toHaveText(group ? 'Nhóm kiểm thử' : 'tienAdmin')
    await expect(page.locator('.thread-heading p')).toContainText(group ? '3' : width === 320 ? 'Trò chuyện riêng' : 'Đang ngoại tuyến')
    const indicator = page.locator('.typing-indicator'), scroll = page.locator('.message-scroll'), composer = page.locator('.composer')
    const header = await page.locator('.thread-heading').textContent()
    const before = await composer.boundingBox()
    await scroll.evaluate(el => { el.scrollTop = 80; el.dispatchEvent(new Event('scroll', { bubbles: true })) })
    const scrollBefore = await scroll.evaluate(el => el.scrollTop)
    const emit = (actorId, isTyping = true, conversationId = room.id) => sockets[0].send(JSON.stringify({ type: 1, target: 'TypingChanged', arguments: [{ actorId, conversationId, isTyping, expiresAt: new Date(Date.now() + 5000).toISOString() }] }) + '\x1e')
    emit('actor'); emit('peer', true, 'other-room')
    await expect(indicator).toBeEmpty()
    emit('peer')
    await expect(indicator).toContainText('tienAdmin đang nhập')
    if (group) { emit('second'); emit('third'); await expect(indicator).toContainText('tienAdmin, Bình và 1 người khác đang nhập') }
    await expect(page.locator('.thread-heading')).toHaveText(header)
    const box = await indicator.boundingBox(), after = await composer.boundingBox(), history = await scroll.boundingBox()
    expect(Math.abs(after.y - before.y)).toBeLessThan(1)
    expect(box.y).toBeGreaterThanOrEqual(history.y + history.height - 1)
    expect(box.y + box.height).toBeLessThanOrEqual(after.y + 1)
    expect(await scroll.evaluate(el => el.scrollTop)).toBe(scrollBefore)
    expect(await page.locator('.typing-dots i').first().evaluate(el => getComputedStyle(el).animationName)).toBe('none')
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    emit('peer', false); if (group) { emit('second', false); emit('third', false) }
    await expect(indicator).toBeEmpty()
    // Existing provider TTL must still remove stale typing without a StopTyping event.
    if (width === 320) { emit('peer'); await expect(indicator).toContainText('tienAdmin'); await expect(indicator).toBeEmpty({ timeout: 7500 }) }
    expect(sockets).toHaveLength(1)
    expect(errors).toEqual([])
  })
}
