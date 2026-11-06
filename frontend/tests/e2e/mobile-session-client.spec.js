import { test, expect } from '@playwright/test'
import { accountFixture } from './account-client-fixture'

test('320px account navigation keeps Vietnamese labels on one line and supports logout (client simulation)', async ({ page, context }) => {
  let loggedOut = false
  await accountFixture(context, page, async (route) => {
    if (new URL(route.request().url()).pathname === '/api/auth/logout') { loggedOut = true; await route.fulfill({ status: 204 }); return true }
    return false
  })
  await page.setViewportSize({ width: 320, height: 700 })
  await page.goto('/account')
  await expect(page.getByLabel('Tên hiển thị', { exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  expect(await page.locator('.mobile-nav a > span').evaluateAll((labels) => labels.every((label) => getComputedStyle(label).whiteSpace === 'nowrap' && label.scrollWidth <= label.parentElement.clientWidth))).toBe(true)
  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click()
  await expect(page).toHaveURL(/\/login$/)
  expect(loggedOut).toBe(true)
})

test('320px real chat list exposes session exit outside the bottom navigation (client simulation)', async ({ page, context }) => {
  await accountFixture(context, page, async (route) => {
    const path = new URL(route.request().url()).pathname
    const data = path === '/api/conversations' || path === '/api/message-requests' ? [] : path === '/api/unread' ? { conversations: [], mainCount: 0, requestCount: 0, notificationCount: 0 } : path === '/api/message-requests/unread-count' ? { count: 0 } : undefined
    if (data !== undefined) { await route.fulfill({ json: data }); return true }
    return false
  })
  await page.setViewportSize({ width: 320, height: 700 })
  await page.goto('/chat')
  await expect(page.getByRole('heading', { name: 'Tin nhắn.', exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  await expect(page.locator('.mobile-theme').getByRole('button', { name: 'Đăng xuất', exact: true })).toBeVisible()
  await expect(page.locator('.mobile-nav').getByRole('button', { name: 'Đăng xuất', exact: true })).toHaveCount(0)
})
