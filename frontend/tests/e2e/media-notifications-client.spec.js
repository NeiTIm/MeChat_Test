import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'
import { settleMotion } from './review-helpers'

async function fixture(context, page, largePreview = false, avatar = false) {
  const calls = []
  const conversation = { id: 'conversation', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 0, updatedAt: '2026-11-06T07:30:00Z' }
  const fileBody = largePreview ? 'Nội dung <script>alert("test-only")</script>.\n'.repeat(4000) : 'Nội dung mẫu kiểm thử'
  const file = { id: 'uploaded', fileName: 'nhat-ky.txt', contentType: 'text/plain', sizeBytes: Buffer.byteLength(fileBody), status: 'Pending' }
  let messages = largePreview ? [{ id: 'large-file-message', conversationId: conversation.id, senderUserId: 'actor', sequence: 1, createdAt: new Date().toISOString(), attachments: [file] }] : [], mutedUntil = null
  const notifications = [{ id: 'notice', type: 'Message', referenceId: 'message', createdAt: '2026-11-06T07:30:00Z', readAt: null }, { id: 'warning', type: 'AdminWarning', createdAt: '2026-11-06T07:30:00Z', readAt: null, text: 'Vui lòng tuân thủ quy tắc cộng đồng.' }]
  await accountFixture(context, page, async (route, me) => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    if (path.startsWith('/api/auth/')) return false
    if (req.method() === 'GET') {
      if (path === '/api/files/avatar/preview') { await route.fulfill({ contentType: 'image/png', body: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j5xQAAAAASUVORK5CYII=', 'base64') }); return true }
      if (path === '/api/files/uploaded/preview' || path === '/api/files/uploaded/download') { await route.fulfill({ contentType: 'text/plain', body: fileBody }); return true }
      let data
      if (path === '/api/conversations') data = [conversation]
      else if (path === '/api/conversations/conversation') data = conversation
      else if (path === '/api/conversations/conversation/messages') data = { items: messages, hasMore: false }
      else if (path === '/api/users/friend') data = { id: 'friend', displayName: 'Gia Huy', username: 'giahuy', avatarFileId: avatar ? 'avatar' : null }
      else if (path.endsWith('/presence')) data = { userId: 'friend', online: null, lastSeenAt: null }
      else if (path === '/api/message-requests') data = []
      else if (path === '/api/message-requests/unread-count') data = { count: 0 }
      else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, conversations: [], notificationCount: 2 }
      else if (path === '/api/notifications') data = { items: notifications, hasMore: false }
      else if (path === '/api/notifications/unread-count') data = { count: notifications.filter((item) => !item.readAt).length }
      else if (path.endsWith('/settings')) data = { mutedUntil }
      else if (path === '/api/messages/message') data = { id: 'message', conversationId: conversation.id, content: 'Nội dung tin nhắn' }
      else if (path.endsWith('/context')) data = { reply: null, reactions: [], myReaction: null }
      else if (path.endsWith('/receipts')) data = { status: 'Sent', deliveredCount: 0, seenCount: 0 }
      if (data !== undefined) { await route.fulfill({ json: data }); return true }
    } else {
      if (path === '/api/files') { calls.push({ path, body: req.postData() }); await route.fulfill({ json: file }); return true }
      calls.push({ path, method: req.method(), data: req.postDataJSON() })
      if (path === '/api/conversations/conversation/messages') { const message = { ...req.postDataJSON(), id: 'sent', conversationId: conversation.id, senderUserId: me.id, sequence: 1, createdAt: new Date().toISOString(), attachments: [file] }; messages = [message]; await route.fulfill({ json: message }); return true }
      if (path.endsWith('/settings')) mutedUntil = req.postDataJSON().mutedUntil
      if (path === '/api/messages/sent/recall') messages = messages.map((message) => ({ ...message, recalled: true, content: null, attachments: [] }))
      if (path === '/api/notifications/read-all') notifications.forEach((item) => item.readAt = new Date().toISOString())
      else if (path.startsWith('/api/notifications/') && path.endsWith('/read')) notifications.find((item) => item.id === path.split('/')[3]).readAt = new Date().toISOString()
      await route.fulfill({ status: 204 }); return true
    }
    return false
  })
  return calls
}
test('attachment upload/send/authorized preview/download uses actual consumers (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/chat/conversation')
  await page.getByLabel('Đính kèm tệp', { exact: true }).setInputFiles({ name: 'nhat-ky.txt', mimeType: 'text/plain', buffer: Buffer.from('Nội dung mẫu kiểm thử') })
  await expect(page.getByText('Sẵn sàng gửi')).toBeVisible()
  expect(calls.find((call) => call.path === '/api/files').body).toContain('name="purpose"\r\n\r\nMessage')
  await page.getByRole('button', { name: 'Gửi tin nhắn', exact: true }).click()
  await page.locator('.attachment-card').click()
  await expect(page.getByRole('dialog')).toContainText('Nội dung mẫu kiểm thử')
  expect(calls.find((call) => call.path.endsWith('/messages')).data).toEqual({ content: null, clientMessageId: expect.any(String), attachmentIds: ['uploaded'] })
  const downloaded = page.waitForEvent('download')
  await page.getByRole('dialog').getByRole('button', { name: 'Tải về', exact: true }).click()
  expect((await downloaded).suggestedFilename()).toBe('nhat-ky.txt')
})
test('avatar upload uses Avatar purpose before binding and retains no local credentials (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/account')
  await page.getByLabel('Chọn ảnh mới', { exact: true }).setInputFiles({ name: 'avatar.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j5xQAAAAASUVORK5CYII=', 'base64') })
  await page.getByRole('button', { name: 'Lưu ảnh đại diện', exact: true }).click()
  await expect(page.locator('.avatar-editor').getByText('Đã cập nhật ảnh đại diện.')).toBeVisible()
  expect(calls.find((call) => call.path === '/api/files').body).toContain('name="purpose"\r\n\r\nAvatar')
  expect(calls.find((call) => call.path === '/api/users/me/avatar').data).toEqual({ storedFileId: 'uploaded' })
})
test('notifications mark-read and conversation mute preserve backend contracts (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/notifications')
  await expect(page.getByText('2 thông báo chưa đọc')).toBeVisible()
  await page.getByRole('button', { name: 'Đánh dấu tất cả đã đọc', exact: true }).click()
  await expect(page.getByText('0 thông báo chưa đọc')).toBeVisible()
  expect(calls.some((call) => call.path === '/api/notifications/read-all')).toBe(true)
  await page.goto('/chat/conversation')
  await page.getByRole('button', { name: 'Thông tin cuộc trò chuyện', exact: true }).click()
  await page.getByRole('button', { name: 'Tắt thông báo', exact: true }).click()
  await expect(page.locator('.conversation-notifications').getByText('Đã cập nhật cài đặt thông báo.')).toBeVisible()
  expect(Date.parse(calls.find((call) => call.path.endsWith('/settings')).data.mutedUntil)).toBeGreaterThan(Date.now())
})
test('final partial text preview is labelled and untrusted text remains escaped (client simulation)', async ({ page, context }) => {
  await fixture(context, page, true)
  let alerts = 0
  page.on('dialog', async (dialog) => { alerts += 1; await dialog.dismiss() })
  await page.goto('/chat/conversation')
  await page.locator('.attachment-card').click()
  await expect(page.getByRole('dialog')).toContainText('Đang hiển thị phần đầu tệp. Tải về để xem đầy đủ nội dung.')
  await expect(page.locator('.text-file-preview')).toContainText('<script>alert("test-only")</script>')
  expect(alerts).toBe(0)
})
test('final cross-feature recall keeps binary avatar cache valid (client simulation)', async ({ page, context }) => {
  await fixture(context, page, false, true)
  const errors = []
  page.on('pageerror', (error) => errors.push(error.message))
  await page.goto('/chat/conversation')
  await expect(page.locator('.thread-header .avatar-photo')).toBeVisible()
  await page.getByRole('textbox', { name: 'Tin nhắn', exact: true }).fill('Tin nhắn để thu hồi.')
  await page.getByRole('button', { name: 'Gửi tin nhắn', exact: true }).click()
  await page.getByRole('button', { name: 'Tùy chọn tin nhắn 1', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Thu hồi', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.locator('.message-bubble')).toContainText('Tin nhắn đã được thu hồi')
  await expect(page.locator('.thread-header .avatar-photo')).toBeVisible()
  expect(errors).toEqual([])
})
for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) test(`notifications ${width}px ${theme}: layout and accessibility (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  await page.goto('/notifications')
  await expect(page.getByText('Vui lòng tuân thủ quy tắc cộng đồng.')).toBeVisible()
  await settleMotion(page)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/notifications', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/notifications/list-${width}-${theme}.png`, fullPage: true })
})
