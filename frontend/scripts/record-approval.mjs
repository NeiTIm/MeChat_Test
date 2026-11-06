// One-time record of the explicit Product Owner decision, preserving review history.
import { readFile, writeFile } from 'node:fs/promises'
const note = '# Phê duyệt Visual Baseline và Vietnamese-first — 06/11/2026\n\n**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. Product Owner đã duyệt phương án C — MeChat Hybrid và cả ba mẫu Login, Private Chat, Group Chat. Các màn hình tiếp theo dùng cùng design system; không hỏi duyệt lại cùng hướng thiết kế. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.\n\nNgôn ngữ bắt buộc: toàn bộ User/Admin UI dùng tiếng Việt, i18next vi-VN/fallback vi, HTML lang=vi, Intl Asia/Ho_Chi_Minh. Không dịch contracts hoặc nội dung người dùng. Xem [implementation-contract.md](implementation-contract.md). Các trạng thái chờ duyệt/ảnh tiếng Anh bên dưới là **lịch sử trước quyết định này**; gate hiện đã được mở. Việt hóa không mở lại Design Gate.\n\n---\n\n'
for (const name of ['ui-ux-spec.md', 'ui-ux-acceptance.md', 'design-review.md']) {
  const path = new URL(`../../docs/frontend/${name}`, import.meta.url)
  const content = await readFile(path, 'utf8')
  if (!content.startsWith('# Phê duyệt Visual Baseline')) await writeFile(path, note + content)
}
const path = new URL('../../roadmapFE.md', import.meta.url)
let roadmap = await readFile(path, 'utf8')
roadmap = roadmap.replace('Design gate blocks remaining screens.', 'Design gate is unlocked; Vietnamese-first is mandatory.')
roadmap = roadmap.replace('No unresolved failing review check. No approval received.', 'No unresolved failing review check. Owner subsequently approved the screens on 2026-11-06.')
roadmap = roadmap.replace('Official direction confirmed, screen approval still BLOCKED.', 'Direction and screen baselines APPROVED on 2026-11-06; localization and full feature acceptance in progress.')
roadmap = roadmap.replace('Preserve design gate and NEEDS_VERIFICATION until evidence exists.', 'Design gate is APPROVED; preserve NEEDS_VERIFICATION until actual evidence exists. Every new task requires Vietnamese localization checks.')
await writeFile(path, roadmap)
