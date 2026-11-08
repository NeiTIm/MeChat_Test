import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'
import { settleMotion } from './review-helpers'

async function fixture(context, page, { requestMode = false } = {}) {
  const calls = []
  const conversation = { id: 'conversation', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 2, updatedAt: new Date().toISOString() }
  let messages = [
    { id: 'incoming', conversationId: conversation.id, senderUserId: 'friend', sequence: 1, content: 'Keep this English user message.', createdAt: new Date().toISOString(), recalled: false },
    { id: 'own', conversationId: conversation.id, senderUserId: 'actor', sequence: 2, content: 'Mình hẹn cuối tuần nhé.', createdAt: new Date().toISOString(), recalled: false },
  ]
  let reaction = null
  await accountFixture(context, page, async (route) => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    if (path.startsWith('/api/auth/')) return false
    if (req.method() === 'GET') {
      let data
      if (path === '/api/groups') data = []
      else if (path === '/api/conversations/conversation/unread') data = { count: 0, lastReadSequence: 0 }
      else if (path === '/api/conversations') data = [conversation]
      else if (path === '/api/conversations/conversation') data = conversation
      else if (path === '/api/conversations/conversation/messages') data = { items: messages, hasMore: false, nextCursor: null }
      else if (path.endsWith('/messages/search')) data = { items: messages.filter((message) => !message.recalled && message.content?.includes(url.searchParams.get('q'))), hasMore: false }
      else if (path === '/api/unread') data = { mainCount: 1, requestCount: requestMode ? 1 : 0, conversations: [{ conversationId: conversation.id, type: 'Private', count: 1, lastReadSequence: 0 }], notificationCount: 0 }
      else if (path === '/api/message-requests/unread-count') data = { count: requestMode ? 1 : 0 }
      else if (path === '/api/message-requests') data = requestMode ? [{ id: 'request', conversationId: conversation.id, senderUserId: 'friend', receiverUserId: 'actor', status: 'Pending', lastSequence: 2, requestReadSequence: 0, createdAt: new Date().toISOString() }] : []
      else if (path === '/api/users/friend') data = { id: 'friend', displayName: 'Gia Huy', username: 'giahuy' }
      else if (path === '/api/users/friend/presence') data = { userId: 'friend', online: null, lastSeenAt: null }
      else if (path.endsWith('/context')) data = { reply: null, reactions: reaction ? [{ emoji: reaction, count: 1 }] : [], myReaction: reaction }
      else if (path.endsWith('/receipts')) data = { status: 'Delivered', deliveredCount: 1, seenCount: 0 }
      else if (path.endsWith('/edit-history')) data = { versions: [{ version: 1, content: 'Mình hẹn cuối tuần nhé.', editedAt: new Date().toISOString() }] }
      else if (path.startsWith('/api/messages/')) data = messages.find((message) => path === `/api/messages/${message.id}`)
      if (data !== undefined) { await route.fulfill({ json: data }); return true }
    } else {
      calls.push({ path, method: req.method(), data: req.postDataJSON() })
      if (path === '/api/conversations/conversation/messages') {
        const message = { ...req.postDataJSON(), id: `sent-${messages.length}`, conversationId: conversation.id, senderUserId: 'actor', sequence: messages.length + 1, createdAt: new Date().toISOString(), recalled: false }
        messages.push(message)
        await route.fulfill({ json: message }); return true
      }
      if (path === '/api/messages/own' && req.method() === 'PATCH') {
        messages = messages.map((message) => message.id === 'own' ? { ...message, content: req.postDataJSON().content, editedAt: new Date().toISOString() } : message)
        await route.fulfill({ json: messages.find((message) => message.id === 'own') }); return true
      }
      if (path.endsWith('/reaction')) reaction = req.method() === 'DELETE' ? null : req.postDataJSON().emoji
      if (path.endsWith('/recall')) messages = messages.map((message) => message.id === path.split('/')[3] ? { ...message, content: null, recalled: true } : message)
      if (path.endsWith('/for-me')) messages = messages.filter((message) => message.id !== path.split('/')[3])
      await route.fulfill({ status: 204 }); return true
    }
    return false
  })
  return calls
}
const options = (page, sequence) => page.getByRole('button', { name: `Tùy chọn tin nhắn ${sequence}`, exact: true })

