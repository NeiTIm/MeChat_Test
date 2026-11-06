import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { mkdir } from 'node:fs/promises'
import { accountFixture } from './account-client-fixture'
import { settleMotion } from './review-helpers'

async function fixture(context, page, role = 'Admin', auditAction = 'WarnUser') {
  const calls = []
  const user = { id: 'target', email: 'giahuy@example.test', username: 'giahuy', systemRole: 'User', suspended: false, createdAt: '2026-11-06T07:00:00Z' }
  const report = { id: 'report', reporterUserId: 'reporter', targetType: 'User', targetId: user.id, reason: 'Cần xem xét hành vi vi phạm.', status: 'Pending', createdAt: '2026-11-06T07:00:00Z', reviewedByAdminId: null }
  const evidence = { id: 'evidence', sourceMessageId: 'message', evidenceContent: 'Evidence content remains unchanged.', storedFileId: 'file', capturedAt: '2026-11-06T07:00:00Z', legalHold: false }
  const appeal = { id: 'appeal', userId: user.id, username: user.username, suspensionId: 'suspension', reason: 'Tôi muốn được xem xét lại.', status: 'Pending', createdAt: '2026-11-06T07:00:00Z' }
  const audit = { id: 'audit', adminUserId: 'actor', action: auditAction, targetType: 'User', targetId: user.id, details: auditAction === 'RetentionDryRun' ? 'offset=1000;limit=30;no deletion' : 'Cảnh báo về hành vi vi phạm.', createdAt: '2026-11-06T07:00:00Z', legalHold: false }
  let mine = []
  await accountFixture(context, page, async (route, me) => {
    me.systemRole = role
    const req = route.request(), url = new URL(req.url()), path = url.pathname
    if (path.startsWith('/api/auth/')) return false
    calls.push({ path, method: req.method(), data: req.postDataJSON(), authorization: req.headers().authorization })
    if (req.method() === 'GET') {
      if (path.endsWith('/evidence/evidence/file')) { await route.fulfill({ contentType: 'text/plain', headers: { 'Content-Disposition': "attachment; filename*=UTF-8''bang-chung.txt" }, body: 'test-only evidence' }); return true }
      let data
      if (path === '/api/admin/dashboard') data = { users: 124, messages: 1234, groups: 12, pendingReports: 3, activity: [{ date: '2026-11-06T00:00:00Z', newUsers: 7, messages: 120 }] }
      else if (path === '/api/admin/users') data = [user]
      else if (path === '/api/admin/groups') data = [{ id: 'group', name: 'Góc sáng tạo', status: 'Active', memberCount: 5 }]
      else if (path === '/api/admin/reports') data = [report]
      else if (path === '/api/admin/reports/report') data = { report, evidence: [evidence] }
      else if (path === '/api/admin/appeals') data = [appeal]
      else if (path === '/api/admin/audit') data = [audit]
      else if (path === '/api/admin/retention/preview') data = { evidenceEligible: 1, auditEligible: 2, evidenceHeld: 0, auditHeld: 1 }
      else if (path === '/api/admin/retention/files/preview') data = { items: [{ id: 'file', reason: 'ExpiredPendingUpload', sizeBytes: 2048 }], hasMore: false, deletionEnabled: false }
      else if (path === '/api/admin/retention/plan') data = { kind: url.searchParams.get('kind'), items: [{ id: 'audit', eligibleSince: '2026-11-06T07:00:00Z' }], nextOffset: null, dryRun: true }
      else if (path === '/api/reports/me') data = mine
      else if (path === '/api/appeals/me') data = mine
      if (data !== undefined) { await route.fulfill({ json: data }); return true }
    } else {
      if (path === '/api/appeals/authenticate') { await route.fulfill({ json: { accessToken: 'appeal-only-test-token', expiresAt: '2099-01-01T00:00:00Z' } }); return true }
      if (path === '/api/reports') mine = [{ ...report, ...req.postDataJSON() }]
      if (path === '/api/appeals') mine = [{ ...appeal, ...req.postDataJSON() }]
      if (path.endsWith('/claim')) report.reviewedByAdminId = 'actor'
      if (path.endsWith('/resolve')) report.status = req.postDataJSON().status
      if (path.endsWith('/suspend')) user.suspended = true
      if (path.endsWith('/unsuspend')) user.suspended = false
      if (path.endsWith('/review')) appeal.status = req.postDataJSON().status
      if (path.endsWith('/hold')) { audit.legalHold = req.postDataJSON().hold; evidence.legalHold = req.postDataJSON().hold }
      await route.fulfill({ status: 204 }); return true
    }
    return false
  })
  return calls
}
async function confirmReason(page, reason = 'Lý do kiểm thử thao tác quản trị.') {
  await page.getByRole('dialog').getByRole('textbox').fill(reason)
  await page.getByRole('dialog').getByRole('button', { name: 'Tiếp tục', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận thực hiện', exact: true }).click()
}
test('DB-role route guard prevents a User from loading admin APIs (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page, 'User')
  await page.goto('/admin')
  await expect(page.getByRole('heading', { name: 'Không có quyền truy cập', exact: true })).toBeVisible()
  expect(calls.filter((call) => call.path.startsWith('/api/admin'))).toHaveLength(0)
})
test('admin reason confirmation, report claim/evidence/resolve and hold are wired (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/admin/users')
  await page.getByRole('button', { name: 'Gửi cảnh báo', exact: true }).click()
  await page.getByRole('dialog').getByRole('textbox').fill('Lý do kiểm thử thao tác quản trị.')
  await page.getByRole('dialog').getByRole('button', { name: 'Tiếp tục', exact: true }).click()
  expect(calls.some((call) => call.path.endsWith('/warn'))).toBe(false)
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận thực hiện', exact: true }).click()
  await expect(page.getByText('Đã thực hiện thao tác.')).toBeVisible()
  expect(calls.find((call) => call.path.endsWith('/warn')).data).toEqual({ reason: 'Lý do kiểm thử thao tác quản trị.' })
  await page.goto('/admin/reports')
  await page.getByRole('button', { name: 'Nhận xử lý', exact: true }).click()
  await expect(page).toHaveURL(/\/admin\/reports\/report$/)
  await expect(page.getByText('Evidence content remains unchanged.')).toBeVisible()
  const downloaded = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Tải tệp bằng chứng', exact: true }).click()
  expect((await downloaded).suggestedFilename()).toBe('bang-chung.txt')
  await page.getByRole('button', { name: 'Bảo lưu dữ liệu', exact: true }).click()
  await confirmReason(page)
  await expect.poll(() => calls.some((call) => call.path.endsWith('/hold'))).toBe(true)
  await expect(page.getByRole('dialog').getByText('Đã thực hiện thao tác.')).toBeVisible()
  expect(calls.find((call) => call.path.endsWith('/hold')).data).toEqual({ hold: true, reason: 'Lý do kiểm thử thao tác quản trị.' })
  await page.keyboard.press('Escape')
  await settleMotion(page)
  await page.getByRole('button', { name: 'Đánh dấu đã xử lý', exact: true }).click()
  await confirmReason(page)
  await expect(page.locator('.role-chip').filter({ hasText: 'Đã xử lý' })).toBeVisible()
  expect(calls.find((call) => call.path.endsWith('/resolve')).data).toEqual({ status: 'Resolved', note: 'Lý do kiểm thử thao tác quản trị.' })
})
test('retention is explicit dry-run and appeal acceptance stays a separate decision (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page)
  await page.goto('/admin/retention')
  await expect(page.getByText('Tác vụ xóa phía máy chủ đang tắt.')).toBeVisible()
  expect(calls.some((call) => call.path.endsWith('/plan'))).toBe(false)
  await page.getByLabel('Loại dữ liệu', { exact: true }).selectOption('Audit')
  await page.getByRole('button', { name: 'Xem kế hoạch', exact: true }).click()
  await expect(page.getByText('Đủ điều kiện từ 06/11/2026')).toBeVisible()
  expect(calls.filter((call) => call.method !== 'GET')).toHaveLength(0)
  await page.goto('/admin/appeals')
  await page.getByRole('button', { name: 'Chấp nhận khiếu nại', exact: true }).click()
  await expect(page.getByRole('dialog')).toContainText('không tự động gỡ đình chỉ')
  await confirmReason(page)
  await expect.poll(() => calls.some((call) => call.path.endsWith('/review'))).toBe(true)
  expect(calls.find((call) => call.path.endsWith('/review')).data.status).toBe('Accepted')
  expect(calls.some((call) => call.path.endsWith('/unsuspend'))).toBe(false)
})
test('appeal token is isolated from normal auth and cleared on ending appeal session (client simulation)', async ({ page, context }) => {
  const calls = await fixture(context, page, 'User')
  await page.goto('/appeals')
  await page.getByLabel('Email hoặc tên đăng nhập', { exact: true }).fill('suspended-test')
  await page.getByLabel('Mật khẩu', { exact: true }).fill('test-only-password')
  await page.getByRole('button', { name: 'Xác thực tài khoản', exact: true }).click()
  await page.getByLabel('Nội dung khiếu nại', { exact: true }).fill('Tôi muốn được xem xét lại quyết định đình chỉ.')
  await page.getByRole('button', { name: 'Gửi khiếu nại', exact: true }).click()
  await expect(page.getByText('Bạn đang có khiếu nại chờ xem xét.')).toBeVisible()
  expect(calls.find((call) => call.path === '/api/appeals').authorization).toBe('Bearer appeal-only-test-token')
  expect(await page.evaluate(() => JSON.stringify({ ...localStorage, ...sessionStorage }))).not.toContain('appeal-only-test-token')
  await page.getByRole('button', { name: 'Kết thúc phiên khiếu nại', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Xác thực tài khoản', exact: true })).toBeVisible()
  await page.goto('/account')
  await expect(page.getByLabel('Tên hiển thị', { exact: true })).toBeVisible()
  expect(calls.filter((call) => call.path === '/api/users/me').every((call) => call.authorization === 'Bearer test-only')).toBe(true)
})
test('final audit metadata is displayed in Vietnamese (client simulation)', async ({ page, context }) => {
  await fixture(context, page, 'Admin', 'RetentionDryRun')
  await page.goto('/admin/audit')
  await expect(page.getByText('Vị trí bắt đầu: 1.000 · Tối đa 30 mục · Không thực hiện xóa.')).toBeVisible()
  await expect(page.getByText('offset=1000;limit=30;no deletion', { exact: true })).toHaveCount(0)
})
for (const theme of ['light', 'dark']) for (const width of [320, 390, 768, 1366, 1440]) test(`admin/moderation ${width}px ${theme}: Vietnamese layout and accessibility (client simulation)`, async ({ page, context }) => {
  await page.setViewportSize({ width, height: 900 })
  await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
  await fixture(context, page)
  await mkdir('../docs/frontend/screenshots/vi/admin', { recursive: true })
  for (const [path, name, heading] of [['/admin', 'dashboard', 'Hoạt động 7 ngày'], ['/admin/users', 'users', '@giahuy'], ['/reports', 'reports', 'Bạn chưa gửi báo cáo nào'], ['/appeals', 'appeals', 'Khiếu nại tài khoản']]) {
    await page.goto(path)
    await expect(page.getByRole('heading', { name: heading, exact: true })).toBeVisible()
    await settleMotion(page)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    await page.screenshot({ path: `../docs/frontend/screenshots/vi/admin/${name}-${width}-${theme}.png`, fullPage: true })
  }
})
