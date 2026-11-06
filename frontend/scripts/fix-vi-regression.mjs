import { readFile, writeFile } from 'node:fs/promises'
const patch = async (path, pairs) => {
  let text = await readFile(path, 'utf8')
  for (const [from, to] of pairs) text = text.replaceAll(from, to)
  await writeFile(path, text)
}
await patch('src/design/LoginPreviewTools.jsx', [['placeholder="you@example.com"', 'placeholder={t("auth:emailPlaceholder")}']])
await patch('src/components/chat/ChatWorkspace.jsx', [['{visibleMessages.length}{t("chat:results")}', '{t("chat:resultCount", { count: visibleMessages.length })}']])
for (const file of ['design.spec.js', 'hybrid-interactions.spec.js']) await patch(`tests/e2e/${file}`, [
  ['This content is unavailable, or your access has changed.', 'Nội dung không khả dụng hoặc quyền truy cập của bạn đã thay đổi.'],
  ['No email was sent.', 'không có email nào được gửi.'], ['Replying', 'Đang trả lời'],
  ['Coffee and books', 'Cà phê và sách'], ['No real message will change', 'không thay đổi tin nhắn thật'],
  ['not a channel', 'không phải kênh'], ['12 characters', '12 ký tự'], ['You’re offline', 'Bạn đang ngoại tuyến'],
])
for (const [namespace, entries] of Object.entries({ auth: { emailPlaceholder: 'ban@example.com' }, chat: { resultCount: '{{count}} kết quả' } })) {
  const path = `src/i18n/locales/vi/${namespace}.json`
  const data = JSON.parse(await readFile(path, 'utf8'))
  await writeFile(path, JSON.stringify({ ...data, ...entries }, null, 2) + '\n')
}
