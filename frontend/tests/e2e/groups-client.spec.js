import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'

async function fixture(context, page, role = 'Owner') {
  const calls = []
  const group = { id: 'group', name: 'Góc sáng tạo của chúng mình', description: 'Cùng sẻ chia những ý tưởng nhỏ mỗi ngày.', status: 'Active', myRole: role, lastSequence: 1, createdByUserId: 'actor', avatarFileId: null }
  const members = [{ userId: 'actor', displayName: 'Minh Anh', username: 'minhanh', role, joinedAt: '2026-11-06T07:00:00Z' }, { userId: 'member', displayName: 'Gia Huy', username: 'giahuy', role: 'Member', joinedAt: '2026-11-06T07:00:00Z' }, { userId: 'admin', displayName: 'Ngọc Hà', username: 'ngocha', role: 'Admin', joinedAt: '2026-11-06T07:00:00Z' }]
  let links = []
  await accountFixture(context, page, async (route) => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    if (path.startsWith('/api/auth/')) return false
    if (req.method() === 'GET') {
      let data
      if (path === '/api/groups') data = [group]
      else if (path === '/api/groups/group') data = group
      else if (path.endsWith('/members')) data = members
      else if (path.endsWith('/invite-links')) data = links
      else if (path === '/api/group-invitations') data = [{ id: 'invite', conversationId: 'group', groupName: group.name, createdAt: '2026-11-06T07:00:00Z' }]
      else if (path === '/api/users/search') data = { items: [{ id: 'invitee', displayName: 'Tuấn Minh', username: 'tuanminh' }], hasMore: false }
      else if (path === '/api/groups/group/messages') data = { items: [{ id: 'message', conversationId: 'group', senderUserId: 'actor', sequence: 1, content: 'Ý tưởng cho buổi gặp mặt.', createdAt: new Date().toISOString(), recalled: false }], hasMore: false }
      else if (path.endsWith('/pins')) data = [{ messageId: 'message', pinnedAt: '2026-11-06T07:00:00Z', pinnedByUserId: 'actor' }]
      else if (path.endsWith('/pins/page')) data = { items: [{ messageId: 'message', pinnedAt: '2026-11-06T07:00:00Z', pinnedByUserId: 'actor' }], hasMore: false }
      else if (path === '/api/messages/message') data = { id: 'message', content: 'Ý tưởng cho buổi gặp mặt.' }
      else if (path.endsWith('/context')) data = { reply: null, reactions: [], myReaction: null }
      else if (path.endsWith('/receipts')) data = { status: 'Sent', deliveredCount: 0, seenCount: 0 }
      else if (path === '/api/unread') data = { mainCount: 0, requestCount: 0, conversations: [], notificationCount: 0 }
      if (data !== undefined) { await route.fulfill({ json: data }); return true }
    } else {
      calls.push({ path, method: req.method(), data: req.postDataJSON() })
      if (path === '/api/groups') { await route.fulfill({ json: { ...group, ...req.postDataJSON() } }); return true }
      if (path === '/api/groups/group') Object.assign(group, req.postDataJSON())
      if (path.endsWith('/role')) members.find((member) => member.userId === path.split('/')[5]).role = req.postDataJSON().role
      if (path.endsWith('/invite-links')) { const link = { id: 'link', createdByUserId: 'actor', createdAt: new Date().toISOString(), expiresAt: req.postDataJSON().expiresAt }; links.push(link); await route.fulfill({ json: { ...link, token: 'test-only-invite-code' } }); return true }
      if (path.endsWith('/resolve')) { await route.fulfill({ json: { groupId: group.id, name: group.name, description: group.description } }); return true }
      if (path.endsWith('/join')) { await route.fulfill({ json: { groupId: group.id } }); return true }
      await route.fulfill({ status: 204 }); return true
    }
    return false
  })
  return calls
}

test('MC-005 one-time invite link survives permission cache reload (client simulation)', async ({ page, context }) => {
  await fixture(context, page)
  await page.goto('/groups/group/manage')
  await page.getByRole('button', { name: 'Tạo liên kết mời', exact: true }).click()
  await expect(page.getByLabel('Liên kết vừa tạo')).toHaveValue(/#token=test-only-invite-code$/)
  // Same cache operation as GroupChanged. This is not a SignalR transport test.
  await page.evaluate(async () => {
    const { queryClient } = await import('/src/lib/query-client.js')
    await queryClient.resetQueries({ queryKey: ['workspace'] })
  })
  await expect(page.getByLabel('Liên kết vừa tạo')).toHaveValue(/#token=test-only-invite-code$/)
})

test('MC-004 create offers an explicit invite action without silently creating a link (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/groups/new')
  await page.getByLabel('Tên nhóm', { exact: true }).fill('Nhóm kiểm thử lời mời')
  await page.getByRole('button', { name: 'Tạo nhóm', exact: true }).click()
  await expect(page.locator('.group-created-notice')).toBeVisible()
  await mkdir('../docs/frontend/screenshots/vi/improvements', { recursive: true })
  await page.screenshot({ path: '../docs/frontend/screenshots/vi/improvements/group-created-390-light.png' })
  expect(calls.some((call) => call.path.endsWith('/invite-links'))).toBe(false)
  await page.locator('.group-created-notice').getByRole('link', { name: 'Mời thành viên / Tạo liên kết mời', exact: true }).click()
  await expect(page.locator('#invite-links')).toBeFocused()
  await expect(page.getByRole('button', { name: 'Tạo liên kết mời', exact: true })).toBeVisible()
})

