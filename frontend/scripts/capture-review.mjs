import { chromium } from '@playwright/test'
import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'

const phase = process.argv[2] || 'before'
if (!['before', 'after'].includes(phase)) throw new Error('Use before or after')
const root = new URL(`../../docs/frontend/screenshots/hybrid-${phase}/`, import.meta.url)
await mkdir(root, { recursive: true })
const browser = await chromium.launch()
const evidence = []
try {
  for (const width of [320, 390, 768, 1366, 1440]) for (const theme of ['light', 'dark']) {
    const context = await browser.newContext({ viewport: { width, height: width < 768 ? 844 : width === 768 ? 1024 : 1000 }, reducedMotion: 'reduce' })
    await context.addInitScript((value) => localStorage.setItem('mechat-theme', value), theme)
    for (const screen of ['login', 'private', 'group']) {
      const page = await context.newPage()
      const errors = []
      page.on('pageerror', (error) => errors.push(error.message))
      await page.goto(`http://127.0.0.1:5173/design/${screen}`)
      await page.getByRole('heading', { name: screen === 'login' ? 'Welcome back.' : screen === 'group' ? 'The Creative Corner' : 'Jamie Chen', exact: true }).waitFor()
      await page.evaluate(() => document.fonts.ready)
      const geometry = await page.evaluate(() => ({
        viewport: { width: innerWidth, height: innerHeight },
        document: { width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight },
        panes: [...document.querySelectorAll('.thread-header,.message-scroll,.composer,.review-toolbar,.review-state-control')].map((element) => {
          const rect = element.getBoundingClientRect()
          return { selector: element.className, width: rect.width, scrollWidth: element.scrollWidth, clientWidth: element.clientWidth, top: rect.top, bottom: rect.bottom }
        }),
      }))
      await page.screenshot({ path: fileURLToPath(new URL(`${screen}-${width}-${theme}.png`, root)), fullPage: true, animations: 'disabled' })
      if (phase === 'after') {
        await page.getByRole('button', { name: screen === 'login' ? 'Forgot password?' : 'Conversation details', exact: true }).click()
        await page.evaluate(async () => { await Promise.all(document.getAnimations().filter((animation) => animation.effect?.getTiming().iterations !== Infinity).map((animation) => animation.finished.catch(() => {}))) })
        // Modal screenshots use the actual viewport; full-page capture would show
        // offscreen login content below a correctly fixed viewport overlay.
        await page.screenshot({ path: fileURLToPath(new URL(`../hybrid-states/${screen}-${width}-${theme}-details.png`, root)), fullPage: false, animations: 'disabled' })
      }
      evidence.push({ screen, width, theme, geometry, errors })
      await page.close()
    }
    await context.close()
  }
} finally { await browser.close() }
await writeFile(new URL(`../../docs/frontend/hybrid-${phase}-metrics.json`, import.meta.url), JSON.stringify(evidence, null, 2) + '\n')
console.log(`${phase}: ${evidence.length} real browser screenshots; document overflow cases: ${evidence.filter((item) => item.geometry.document.width > item.width).length}; runtime errors: ${evidence.reduce((count, item) => count + item.errors.length, 0)}`)
