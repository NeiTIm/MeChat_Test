import { readdir, writeFile } from 'node:fs/promises'
const root = '../docs/frontend/screenshots/vi'
const sections = []
let count = 0
for (const dir of await readdir(root, { withFileTypes: true })) {
  if (!dir.isDirectory()) continue
  const files = (await readdir(`${root}/${dir.name}`)).filter((file) => file.endsWith('.png')).sort()
  count += files.length
  sections.push(`## ${dir.name} — ${files.length} ảnh\n\n` + files.map((file) => `- [${file}](screenshots/vi/${dir.name}/${file})`).join('\n'))
}
await writeFile('../docs/frontend/screenshot-index.md', `# Screenshots tiếng Việt\n\n${count} ảnh PNG thực tế từ Chromium. Viewports 320/390/768/1366/1440, Light/Dark, states và interactions theo từng suite. Các ảnh client integration sử dụng HTTP simulation có nhãn trong báo cáo/tests; không phải bằng chứng tài khoản Backend thật. Không lưu authenticated screenshots/traces. Xem verification-report.md để biết trạng thái mới nhất; ảnh cũ được ghi đè khi ca tương ứng chạy lại.\n\nẢnh English của baseline phê duyệt vẫn được giữ riêng tại screenshots/hybrid-before, hybrid-after, hybrid-states và được liên kết trong history/design-review-design-checkpoint.md.\n\n${sections.join('\n\n')}\n`)
console.log(`${count} Vietnamese screenshot links written`)
