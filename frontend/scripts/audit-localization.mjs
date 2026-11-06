import { readFile, readdir } from 'node:fs/promises'
import { join } from 'node:path'
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
const traverse = traversePackage.default
const namespaces = {}
for (const file of await readdir('src/i18n/locales/vi')) namespaces[file.replace('.json', '')] = JSON.parse(await readFile(join('src/i18n/locales/vi', file), 'utf8'))
const problems = []
let checked = 0
function exists(key) {
  const [namespace, ...rest] = key.split(':')
  const path = rest.join(':').split('.')
  return typeof path.reduce((value, part) => value?.[part], namespaces[namespace]) === 'string'
}
async function walk(dir) {
  for (const file of await readdir(dir, { withFileTypes: true })) {
    const path = join(dir, file.name)
    if (file.isDirectory()) { if (file.name !== 'locales') await walk(path); continue }
    if (!/\.(js|jsx)$/.test(path)) continue
    const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
    const issue = (node, message) => problems.push(`${path}:${node.node.loc.start.line}: ${message}`)
    traverse(ast, {
      CallExpression(node) {
        if (!['t', 'translate'].includes(node.node.callee.name)) return
        const key = node.node.arguments[0]
        if (key?.type === 'StringLiteral' && key.value.includes(':')) { checked += 1; if (!exists(key.value)) issue(node, `Missing translation ${key.value}`) }
      },
      JSXText(node) {
        const value = node.node.value.replace(/\s+/g, ' ').trim()
        if (/[A-Za-z]{2}/.test(value) && !['MeChat', 'mechat', 'MECHAT', 'MECHAT / 2026'].includes(value)) issue(node, 'Visible copy must come from a locale resource')
      },
      JSXAttribute(node) {
        if (!['aria-label', 'title', 'placeholder', 'description'].includes(node.node.name.name)) return
        const value = node.node.value
        if (value?.type === 'StringLiteral' && /[A-Za-z]{2}/.test(value.value) && value.value !== 'MeChat') issue(node, 'Accessible/visible attribute must use a locale resource')
      },
    })
  }
}
await walk('src')
if (problems.length) { console.error(problems.join('\n')); process.exitCode = 1 }
else console.log(`PASS: ${checked} static translation calls resolved; no hardcoded visible JSX copy. Dynamic enum/formatting paths are covered separately in tests.`)
