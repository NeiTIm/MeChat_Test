import { readFile, readdir, writeFile } from 'node:fs/promises'

const result = JSON.parse(await readFile(new URL('../test-results/design-results.json', import.meta.url), 'utf8'))
const cases = []
function collect(suites) {
  for (const suite of suites) {
    for (const spec of suite.specs || []) for (const test of spec.tests || []) {
      const last = test.results.at(-1)
      cases.push({ name: spec.title, file: spec.file, status: last?.status, durationMs: last?.duration, expectedStatus: test.expectedStatus })
    }
    collect(suite.suites || [])
  }
}
collect(result.suites)
const counts = { tests: cases.length, passed: cases.filter((item) => item.status === 'passed').length, failed: cases.filter((item) => item.status === 'failed' || item.status === 'timedOut').length, skipped: cases.filter((item) => item.status === 'skipped').length }
const screenshotCounts = {}
for (const name of ['hybrid-before', 'hybrid-after', 'hybrid-states']) screenshotCounts[name] = (await readdir(new URL(`../../docs/frontend/screenshots/${name}/`, import.meta.url))).filter((name) => name.endsWith('.png')).length
const summary = { checkpoint: 'MeChat Hybrid screen review; owner approval still required', browser: 'Playwright bundled Chromium', counts, screenshotCounts, cases }
await writeFile(new URL('../../docs/frontend/hybrid-test-summary.json', import.meta.url), JSON.stringify(summary, null, 2) + '\n')
console.log(JSON.stringify({ counts, screenshotCounts }))
