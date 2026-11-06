# Đặc tả UI/UX MeChat hiện hành

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER** — 2026-11-06. Product Owner phê duyệt phương án C — MeChat Hybrid và ba mẫu Login, Private Chat, Group Chat làm Visual Baseline. Không xin duyệt lại cùng hướng; màn hình mới dùng chung design system. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.

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

Chromium + axe + screenshots ở 320/390/768/1366/1440 × Light/Dark; kiểm tra bàn phím, modal, overflow, focus, scroll, lỗi runtime, states. Bộ hồi quy cuối đã hoàn tất; xem verification-report.md và roadmapFE.md. Mock chỉ ở DEV review hoặc test có nhãn; production consumers gọi Backend thật.

Successful authenticated SQL, 2-user SignalR, thiết bị mobile/IME, native screen reader/Chrome/Edge, full WCAG và performance vẫn NEEDS_VERIFICATION. [Đặc tả checkpoint thiết kế cũ](history/ui-ux-spec-design-checkpoint.md).

## Kết quả checkpoint cuối — 06/11/2026

Full browser128 PASS; final copy2 PASS; secure input1 PASS; hồi quy lại message/media29 PASS sau sửa cache binary/reply. Không cộng các lần chạy có ca trùng thành một full mới. Unit/component33 PASS; build/lint/localization646/API122/production scan/smoke PASS. Real boundaries4 PASS, successful auth1 SKIPPED; authenticated/two-user/native/device/performance NEEDS_VERIFICATION. [Báo cáo chi tiết](verification-report.md). Không nghiệm thu 100% hoặc mở lại Design Gate.