test('QA-017 past membership cannot compose or manage an active group (client simulation)', async ({ page, context }) => {
  await fixture(context, page, null)
  await page.goto('/groups/group/manage')
  await expect(page.getByRole('button', { name: 'Tạo liên kết mời', exact: true })).toHaveCount(0)
  await expect(page.getByLabel('Tên nhóm', { exact: true })).toHaveCount(0)
  await page.goto('/groups/group')
  await expect(page.getByRole('textbox', { name: 'Tin nhắn', exact: true })).toBeDisabled()
  await page.getByRole('button', { name: 'Thông tin cuộc trò chuyện', exact: true }).click()
  await expect(page.getByRole('link', { name: 'Mời thành viên / Tạo liên kết mời', exact: true })).toHaveCount(0)
})

test('MC-005 rejects a create response without a one-time token (client simulation)', async ({ page, context }) => {
  await fixture(context, page)
  await page.route('**/api/groups/group/invite-links', (route) => route.request().method() === 'POST' ? route.fulfill({ json: { id: 'no-token' } }) : route.fallback())
  await page.goto('/groups/group/manage')
  await page.getByRole('button', { name: 'Tạo liên kết mời', exact: true }).click()
  await expect(page.locator('form').filter({ has: page.getByRole('button', { name: 'Tạo liên kết mời', exact: true }) }).getByRole('alert')).toBeVisible()
  await expect(page.getByLabel('Liên kết vừa tạo')).toHaveCount(0)
})

test('create, invite, group profile and role DTOs (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/groups/new')
  await page.getByLabel('Tên nhóm', { exact: true }).fill('Hẹn cuối tuần')
  await page.getByLabel('Giới thiệu nhóm', { exact: true }).fill('Hẹn nhau đi cà phê.')
  await page.getByRole('button', { name: 'Tạo nhóm', exact: true }).click()
  await expect(page).toHaveURL(/\/groups\/group$/)
  expect(calls.find((call) => call.path === '/api/groups').data).toEqual({ name: 'Hẹn cuối tuần', description: 'Hẹn nhau đi cà phê.', inviteeUserIds: [] })
  await page.goto('/groups/group/manage')
  await page.getByLabel('Tên nhóm', { exact: true }).fill('Nhóm của chúng mình')
  await page.locator('form').filter({ has: page.getByLabel('Tên nhóm', { exact: true }) }).getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(page.locator('.form-feedback').filter({ hasText: 'Đã cập nhật thông tin nhóm.' })).toBeVisible()
  expect(calls.find((call) => call.path === '/api/groups/group').data).toEqual({ name: 'Nhóm của chúng mình', description: 'Cùng sẻ chia những ý tưởng nhỏ mỗi ngày.', avatarFileId: null })
  await page.getByLabel('Tìm theo tên hoặc tên đăng nhập', { exact: true }).fill('Tuấn')
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await page.getByRole('radio', { name: /Tuấn Minh/ }).check()
  await page.getByRole('button', { name: 'Gửi lời mời', exact: true }).click()
  await expect(page.locator('.form-feedback').filter({ hasText: 'Đã gửi lời mời vào nhóm.' })).toBeVisible()
  expect(calls.find((call) => call.path.endsWith('/invitations')).data).toEqual({ inviteeUserId: 'invitee' })
  const member = page.getByRole('article').filter({ hasText: 'Gia Huy' })
  await member.getByRole('button', { name: 'Đặt làm quản trị viên', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(member.getByRole('button', { name: 'Chuyển thành thành viên', exact: true })).toBeVisible()
  expect(calls.find((call) => call.path.endsWith('/role')).data).toEqual({ role: 'Admin' })
})

test('invite links resolve before explicit join and tokens stay out of storage (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/groups/group/manage')
  await page.getByRole('button', { name: 'Tạo liên kết mời', exact: true }).click()
  await expect(page.getByLabel('Liên kết vừa tạo')).toHaveValue(/#token=test-only-invite-code$/)
  const link = await page.getByLabel('Liên kết vừa tạo').inputValue()
  expect(Date.parse(calls.find((call) => call.path.endsWith('/invite-links')).data.expiresAt)).toBeGreaterThan(Date.now())
  await page.goto(link)
  await expect(page.getByLabel('Mã mời vào nhóm')).toHaveValue('test-only-invite-code')
  await expect(page).toHaveURL(/\/groups\/join$/)
  expect(calls.some((call) => call.path.endsWith('/join'))).toBe(false)
  await page.getByRole('button', { name: 'Xem thông tin nhóm', exact: true }).click()
  await page.getByRole('button', { name: 'Xác nhận tham gia', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page).toHaveURL(/\/groups\/group$/)
  expect(calls.find((call) => call.path.endsWith('/join')).data).toEqual({ token: 'test-only-invite-code', confirm: true })
  expect(await page.evaluate(() => JSON.stringify({ ...localStorage, ...sessionStorage }))).not.toContain('test-only-invite-code')
})

test('member role hides privileged actions while profile and pins remain available (client simulation)', async ({ page, context }) => {
  await fixture(context, page, 'Member')
  await page.goto('/groups/group/manage')
  await expect(page.getByRole('heading', { name: 'Thành viên nhóm', exact: true })).toBeVisible()
  for (const name of ['Đặt làm quản trị viên', 'Chuyển thành thành viên', 'Mời ra khỏi nhóm', 'Chuyển quyền chủ nhóm', 'Giải tán nhóm']) await expect(page.getByRole('button', { name, exact: true })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Rời nhóm', exact: true })).toBeVisible()
})

for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) test(`group management ${width}px ${theme}: Vietnamese layout and contrast (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  await page.goto('/groups/group/manage')
  await expect(page.getByRole('heading', { name: 'Thành viên nhóm', exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  await mkdir('../docs/frontend/screenshots/vi/groups', { recursive: true })
  await page.screenshot({ path: `../docs/frontend/screenshots/vi/groups/manage-${width}-${theme}.png`, fullPage: true })
})