test('QA-014 search opens older context once, highlights and handles unavailable history (client simulation)', async ({ page, context }) => {
  await fixture(context, page)
  const old = { id: 'older', conversationId: 'conversation', senderUserId: 'friend', sequence: 100, content: 'Tin cũ để tìm lại.', createdAt: '2026-11-06T00:00:00Z' }
  const cursors = []
  await page.route('**/api/conversations/conversation/messages**', async (route) => {
    if (route.request().method() !== 'GET') return route.fallback()
    const url = new URL(route.request().url())
    if (url.pathname.endsWith('/search')) return route.fulfill({ json: { items: [old], hasMore: false } })
    const before = url.searchParams.get('beforeSequence')
    cursors.push(before)
    if (before) return route.fulfill({ json: { items: before === '101' ? [old] : [], hasMore: false } })
    return route.fallback()
  })
  await page.route('**/api/messages/older', (route) => route.fulfill({ json: old }))
  await page.goto('/chat/conversation')
  await page.getByRole('button', { name: 'Thông tin cuộc trò chuyện', exact: true }).click()
  await page.getByRole('button', { name: 'Tìm tin nhắn', exact: true }).click()
  await page.getByLabel('Nội dung cần tìm').fill('Tin cũ')
  await page.getByRole('dialog').getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await page.getByRole('button', { name: 'Xem tin nhắn', exact: true }).click()
  await page.getByRole('button', { name: 'Đi tới tin nhắn trong cuộc trò chuyện', exact: true }).click()
  await expect(page.locator('[data-message-id="older"]')).toHaveClass(/message-highlighted/)
  await expect(page.locator('[data-message-id="older"]')).toBeFocused()
  await expect(page.getByRole('dialog')).toHaveCount(0)
  expect(cursors).toContain('101')
  await page.getByRole('button', { name: 'Trở về tin mới nhất', exact: true }).click()
  await expect(page.locator('[data-message-id="own"]')).toBeVisible()
  await page.goto('/chat/conversation?at=9&message=missing')
  await expect(page.getByText('Tin nhắn không còn trong phạm vi lịch sử bạn được xem.')).toBeVisible()
})

test('real message consumers preserve content and wire reply/edit/reaction/recall/delete (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/chat/conversation')
  await expect(page.getByText('Keep this English user message.', { exact: true })).toBeVisible()
  await options(page, 1).click()
  await page.getByRole('menuitem', { name: 'Trả lời', exact: true }).click()
  await expect(page.locator('.compose-context')).toContainText('Đang trả lời')
  await page.getByRole('textbox', { name: 'Tin nhắn', exact: true }).fill('Mình đã nhận được tin nhắn!')
  await page.getByRole('button', { name: 'Gửi tin nhắn', exact: true }).click()
  await expect(page.locator('[data-message-id="sent-2"] .message-bubble')).toContainText('Mình đã nhận được tin nhắn!')
  expect(calls.find((call) => call.path.endsWith('/messages')).data).toEqual({ content: 'Mình đã nhận được tin nhắn!', clientMessageId: expect.any(String), replyToMessageId: 'incoming' })
  await options(page, 2).click()
  await page.getByRole('menuitem', { name: 'Chỉnh sửa', exact: true }).click()
  await page.getByRole('textbox', { name: 'Tin nhắn', exact: true }).fill('Mình hẹn sáng Chủ nhật nhé.')
  await page.getByRole('button', { name: 'Lưu chỉnh sửa', exact: true }).click()
  await expect(page.locator('[data-message-id="own"] .message-bubble')).toContainText('Mình hẹn sáng Chủ nhật nhé.')
  expect(calls.find((call) => call.method === 'PATCH').data).toEqual({ content: 'Mình hẹn sáng Chủ nhật nhé.' })
  await options(page, 1).click()
  await page.getByRole('menuitem', { name: 'Bày tỏ cảm xúc', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Cảm xúc ❤️: 1 lượt', exact: true }).click()
  await expect(page.locator('[data-message-id="incoming"] .message-reactions')).toContainText('❤️ 1')
  await options(page, 2).click()
  await page.getByRole('menuitem', { name: 'Thu hồi', exact: true }).click()
  expect(calls.some((call) => call.path.endsWith('/recall'))).toBe(false)
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.locator('[data-message-id="own"]')).toContainText('Tin nhắn đã được thu hồi')
  await options(page, 1).click()
  await page.getByRole('menuitem', { name: 'Xóa phía tôi', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.locator('[data-message-id="incoming"]')).toHaveCount(0)
  expect(calls.some((call) => call.path.endsWith('/delivery-acks') && call.data.messageIds.includes('incoming'))).toBe(true)
  expect(calls.some((call) => call.path.endsWith('/read-cursor'))).toBe(true)
})

