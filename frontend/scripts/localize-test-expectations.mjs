// Migrate existing UI selectors to the approved Vietnamese copy; technical values stay intact.
import { parse } from '@babel/parser'
import traversePackage from '@babel/traverse'
import generatorPackage from '@babel/generator'
import { readFile, writeFile, readdir } from 'node:fs/promises'
import { join } from 'node:path'
import copy from './vi-copy-map.mjs'
const traverse = traversePackage.default
const generate = generatorPackage.default
const fixtures = { 'Jamie Chen': 'Gia Huy', 'The Creative Corner': 'Góc sáng tạo', 'Welcome back.': 'Chào mừng trở lại.', 'Messages.': 'Tin nhắn.', 'Groups.': 'Nhóm.', 'Switch to dark mode': 'Chuyển sang giao diện tối', 'Switch to light mode': 'Chuyển sang giao diện sáng', 'Filter messages': 'Lọc tin nhắn', 'Filter groups': 'Lọc nhóm', 'Back to messages': 'Quay lại tin nhắn', 'Back to groups': 'Quay lại nhóm', 'Message from Jamie Chen': 'Tin nhắn từ Gia Huy', '1 results': '1 kết quả', 'Sample message 6 options': 'Tùy chọn tin nhắn mẫu số 6', 'New messages · jump to latest': 'Có tin nhắn mới · xem mới nhất', 'bookshop': 'tiệm sách', 'Jamie': 'Gia Huy', 'Coffee and books? Count me in. Saturday at 10?': 'Cà phê và sách thì mình đi ngay. Thứ Bảy lúc 10 giờ nhé?', 'A new sample message arrived. Your reading position should stay where you left it.': 'Có tin nhắn minh họa mới. Vị trí bạn đang đọc vẫn được giữ nguyên.', 'credentials': 'thông tin đăng nhập', 'timed out': 'quá thời gian chờ', 'reach MeChat': 'kết nối đến MeChat', 'Your draft is kept': 'Bản nháp vẫn được giữ', 'Design preview': 'Minh họa', 'the owner': 'Chủ nhóm' }
async function walk(dir) {
  for (const file of await readdir(dir, { withFileTypes: true })) {
    const path = join(dir, file.name)
    if (file.isDirectory()) { await walk(path); continue }
    if (!/\.(js|jsx)$/.test(path)) continue
    const ast = parse(await readFile(path, 'utf8'), { sourceType: 'module', plugins: ['jsx'] })
    traverse(ast, {
      StringLiteral(node) {
        if (node.parentPath.isImportDeclaration()) return
        const value = node.node.value
        if (['group', 'conversation'].includes(value)) return
        if (['Member', 'Owner', 'Admin'].includes(value)) {
          if (node.parentPath.isCallExpression() && node.parentPath.node.callee.property?.name === 'selectOption' || node.parentPath.isObjectProperty() && node.parentPath.node.key.name === 'role') return
        }
        const translated = fixtures[value] || copy[value]
        if (translated) node.node.value = translated
      },
      RegExpLiteral(node) {
        const replacements = { 'Signing you in': 'Đang đăng nhập', 'No sign-in request was sent': 'Không có yêu cầu đăng nhập thật nào được gửi', 'couldn’t reach MeChat': 'Không thể kết nối đến MeChat', 'Pinned message': 'Tin nhắn đã ghim' }
        if (replacements[node.node.pattern]) node.node.pattern = replacements[node.node.pattern]
      },
    })
    let source = generate(ast, { jsescOption: { minimal: true } }).code
    source = source.replaceAll('../docs/frontend/screenshots/hybrid-states', '../docs/frontend/screenshots/vi/states').replaceAll('../docs/frontend/screenshots/hybrid-after', '../docs/frontend/screenshots/vi/samples')
    source = source.replaceAll('../docs/frontend/screenshots/${sample}', '../docs/frontend/screenshots/vi/samples/${sample}')
    source = source.replaceAll('Your messages will send', 'Tin nhắn sẽ gửi')
    await writeFile(path, source + '\n')
  }
}
await walk('tests')
