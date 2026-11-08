/* global chrome */
import { chromium } from '@playwright/test'
import { mkdir, writeFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import assert from 'node:assert/strict'
import { accountFixture } from '../tests/e2e/account-client-fixture.js'

const extension = resolve('tests/fixtures/zoom-extension')
const directory = resolve('../docs/frontend/screenshots/vi/improvements')
await mkdir(directory, { recursive: true })
const cases = [], errors = []
let context
try {
  context = await chromium.launchPersistentContext(resolve('test-results/zoom-profile'), {
    channel: 'chromium', headless: true, viewport: null,
    args: [`--disable-extensions-except=${extension}`, `--load-extension=${extension}`, '--window-size=1440,1000'],
  })
  const worker = context.serviceWorkers()[0] || await context.waitForEvent('serviceworker', { timeout: 10000 })
  for (const theme of ['light', 'dark']) {
    const page = await context.newPage()
    page.on('pageerror', (error) => errors.push(error.message))
    await page.addInitScript((theme) => localStorage.setItem('mechat-theme', theme), theme)
    const room = { id: 'zoom-room', type: 'Private', status: 'Active', otherUserId: 'friend', lastSequence: 1, updatedAt: '2026-11-07T00:00:00Z' }
    const group = { id: 'zoom-group', name: 'Nhóm kiểm thử zoom', status: 'Active', myRole: 'Owner', lastSequence: 1 }
    await accountFixture(context, page, async (route) => {
      const req = route.request(), path = new URL(req.url()).pathname
      let data
      if (path === '/api/conversations') data = [room]
      else if (path === '/api/groups') data = [group]
      else if (path === '/api/conversations/zoom-room') data = room
      else if (path === '/api/groups/zoom-group') data = group
      else if (path.endsWith('/messages')) data = { items: [{ id: 'zoom-message', senderUserId: 'actor', sequence: 1, content: 'Nội dung tổng hợp để kiểm tra khoảng trống, căn lề và co giãn của khung chat. '.repeat(6), createdAt: '2026-11-07T00:00:00Z' }], hasMore: false }
      else if (path.endsWith('/members')) data = [{ userId: 'actor', displayName: 'Người kiểm thử', role: 'Owner' }]
      else if (path === '/api/users/friend') data = { id: 'friend', displayName: 'Bạn kiểm thử', username: 'test' }
      else if (path === '/api/unread') data = { conversations: [], requestCount: 0, mainCount: 0, notificationCount: 0 }
      else if (path.endsWith('/unread') || path.endsWith('/unread-count')) data = { count: 0 }
      else if (path.endsWith('/presence')) data = { online: null, lastSeenAt: null }
      else if (path.endsWith('/settings')) data = { mutedUntil: null }
      else if (path.endsWith('/context')) data = { reply: null, reactions: [], myReaction: null }
      else if (path.endsWith('/receipts')) data = { status: 'Sent', deliveredCount: 0, seenCount: 0 }
      else if (path.endsWith('/pins')) data = []
      else if (path === '/api/message-requests') data = []
      if (data !== undefined) { await route.fulfill({ json: data }); return true }
      if (req.method() !== 'GET' && !path.startsWith('/api/auth/')) { await route.fulfill({ status: 204 }); return true }
      return false
    })
    const cdp = await context.newCDPSession(page)
    const { windowId } = await cdp.send('Browser.getWindowForTarget')
    for (const width of [1366, 1440]) for (const area of ['chat', 'groups']) {
      await cdp.send('Browser.setWindowBounds', { windowId, bounds: { width, height: 1000, windowState: 'normal' } })
      await page.goto(`http://127.0.0.1:5173/${area}/${area === 'chat' ? room.id : group.id}`)
      for (const zoom of [0.8, 0.9, 1, 1.1, 1.25, 1.5, 2]) {
        const applied = await worker.evaluate(async ({ zoom }) => {
          const tabs = await chrome.tabs.query({ url: 'http://127.0.0.1:5173/*' })
          const tab = tabs.at(-1)
          await chrome.tabs.setZoom(tab.id, zoom)
          return chrome.tabs.getZoom(tab.id)
        }, { zoom })
        assert.ok(Math.abs(applied - zoom) < 0.001, 'Browser did not apply actual tabs zoom')
        await page.locator('.message-bubble').waitFor()
        await page.evaluate(async () => { await document.fonts.ready; await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r))) })
        const facts = await page.evaluate(() => {
          const composer = document.querySelector('.composer').getBoundingClientRect()
          const scroll = document.querySelector('.message-scroll').getBoundingClientRect()
          const message = document.querySelector('.message-bubble').getBoundingClientRect()
          return { innerWidth, innerHeight, dpr: devicePixelRatio, overflow: document.documentElement.scrollWidth - innerWidth, composerBottom: composer.bottom, composerWidth: composer.width, rightGap: scroll.right - message.right }
        })
        assert.ok(facts.overflow <= 1, `Horizontal overflow at ${zoom}`)
        assert.ok(facts.composerBottom <= facts.innerHeight + 1 && facts.composerWidth > 200, 'Composer clipped')
        assert.ok(facts.rightGap < 65, `Message area has excessive inset: ${facts.rightGap}`)
        cases.push({ width, area, theme, browserZoom: applied, ...facts, status: 'PASS' })
        if (width === 1440 && [0.8, 1, 1.5, 2].includes(zoom)) {
          // Capture the physical viewport directly. Playwright's CSS-sized
          // screenshot clip can append blank margins at non-100% browser zoom.
          const capture = await cdp.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false })
          await writeFile(`${directory}/zoom-${area}-${Math.round(zoom * 100)}-${theme}.png`, Buffer.from(capture.data, 'base64'))
        }
      }
    }
    await page.close()
  }
  assert.deepEqual(errors, [])
} catch (error) {
  errors.push(error.message)
  process.exitCode = 1
} finally { await context?.close() }
const report = { capturedAt: new Date().toISOString(), scope: 'Bundled Chromium actual chrome.tabs.setZoom, synthetic accounts/HTTP simulation. Separate from responsive viewport testing. Native installed Chrome/Edge and physical devices remain NEEDS_VERIFICATION.', cases, errors }
await writeFile('../docs/frontend/improvement-browser-zoom.json', JSON.stringify(report, null, 2) + '\n')
console.log(JSON.stringify({ cases: cases.length, errors, evidence: 'improvement-browser-zoom.json' }))
