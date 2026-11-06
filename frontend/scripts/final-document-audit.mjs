import { readFile, writeFile, access } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
const files = ['../roadmapFE.md', '../README.md', '../docs/frontend/implementation-contract.md', '../docs/frontend/ui-ux-spec.md', '../docs/frontend/ui-ux-acceptance.md', '../docs/frontend/design-review.md', '../docs/frontend/verification-report.md', '../docs/frontend/final-acceptance.md', '../docs/frontend/screenshot-index.md']
const changes = {
  '../roadmapFE.md': [['33 PASS**, 8 files', '33 PASS**, 9 files']],
  '../docs/frontend/ui-ux-acceptance.md': [['32 unit/component', '33 unit/component']],
}
for (const file of files) {
  let content = await readFile(file, 'utf8')
  for (const [from, to] of changes[file] || []) content = content.replaceAll(from, to)
  content = content.replace(/\nFinal additional evidence:[^\n]*\n/g, '\n')
  if ((file.endsWith('verification-report.md') || file.endsWith('final-acceptance.md')) && !content.includes('| Message/media sau sửa cache |')) content = content.replace('| Final copy/partial-preview regression | 2 PASS |', '| Final copy/partial-preview regression | 2 PASS |\n| Message/media sau sửa cache | 29 PASS, targeted rerun |\n| Safe live-input helper | 1 PASS, client simulation |')
  if (file.endsWith('verification-report.md') || file.endsWith('final-acceptance.md') || file.endsWith('roadmapFE.md')) content += '\nBằng chứng bổ sung cuối: secure-auth-input-client.json **1 PASS** với dữ liệu giả lập; actual account vẫn SKIPPED. final-message-media-regression.json **29 PASS** sau khi giới hạn sanitization vào message pages, giữ nguyên Blob và xóa reply/detail/history phụ thuộc. Các lần chạy tập trung trùng một phần với full128; không cộng thành số ca duy nhất. Unit cuối **33 PASS trong 9 files**.\n'
  await writeFile(file, content)
}
const broken = []
for (const file of files) {
  const content = await readFile(file, 'utf8')
  for (const match of content.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const target = match[1].replace(/^<|>$/g, '').split('#')[0]
    if (!target || /^(https?:|mailto:)/.test(target)) continue
    try { await access(resolve(dirname(file), target)) } catch { broken.push(`${file} -> ${target}`) }
  }
}
if (broken.length) { console.error(broken.join('\n')); process.exitCode = 1 } else console.log('PASS: requested/current document links resolve, final verification counts recorded.')
