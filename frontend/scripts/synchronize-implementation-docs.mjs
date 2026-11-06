import { readFile, writeFile, mkdir } from 'node:fs/promises'
const docs = '../docs/frontend'
await mkdir(`${docs}/history`, { recursive: true })
for (const name of ['ui-ux-spec', 'ui-ux-acceptance', 'design-review', 'final-acceptance', 'verification-report']) {
  const original = await readFile(`${docs}/${name}.md`, 'utf8')
  try { await writeFile(`${docs}/history/${name}-design-checkpoint.md`, original.replaceAll('(screenshots/', '(../screenshots/'), { flag: 'wx' }) } catch (error) { if (error.code !== 'EEXIST') throw error }
}
const approval = `**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER** — 2026-11-06. Product Owner phê duyệt phương án C — MeChat Hybrid và ba mẫu Login, Private Chat, Group Chat làm Visual Baseline. Không xin duyệt lại cùng hướng; màn hình mới dùng chung design system. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.\n`
await writeFile(`${docs}/ui-ux-spec.md`, `# Đặc tả UI/UX MeChat hiện hành

${approval}
## Design system

MeChat Hybrid = Modern Messenger + Minimalist SaaS. Identity/logo riêng; Indigo #4F46E5, Violet #7C3AED, Inter variable cục bộ, Light mặc định và Dark hoàn chỉnh. Dùng tokens trong frontend/src/styles/index.css; cặp màu nút destructive tách khỏi màu chữ lỗi để đạt tương phản trên Dark. Button/Input/Dialog, focus ring, spacing 4px, surfaces/radii và chuyển động dùng chung.

Desktop có rail, danh sách, thread và details tùy chọn; ≥1280px details inline, nhỏ hơn dùng modal. Tablet 768–1023px hiển thị danh sách hoặc thread. Mobile có back và bottom navigation; đăng xuất ở công cụ đầu trang. Không có Channel UI, call/video giả hoặc chức năng Backend chưa hỗ trợ.

## Vietnamese-first bắt buộc

i18next/react-i18next: vi-VN, fallback vi, HTML lang=vi. Namespaces common/auth/users/friends/chat/messages/groups/files/notifications/reports/appeals/admin/errors; resources bổ sung locale khác được trong tương lai. Mọi giao diện User/Admin, validation/error/feedback/tooltip/aria dùng tiếng Việt tự nhiên. Intl ngày/giờ/số dùng vi-VN, Asia/Ho_Chi_Minh; dữ liệu truyền API giữ nguyên contract.

Không dịch routes, property/DTO/enums/event names/IDs; enum hiển thị qua mapping. Nội dung người dùng, tin nhắn, lý do báo cáo, ghi chú quản trị và bằng chứng giữ nguyên. Không hiện raw server exception/stack trace. Kiểm tra thiếu resource, hardcoded JSX, dấu tiếng Việt, text expansion, contrast và 320px trong mọi task.

## Luồng đã có consumer thật

- Auth: đăng nhập/đăng ký/xác minh/gửi lại/quên/đặt lại/kích hoạt lại; profile, username cooldown, đổi email/xác nhận, privacy, mật khẩu, vô hiệu hóa và sessions. Refresh cookie/CSRF, memory JWT, cache isolation, broadcast logout. Các mã email do người dùng nhập, không đọc private uploads/dev-mail.
- Friends: tìm kiếm cursor, hồ sơ, lời mời đến/đi, accept/reject/cancel, unfriend/block/unblock, mở trò chuyện. Reverse request theo phản hồi Backend.
- Messaging: history cursor, send/reply/edit ≤15 phút, recall/delete-for-me, forward private/group, context/reactions/history, server search và local filter. Delivered/Seen lấy từ API; chỉ gửi read cursor khi bubble thực sự nhìn thấy trong thread foreground có focus. Requests dùng con trỏ đọc riêng và composer chỉ đọc trước khi chấp nhận.
- Groups: create/profile/avatar/members/roles/invitations/links/pins/leave/transfer/dissolve/hide. Member có thể sửa profile và ghim theo service; không tự giới hạn thành Admin. Owner không rời trước khi chuyển quyền. Resolve link trước khi xác nhận join; token chỉ lưu trong memory và gỡ fragment khỏi URL.
- Files: multipart purpose Message/Avatar, progress/retry/cancel, tối đa 5 tệp; image 10 MiB, file 25 MiB, avatar 5 MiB. Backend tiếp tục kiểm tra signature/ownership. Preview/download qua request có JWT, blob URL thu hồi khi cleanup/mất dữ liệu quyền; ảnh/TXT/PDF và metadata cho định dạng khác.
- Notifications: cursor timestamp + ID, count/read/read-all, mở reference qua API có quyền; mute/unmute cuộc trò chuyện.
- Reports/Appeals: context User/Group/Message, theo dõi own reports; appeal JWT/transport/cache riêng, không đưa token appeal vào normal auth, clear khi kết thúc/hết hạn. Chấp nhận appeal không tự gỡ đình chỉ.
- Admin: DB SystemRole guard, dashboard/users/groups/reports claim + evidence + resolve, appeals/audit/retention preview + explicit dry-run + hold. Mutations yêu cầu lý do và bước xác nhận; giao diện không có chạy purge/delete dữ liệu.

## Trạng thái và tương tác

Mỗi query có loading/empty/error/retry, mutations có pending/error/success, form có validation và labels. Không tạo thành công giả. Composer readOnly khi gửi để giữ keyboard/focus; giữ payload idempotency sau lỗi, reply không xóa draft đang nhập. Animation entry/panel hữu hạn và hỗ trợ prefers-reduced-motion. Không tự cuộn người đang đọc lên đầu khi prepend/incoming.

## Kiểm tra và giới hạn

Chromium + axe + screenshots ở 320/390/768/1366/1440 × Light/Dark; kiểm tra bàn phím, modal, overflow, focus, scroll, lỗi runtime, states. Bộ hồi quy cuối đang chạy; xem verification-report.md và roadmapFE.md. Mock chỉ ở DEV review hoặc test có nhãn; production consumers gọi Backend thật.

Successful authenticated SQL, 2-user SignalR, thiết bị mobile/IME, native screen reader/Chrome/Edge, full WCAG và performance vẫn NEEDS_VERIFICATION. [Đặc tả checkpoint thiết kế cũ](history/ui-ux-spec-design-checkpoint.md).
`)
await writeFile(`${docs}/ui-ux-acceptance.md`, `# Nghiệm thu UI/UX và chức năng

${approval}
## Điều kiện bắt buộc cho mọi task

- Giữ MeChat Hybrid, tokens chung, Inter, desktop/mobile, Light/Dark và reduced-motion.
- Toàn bộ copy/aria/feedback bằng tiếng Việt; không dịch contracts hoặc nội dung người dùng.
- Loading/empty/error/retry/success/pending/validation thật; không có nút giả hoặc dữ liệu giả trong production.
- Không horizontal overflow/overlap/clipping; keyboard/focus/modal/composer/scroll dùng được; không runtime error nghiêm trọng.
- API/JWT/cookie/CSRF/role/pagination/SignalR/upload đúng source; UI không thay thế authorization Backend.
- Build/lint/unit/component/browser/localization checks có bằng chứng. Không PASS runtime khi chưa chạy Backend thật.

## Bằng chứng và trạng thái

| Phạm vi | Trạng thái hiện tại |
| --- | --- |
| Visual Direction + 3 mẫu | APPROVED bởi Product Owner, 06/11/2026 |
| Recovery build/lint + 31 unit/component | PASS; hồi quy cuối được ghi riêng trong verification-report |
| Groups client | 13/13 PASS chạy lại khi phục hồi |
| Private client | 13/13 PASS trước bổ sung audit; hồi quy mới đang chạy |
| Files/Avatar/Notifications client | 13/13 PASS sau sửa blob/aria |
| Admin/moderation client | source + matrix; case confirmation đã sửa timing, đang chạy lại full |
| API source mapping | 122 controller consumers + 1 health, không phải API runtime PASS |
| Backend boundaries | 4 PASS thật, successful auth 1 SKIPPED theo lựa chọn người dùng |
| Production build/isolation/smoke | PASS sau loại DEV navigation strings; build chia chunks |
| Authenticated business / 2-user / real devices | NEEDS_VERIFICATION |

Frontend chưa PORTFOLIO_READY khi chưa đủ business evidence. Không yêu cầu Product Owner duyệt lại cùng Visual Direction. [Điểm tự đánh giá và lịch sử](design-review.md), [checkpoint cũ](history/ui-ux-acceptance-design-checkpoint.md).
`)
const originalReview = await readFile(`${docs}/history/design-review-design-checkpoint.md`, 'utf8')
const scorecards = originalReview.slice(originalReview.indexOf('## Scorecard'), originalReview.indexOf('## Vấn đề phát hiện')).replaceAll('(../screenshots/', '(screenshots/')
await writeFile(`${docs}/design-review.md`, `# MeChat Hybrid — Design Review và triển khai tiếp

${approval}
## Bằng chứng hiện hành

Đã Việt hóa ba mẫu và mở rộng Auth/account/Friends/chat/Groups/Files/Notifications/moderation/Admin theo cùng baseline. Ảnh Chromium thật: [Login](screenshots/vi/samples/login-390-light.png), [Private](screenshots/vi/integration/private-390-dark.png), [Groups](screenshots/vi/groups/manage-390-light.png), [Admin desktop](screenshots/vi/admin/dashboard-1440-dark.png), [Notifications](screenshots/vi/notifications/list-390-light.png). Ảnh client integration dùng HTTP simulation có nhãn trong report; không phải tài khoản Backend thật. Xem screenshot-index.md.

Vấn đề đã xử lý: bản dịch thiếu/English copy; label logo/profile; tương phản nút destructive trên Dark; blob URL null gây runtime; trạng thái chưa đọc thiếu role; headings mobile; nested-dialog test phải chờ exit/pending; retry idempotency phân biệt reply/attachment; đăng xuất và nhãn navigation mobile. Build split vendor tránh entry >500 kB; DEV routes/fixtures bị loại khỏi production. Bộ hồi quy cuối đang chạy, không dùng kết quả lịch sử để che lỗi mới.

## Scorecards của Visual Baseline đã được duyệt

Giữ nguyên điểm tự đánh giá đã nộp: Login 91, Private 89, Group 88. Đây là đánh giá UI mẫu trước phê duyệt, không phải chứng nhận hoặc điểm nghiệm thu full features. Nhận xét “chưa tích hợp/chưa duyệt” trong các bảng sau mô tả thời điểm checkpoint đó; trạng thái hiện tại ở phần đầu và roadmap. Không tự tăng điểm vì thêm source.

${scorecards}
## Hạn chế và đề xuất

Visual Baseline đã được duyệt; không cần mở lại gate. Successful auth/business/two-user/admin/file authorization thật, native browser/mobile keyboard/screen reader và full performance còn NEEDS_VERIFICATION. Có thể xem các luồng client; chưa nghiệm thu portfolio toàn bộ. [Bản review checkpoint đầy đủ](history/design-review-design-checkpoint.md).
`)
let readme = await readFile('../README.md', 'utf8')
readme = readme.replace('Complete Auth, friends, message actions, requests, groups management, files, notifications, settings, moderation and Admin slices remain pending design approval and integration verification.', 'Auth/account, Friends, private messages/requests, Groups, Files/Avatar, Notifications, Reports/Appeals and Admin now have real API consumers. Their successful business integration remains NEEDS_VERIFICATION; HTTP client simulations and design approval do not accept Backend journeys.')
await writeFile('../README.md', readme)
console.log('Synchronized current UI specification/acceptance/review; preserved historical documents.')
