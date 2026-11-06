// One-time syntax-aware migration. Never apply a text replace to API/enum values.
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import generatorPackage from '@babel/generator'
import * as types from '@babel/types'
import { readFile, writeFile, readdir } from 'node:fs/promises'
import { join } from 'node:path'
import copy from './vi-copy-map.mjs'
const traverse = traversePackage.default
const generate = generatorPackage.default
const resources = {}
const skipped = new Set(['Member', 'Admin', 'Owner', 'group', 'conversation'])
const keyFor = (value) => value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
async function walk(directory) {
  for (const file of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, file.name)
    if (file.isDirectory()) { await walk(path); continue }
    if (!file.name.endsWith('.jsx')) continue
    const ns = path.includes('Login') ? 'auth' : path.includes('GroupPreview') ? 'groups' : path.includes('design') ? 'design' : path.includes('chat') ? 'chat' : 'common'
    const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
    const components = new Set()
    let staticUsed = false
    function translated(node, value, args) {
      const translatedValue = copy[value]
      if (!translatedValue) return null
      const key = keyFor(value)
      resources[ns] ||= {}; resources[ns][key] = translatedValue
      let ancestor = node
      while (ancestor && !(ancestor.isFunctionDeclaration() && /^[A-Z]/.test(ancestor.node.id?.name || ''))) ancestor = ancestor.parentPath
      if (ancestor) components.add(ancestor.node)
      else staticUsed = true
      return types.callExpression(types.identifier(ancestor ? 't' : 'translate'), [types.stringLiteral(`${ns}:${key}`), ...(args?.length ? [types.objectExpression(args.map((argument, index) => types.objectProperty(types.identifier(`value${index}`), argument)))] : [])])
    }
    traverse(ast, {
      JSXText(node) {
        const value = node.node.value.replace(/\s+/g, ' ').trim()
        const call = translated(node, value)
        if (call) node.replaceWith(types.jsxExpressionContainer(call))
      },
      StringLiteral(node) {
        if (node.parentPath.isImportDeclaration() || node.parentPath.isExportNamedDeclaration() || node.parentPath.isCallExpression() && ['t', 'translate'].includes(node.parentPath.node.callee.name)) return
        if (node.parentPath.isJSXAttribute()) {
          if (!['aria-label', 'title', 'placeholder', 'description'].includes(node.parentPath.node.name.name)) return
        } else {
          if (skipped.has(node.node.value) || node.parentPath.isBinaryExpression() || node.parentPath.isObjectProperty() && ['role', 'status', 'type'].includes(node.parentPath.node.key.name)) return
          if (node.parentPath.isObjectProperty() && node.key === 'key') return
        }
        const call = translated(node, node.node.value)
        if (call) node.replaceWith(node.parentPath.isJSXAttribute() ? types.jsxExpressionContainer(call) : call)
      },
      TemplateLiteral(node) {
        const value = node.node.quasis.map((item, index) => item.value.cooked + (index < node.node.expressions.length ? `{{value${index}}}` : '')).join('')
        const call = translated(node, value, node.node.expressions)
        if (call) node.replaceWith(call)
      },
    })
    if (!components.size && !staticUsed) continue
    for (const component of components) if (!component.body.body.some((statement) => types.isVariableDeclaration(statement) && statement.declarations.some((declaration) => types.isObjectPattern(declaration.id) && declaration.id.properties.some((property) => property.key?.name === 't')))) component.body.body.unshift(types.variableDeclaration('const', [types.variableDeclarator(types.objectPattern([types.objectProperty(types.identifier('t'), types.identifier('t'), false, true)]), types.callExpression(types.identifier('useTranslation'), []))]))
    if (components.size && !ast.program.body.some((node) => types.isImportDeclaration(node) && node.specifiers.some((specifier) => specifier.local.name === 'useTranslation'))) ast.program.body.unshift(types.importDeclaration([types.importSpecifier(types.identifier('useTranslation'), types.identifier('useTranslation'))], types.stringLiteral('react-i18next')))
    if (staticUsed && !ast.program.body.some((node) => types.isImportDeclaration(node) && node.specifiers.some((specifier) => specifier.local.name === 'translate'))) ast.program.body.unshift(types.importDeclaration([types.importSpecifier(types.identifier('translate'), types.identifier('translate'))], types.stringLiteral('@/i18n')))
    await writeFile(path, generate(ast, { jsescOption: { minimal: true } }).code + '\n')
  }
}
await walk('src/app'); await walk('src/components'); await walk('src/design'); await walk('src/features')
for (const [ns, values] of Object.entries(resources)) {
  const path = `src/i18n/locales/vi/${ns}.json`
  const existing = JSON.parse(await readFile(path, 'utf8'))
  await writeFile(path, JSON.stringify({ ...existing, ...values }, null, 2) + '\n')
}
console.log('Migrated existing JSX copy. Audit dynamic text, enum labels and fixtures separately.')
