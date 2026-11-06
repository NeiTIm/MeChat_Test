import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'

async function fixture(context, page) {
  const calls = []
  const people = [
    { id: 'friend', userId: 'friend', displayName: 'Nguyễn Hoàng Gia Huy', username: 'giahuy', bio: 'Thích đọc sách và cà phê.', since: '2026-11-06T07:00:00Z' },
    { id: 'new-person', displayName: 'Ngọc Hà', username: 'ngocha' },
    { id: 'incoming', displayName: 'Đức Phúc', username: 'ducphuc' },
    { id: 'outgoing', displayName: 'Tuấn Minh', username: 'tuanminh' },
  ]
  let friends = [people[0]], blocks = []
  const requests = {
    incoming: [{ id: 'request-in', senderUserId: 'incoming', receiverUserId: 'actor', status: 'Pending', createdAt: '2026-11-06T07:00:00Z' }],
    outgoing: [{ id: 'request-out', senderUserId: 'actor', receiverUserId: 'outgoing', status: 'Pending', createdAt: '2026-11-06T07:00:00Z' }],
  }
  await accountFixture(context, page, async (route) => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    if (path === '/api/friends' && req.method() === 'GET') { await route.fulfill({ json: friends }); return true }
    if (path === '/api/friends/requests' && req.method() === 'GET') { await route.fulfill({ json: requests }); return true }
    if (path === '/api/blocks' && req.method() === 'GET') { await route.fulfill({ json: blocks }); return true }
    if (path === '/api/users/search') { await route.fulfill({ json: { items: url.searchParams.get('cursor') ? [people[2], people[3]] : people.slice(0, 2), hasMore: !url.searchParams.get('cursor'), nextCursor: 'page2' } }); return true }
    const person = people.find((person) => path === `/api/users/${person.id}`)
    if (person) { await route.fulfill({ json: person }); return true }
    if (path.startsWith('/api/friends') || path.startsWith('/api/blocks') || path === '/api/conversations/private') {
      calls.push({ path, method: req.method(), data: req.postDataJSON() })
      if (path.endsWith('/accept')) { friends.push({ ...people[2], userId: 'incoming', since: '2026-11-06T07:00:00Z' }); requests.incoming = [] }
      if (path.endsWith('/reject')) requests.incoming = []
      if (path.endsWith('/cancel')) requests.outgoing = []
      if (path === '/api/friends/requests') requests.outgoing.push({ id: 'request-new', senderUserId: 'actor', receiverUserId: req.postDataJSON().targetUserId, status: 'Pending', createdAt: '2026-11-06T07:00:00Z' })
      if (path === '/api/blocks') { blocks = [{ ...people[0], blockedAt: '2026-11-06T07:00:00Z' }]; friends = [] }
      if (path.startsWith('/api/blocks/')) blocks = []
      await route.fulfill({ status: path === '/api/conversations/private' ? 200 : 204, ...(path === '/api/conversations/private' ? { json: { id: 'chat-id' } } : {}) }); return true
    }
    return false
  })
  return calls
}

test('friend requests, cursor search, profile and block confirmations (client simulation)', async ({ context, page }) => {
  const calls = await fixture(context, page)
  await page.goto('/friends/incoming')
  await page.getByRole('button', { name: 'Chấp nhận', exact: true }).click()
  await expect(page.getByText('Đã chấp nhận lời mời. Hai bạn đã trở thành bạn bè.')).toBeVisible()
  expect(calls.at(-1).path).toBe('/api/friends/requests/request-in/accept')
  await page.goto('/friends/outgoing')
  await page.getByRole('button', { name: 'Hủy lời mời', exact: true }).click()
  await expect(page.getByText('Bạn chưa gửi lời mời nào')).toBeVisible()
  await page.goto('/friends/search')
  await page.getByLabel('Tìm theo tên hoặc tên đăng nhập').fill('ng')
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await expect(page.getByText('2 người đã tải')).toBeVisible()
  await page.getByRole('button', { name: 'Tải thêm kết quả', exact: true }).click()
  await expect(page.getByText('4 người đã tải')).toBeVisible()
  await page.getByRole('article').filter({ hasText: 'Ngọc Hà' }).getByRole('button', { name: 'Kết bạn', exact: true }).click()
  await expect(page.getByText('Đã gửi lời mời kết bạn.')).toBeVisible()
  expect(calls.at(-1).data).toEqual({ targetUserId: 'new-person' })
  await page.goto('/friends')
  const row = page.getByRole('article').filter({ hasText: 'Nguyễn Hoàng Gia Huy' })
  await row.getByRole('button', { name: /Nguyễn Hoàng Gia Huy/ }).click()
  await expect(page.getByRole('dialog')).toContainText('Thích đọc sách và cà phê.')
  await page.keyboard.press('Escape')
  await row.getByRole('button', { name: 'Chặn', exact: true }).click()
  expect(calls.some((call) => call.path === '/api/blocks')).toBe(false)
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByText('Đã chặn người dùng.')).toBeVisible()
  expect(calls.at(-1).data).toEqual({ targetUserId: 'friend' })
  await page.goto('/friends/blocked')
  await page.getByRole('button', { name: 'Bỏ chặn', exact: true }).click()
  await expect(page.getByRole('dialog')).toContainText('không tự động được khôi phục')
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByText('Bạn chưa chặn ai')).toBeVisible()
})

for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) test(`friends ${width}px ${theme}: Vietnamese layout, focus and contrast (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  const errors = []
  page.on('pageerror', (error) => errors.push(error.message))
  await page.goto('/friends')
  await expect(page.getByText('Nguyễn Hoàng Gia Huy', { exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/friends', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/friends/list-${width}-${theme}.png`, fullPage: true })
  expect(errors).toEqual([])
})
