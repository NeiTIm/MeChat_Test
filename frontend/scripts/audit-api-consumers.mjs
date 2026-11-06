import { readFile, writeFile } from 'node:fs/promises'
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
const inventory = JSON.parse(await readFile('../docs/frontend/api-inventory.json', 'utf8'))
const cache = new Map()
const errors = []
for (const row of inventory) {
  if (row.controller === 'Program') continue
  try {
    let definitions = cache.get(row.module)
    if (!definitions) {
      definitions = new Set()
      const ast = parse(await readFile(`../${row.module}`, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
      traversePackage.default(ast, { VariableDeclarator(path) {
        if (path.node.init?.type === 'ObjectExpression') for (const property of path.node.init.properties) definitions.add(`${path.node.id.name}.${property.key?.name}`)
      }, FunctionDeclaration(path) { definitions.add(path.node.id?.name) } })
      cache.set(row.module, definitions)
    }
    if (!definitions.has(row.api_function)) errors.push(`${row.id}: missing ${row.api_function} in ${row.module}`)
    if (!row.hook_or_consumer || row.hook_or_consumer.startsWith('planned')) errors.push(`${row.id}: consumer not mapped`)
  } catch (error) { errors.push(`${row.id}: ${error.message}`) }
}
const report = { date: '2026-11-06', scope: 'Static module/function/consumer mapping only. Does not establish live backend business acceptance.', controllerActions: inventory.length - 1, mappedFunctions: inventory.filter((row) => row.controller !== 'Program' && !errors.some((error) => error.startsWith(row.id))).length, errors }
await writeFile('../docs/frontend/api-consumer-audit.json', JSON.stringify(report, null, 2) + '\n')
if (errors.length) { console.error(errors.join('\n')); process.exitCode = 1 } else console.log(`PASS: ${report.mappedFunctions} controller actions map to existing JavaScript functions and named consumers; live verification stays NEEDS_VERIFICATION.`)
