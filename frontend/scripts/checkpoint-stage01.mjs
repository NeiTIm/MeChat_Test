import { readFile, writeFile } from 'node:fs/promises'
const path = '../roadmapFE.md'
const text = await readFile(path, 'utf8')
await writeFile(path, text + `
## Checkpoint triển khai tiếng Việt — 06/11/2026

- Product Owner đã duyệt phương án C và ba mẫu; gate giữ APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER. Không yêu cầu duyệt lại cùng hướng.
- Người dùng chọn tiếp tục và để đăng nhập thật ở NEEDS_VERIFICATION khi chưa có credentials trong môi trường. Không lấy mật khẩu/token từ chat hoặc private uploads/dev-mail. Tiếp tục các công việc độc lập.
- FE-017: đã cài i18next/react-i18next, vi-VN fallback vi, HTML vi, namespaces common/auth/users/friends/chat/groups/design/errors, Intl Asia/Ho_Chi_Minh. Đã chuyển bản mẫu và UI thật sang tiếng Việt; chưa thay đổi contracts hoặc nội dung tin nhắn thật.
- FE-007: đã thêm đăng ký/xác minh/gửi lại/quên/đặt lại/kích hoạt lại, đổi mật khẩu/vô hiệu hóa, profile/username/email/privacy/sessions. Tất cả consumer gọi API thật. CSRF đọc lại sau refresh; chặn phát lại mutation khi identity đổi. Chưa nghiệm thu Auth thành công bằng SQL/cookie thật.
- FE-008: đã thêm Friends list/search cursor/requests accept-reject-cancel/unfriend/block-unblock/profile/open chat. Đang QA client, toàn bộ luồng thật NEEDS_VERIFICATION. SignalR FriendshipChanged và PermissionsChanged đã làm mới cache chung; hai người dùng thật chưa kiểm thử.
- Bằng chứng hiện tại: 25 unit/component PASS; 4 browser mẫu tiếng Việt PASS; 6 browser Auth client simulation PASS (DTO/password whitespace, profile/privacy/revoke confirmation, 320/1440 light/dark + axe gồm contrast). Audit 365 static translation calls PASS; lint PASS. Đây là kiểm thử frontend qua HTTP simulation có nhãn, không phải PASS backend. Bộ Friends lần đầu phát hiện aria-label thiếu username, đang sửa và chạy lại. Chưa chạy full regression sau mọi thay đổi.
- Files: frontend/src/i18n/**, features/auth/**, features/users/**, features/friends/**, components/forms/layout/ConfirmAction, app/router/providers, lib/http/errors; resources/tests/scripts/package+lock; docs implementation contract/spec/acceptance/review. Backend/DB không sửa; user deletions giữ nguyên.
- Next Exact Action: hoàn tất Friends + toàn bộ regression tiếng Việt, cập nhật coverage adapter; triển khai message actions/context/receipts/requests + realtime typing/presence theo source contracts, rồi Groups/Media/Notifications/Reports/Admin và QA cuối. Chưa PORTFOLIO_READY.
`)
