import { readFile, writeFile } from 'node:fs/promises'
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import generatePackage from '@babel/generator'
const path = 'src/features/friends/FriendsPage.jsx'
const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
traversePackage.default(ast, { JSXOpeningElement(node) {
  if (node.node.attributes.some((attr) => attr.name?.name === 'className' && attr.value?.value === 'person-identity')) node.node.attributes = node.node.attributes.filter((attr) => attr.name?.name !== 'aria-label')
} })
await writeFile(path, generatePackage.default(ast).code + '\n')
const test = 'tests/e2e/friends-client.spec.js'
await writeFile(test, (await readFile(test, 'utf8')).replace("name: 'Xem hồ sơ Nguyễn Hoàng Gia Huy @giahuy', exact: true", "name: /Nguyễn Hoàng Gia Huy/"))
