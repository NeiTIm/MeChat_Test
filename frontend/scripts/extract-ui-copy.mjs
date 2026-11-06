import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join } from 'node:path'
const traverse = traversePackage.default
const copy = new Map()
async function walk(directory) {
  for (const file of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, file.name)
    if (file.isDirectory()) { if (file.name !== 'i18n') await walk(path); continue }
    if (!/\.(jsx|js)$/.test(path)) continue
    const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
    traverse(ast, {
      JSXText(node) { const value = node.node.value.replace(/\s+/g, ' ').trim(); if (/[A-Za-z]{2}/.test(value)) record(value, path) },
      StringLiteral(node) { const value = node.node.value; if (/[a-z]{2}\s+[a-z]/i.test(value) && !/[./]src|className|\bflex\b|\bpadding\b/.test(value) && !node.parentPath.isImportDeclaration()) record(value, path) },
      TemplateLiteral(node) { const value = node.node.quasis.map((item) => item.value.cooked).join('{{value}}'); if (/[a-z]{2}\s+[a-z]/i.test(value)) record(value, path) },
    })
  }
}
function record(value, path) { if (!copy.has(value)) copy.set(value, []); if (!copy.get(value).includes(path)) copy.get(value).push(path) }
await walk('src')
await writeFile('test-results/ui-copy-source.json', JSON.stringify(Object.fromEntries(copy), null, 2))
console.log([...copy.keys()].join('\n'))
