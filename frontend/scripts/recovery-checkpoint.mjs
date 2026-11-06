import { readFile, writeFile } from 'node:fs/promises'
const path = '../roadmapFE.md'
let text = await readFile(path, 'utf8')
text = text.replace('Phase 05 — Stage 01 Foundation & Authentication. Current tasks: FE-017 localization IN_PROGRESS, then FE-007 full Auth/profile/privacy/session management.', 'Phase 05 — Stage 05 Media/Notifications/Reports/Appeals. FE-017 localization and FE-007–010 clients exist; real business verification remains pending. Current task: recover interrupted Files/Notifications/moderation work, then Admin and final audits.')
for (const id of ['FE-008', 'FE-009', 'FE-010', 'FE-011', 'FE-012']) text = text.replace(new RegExp(`(\\| ${id} \\|[^\\n]*?)\\| TODO \\|`), '$1| IN_PROGRESS |')
text = text.replace('14 minimal live API adapters implemented but NEEDS_VERIFICATION. 1 health diagnostic consumer PASS. Remaining 108 endpoint consumers TODO.', 'The original 14-adapter count is obsolete. Auth/users/friends/private messages/requests/groups/files/notifications adapters now exist; recovery coverage census is in progress. Reports/Appeals consumers were interrupted during routing/localization and are being completed. Successful business API verification remains NEEDS_VERIFICATION. Health PASS below is historical until rerun.')
text = text.replace('Typing UI/invocation, presence UI, friendship/request/notification feature consumers and receipts remain TODO.', 'Join/leave/typing invocations, typing expiry, privacy-aware presence, friendship/request/notification consumers and server receipts now have source. Actual authenticated transport and two-user acceptance remain NEEDS_VERIFICATION.')
text = text.replace('No known failing current checkpoint checks;', 'Interrupted message client suite had a nested-dialog timing failure; the saved correction is awaiting rerun. Files/notifications/moderation regression is incomplete;')
text = text.replace('No >500kB chunk warning after lazy routing/realtime split.', 'Recovery build succeeds but currently warns about a 546.46 kB entry chunk; performance optimization/verification remains pending.')
text = text.replace('Complete FE-017 namespaces, Vietnamese error mapping and Intl formatting; localize all shared/approved sample UI. Complete FE-007 Auth/account/session flows using real contracts and tests, then Friends and the remaining stages.', 'Finish interrupted Files/Attachments/Avatar/Notifications and Reports/Appeals wiring/tests, fix message regression, update exact API census, then implement Admin and final audits.')
text += `
## Recovery audit sau usage limit — 06/11/2026

- Đã đọc roadmap, implementation-contract, spec/acceptance/architecture/backend-gaps; kiểm tra git và source. Không có AGENTS.md áp dụng trong repo/ancestors. Không khôi phục REFACTOR_STATUS.md hoặc STATIC_AUDIT.json; không sửa Backend/DB.
- Xác minh lại độc lập: lint PASS; localization static audit 574 calls PASS; production build PASS (546.46 kB entry warning + third-party PURE comments); 26 unit/component tests PASS. Groups **13/13 Chromium client checks PASS** lần phục hồi, không phải Backend nghiệm thu. Báo cáo: docs/frontend/recovery-groups-client.json.
- Bằng chứng lịch sử có file: vi-foundation-regression.json 72 PASS; vi-groups-client.json 13 PASS. Không coi đây là kết quả chạy lại toàn bộ source mới nhất.
- Auth/account/Friends/private actions/requests/receipts/realtime/presence/Groups có source và client tests. Files/Avatar có consumer đã nối; Notifications có route và consumer nhưng chưa đủ CSS/tests. Reports/Appeals đã có source nhưng chưa nối route/namespace ở điểm ngắt; đang hoàn tất. Admin chưa có source.
- Lỗi dang dở: suite messages-client trước ngắt có 12 PASS/1 FAIL ở Escape của hai dialog liên tiếp; fix chờ exit motion đã lưu, đang chạy lại. Source không bị ghi đè bằng bản cũ.
- Backend 5080 không phản hồi khi recovery probe; đang khởi động bản build hiện có với cả scheduled retention/deletion/purge tắt, không migrate/seed. Chỉ chạy boundary checks an toàn. Người dùng đã chọn hoãn successful login: giữ NEEDS_VERIFICATION, không hỏi credentials lại.
- Next Exact Action: hoàn tất wiring moderation; regression message + files/avatar/notifications; cập nhật API matrix và tài liệu hiện hành, triển khai Admin theo contracts; Final Audit. Tất cả authenticated business flows vẫn NEEDS_VERIFICATION, chưa PORTFOLIO_READY.
`
await writeFile(path, text)
const readme = '../README.md'
let readmeText = await readFile(readme, 'utf8')
readmeText = readmeText.replace('Screen approval remains **BLOCKED: DESIGN_APPROVAL_REQUIRED**.', 'Product Owner approved Option C and all three screen baselines on 2026-11-06: **APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. All User/Admin UI is Vietnamese-first (vi-VN). Visual approval does not accept unverified functionality.')
await writeFile(readme, readmeText)
