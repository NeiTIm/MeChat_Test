import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import generatorPackage from '@babel/generator'
import { readFile, writeFile, readdir } from 'node:fs/promises'
import { join } from 'node:path'
const traverse = traversePackage.default
const generate = generatorPackage.default
async function walk(dir) {
  for (const item of await readdir(dir, { withFileTypes: true })) {
    const path = join(dir, item.name)
    if (item.isDirectory()) { await walk(path); continue }
    if (!/\.(js|jsx)$/.test(path)) continue
    const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
    traverse(ast, { StringLiteral(node) { if (node.node.value === 'nhóm') node.node.value = 'group' } })
    await writeFile(path, generate(ast, { jsescOption: { minimal: true } }).code + '\n')
  }
}
await walk('tests')
