import { readFile, writeFile } from 'node:fs/promises'
const name = process.argv[2] || 'client-regression'
if (!/^[a-z0-9-]+$/.test(name)) throw new Error('Invalid report name')
const result = JSON.parse(await readFile('test-results/design-results.json', 'utf8'))
const cases = []
function collect(suites, parent = '') { for (const suite of suites) { for (const spec of suite.specs || []) for (const test of spec.tests || []) cases.push({ title: `${parent} ${spec.title}`.trim(), status: test.status, results: test.results.map((run) => ({ status: run.status, duration: run.duration, errors: run.errors.map((error) => error.message) })) }); collect(suite.suites || [], `${parent} ${suite.title}`.trim()) } }
collect(result.suites)
await writeFile(`../docs/frontend/${name}.json`, JSON.stringify({ scope: 'Chromium frontend/design and explicitly labelled HTTP client simulations; not live authenticated business acceptance.', capturedAt: new Date().toISOString(), stats: result.stats, cases }, null, 2) + '\n')
console.log(`${name}: ${result.stats.expected} passed; ${result.stats.unexpected} failed; ${result.stats.skipped} skipped`)