test('server search, message detail, forwarding and history consumers (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/chat/conversation')
  await page.getByRole('button', { name: 'Thông tin cuộc trò chuyện', exact: true }).click()
  await page.getByRole('button', { name: 'Tìm tin nhắn', exact: true }).click()
  await page.getByLabel('Nội dung cần tìm').fill('English')
  await page.getByRole('dialog').getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await page.getByRole('button', { name: 'Xem tin nhắn', exact: true }).click()
  await expect(page.getByRole('dialog').last()).toContainText('Keep this English user message.')
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).toHaveCount(1)
  await settleMotion(page)
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).toHaveCount(0)
  await options(page, 2).click()
  await page.getByRole('menuitem', { name: 'Lịch sử chỉnh sửa', exact: true }).click()
  await expect(page.getByRole('dialog')).toContainText('Phiên bản 1')
  await page.keyboard.press('Escape')
  await options(page, 1).click()
  await page.getByRole('menuitem', { name: 'Chuyển tiếp', exact: true }).click()
  await page.getByLabel('Chuyển đến', { exact: true }).selectOption('conversation')
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByRole('dialog')).toHaveCount(0)
  expect(calls.find((call) => call.path.endsWith('/forward')).data).toEqual({ destinationConversationId: 'conversation', clientMessageId: expect.any(String) })
})

test('message requests stay read-only and use separate request read cursors (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page, { requestMode: true })
  await page.goto('/message-requests/conversation')
  await expect(page.getByRole('textbox', { name: 'Tin nhắn', exact: true })).toBeDisabled()
  await expect(page.locator('.request-notice')).toContainText('Chấp nhận lời nhắn trước khi trả lời')
  await expect.poll(() => calls.some((call) => call.path === '/api/message-requests/request/read-cursor')).toBe(true)
  expect(calls.some((call) => call.path === '/api/conversations/conversation/read-cursor')).toBe(false)
  await page.getByRole('button', { name: 'Chấp nhận trò chuyện', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page).toHaveURL(/\/chat\/conversation$/)
  expect(calls.some((call) => call.path === '/api/message-requests/request/accept')).toBe(true)
})
test('message report preserves target identifiers and foreground-only read cursors (client simulation)', async ({ page, context }) => {
  await page.addInitScript(() => { document.hasFocus = () => false })
  const calls = await fixture(context, page)
  await page.goto('/chat/conversation')
  await expect(page.getByText('Keep this English user message.', { exact: true })).toBeVisible()
  await options(page, 1).click()
  await page.getByRole('menuitem', { name: 'Báo cáo vi phạm', exact: true }).click()
  await page.getByLabel('Lý do báo cáo', { exact: true }).fill('Nội dung có dấu hiệu vi phạm.')
  await page.getByRole('button', { name: 'Gửi báo cáo', exact: true }).click()
  await expect(page.locator('.form-feedback').filter({ hasText: 'Đã gửi báo cáo. Bạn có thể theo dõi trong mục Báo cáo của tôi.' })).toBeVisible()
  expect(calls.find((call) => call.path === '/api/reports').data).toEqual({ targetType: 'Message', targetId: 'incoming', reason: 'Nội dung có dấu hiệu vi phạm.' })
  expect(calls.some((call) => call.path === '/api/conversations/conversation/read-cursor')).toBe(false)
})

for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) test(`integrated private chat ${width}px ${theme}: geometry and accessibility (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  await page.goto('/chat/conversation')
  await expect(page.getByText('Keep this English user message.', { exact: true })).toBeVisible()
  await settleMotion(page)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/integration', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/integration/private-${width}-${theme}.png`, fullPage: true })
})
