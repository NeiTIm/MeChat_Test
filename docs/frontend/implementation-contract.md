# Hợp đồng triển khai frontend

## Quyết định Product Owner — 06/11/2026

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. Phương án C — MeChat Hybrid và ba giao diện Login, Private Chat, Group Chat được duyệt làm Visual Baseline. Không hỏi lại cùng hướng thiết kế; các màn hình sau dùng chung design system. Chỉ thay đổi đáng kể Visual Direction mới cần phê duyệt. Phê duyệt UI không phải nghiệm thu chức năng hoặc xác nhận API đã được kiểm thử.

## Ngôn ngữ bắt buộc

- Toàn bộ User/Admin UI dùng tiếng Việt tự nhiên: nhãn, điều hướng, validation, feedback, modal, accessibility, trạng thái và tooltip.
- i18next + react-i18next; mặc định vi-VN, fallback vi, HTML lang=vi. Resources chia namespace theo feature; không cần nút đổi ngôn ngữ hoặc bản tiếng Anh hoàn chỉnh.
- Intl.DateTimeFormat/NumberFormat; thời gian hiển thị Việt Nam dùng Asia/Ho_Chi_Minh. Không sửa giá trị UTC/JSON truyền API.
- Không dịch routes, DTO/JSON keys, enums, SignalR names, IDs, file names hay nội dung tin nhắn/người dùng nhập. Enum chỉ được ánh xạ sang nhãn hiển thị.
- Backend errors được ánh xạ sang thông báo tiếng Việt an toàn; không hiển thị raw exception/stack trace hoặc nguyên văn thông báo server không kiểm soát.
- Mọi task mới phải kiểm tra tiếng Việt, dấu Inter, text expansion, mobile/desktop, light/dark, keyboard, loading/empty/error/success.

## Quy trình và an toàn

Giữ nguyên 6 Phase chính thức. Bên trong Phase 05 triển khai Stage 01 Foundation/Auth → 02 Friends → 03 Private Chat → 04 Groups → 05 Media/Notifications/Reports/Appeals → 06 Admin; Stage 07 là audit xuyên suốt và Phase 06 tổng thể. Không reset hoặc viết lại module đã hoạt động. Không sửa backend/database/migration hoặc chạy tác vụ destructive khi chưa có phê duyệt riêng. Không commit credentials, tokens, dev-mail, private uploads, authenticated traces.

Mỗi slice có API module, consumer thật, validation/permission/error states, tests, coverage status và roadmap. Mock chỉ trong test hoặc bản thiết kế có nhãn. Chỉ PASS khi có bằng chứng đúng phạm vi; authenticated/two-user/admin/device flows chưa chạy giữ NEEDS_VERIFICATION. Tiếp tục việc độc lập khi thiếu credentials; không lấy dữ liệu riêng tư để tự lấp cấu hình kiểm thử.
