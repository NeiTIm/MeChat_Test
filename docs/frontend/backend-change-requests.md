# Backend Change Requests — 07/11/2026

Chỉ là đề xuất để Product Owner duyệt. **Chưa sửa Backend/Database, chưa thay đổi contract hoặc chạy migration.**

## BCR-001 — Xóa thông báo

- Related Ticket: **MC-006**, QA-023.
- Current Backend Limitation: `Features/Notifications/Controllers/NotificationsController.cs` có GET danh sách/count, PATCH read, POST read-all và settings; không có DELETE. Mark Read không xóa record.
- Proposed API/Business Change: DELETE `/api/notifications/{id}` cho thông báo thuộc người gọi; DELETE `/api/notifications` cho toàn bộ thông báo của người gọi tại thời điểm xử lý. Trả 204, làm mới count và danh sách sau khi server xác nhận. Chốt chính sách idempotency/404 và xử lý notification mới phát sinh trong lúc xóa trước khi triển khai.
- Security Impact: xác thực phiên hiện có; recipient ownership tại service/repository, không nhận userId từ client. Rate limit và truy vết số lượng, không log nội dung. Không cho xóa audit/moderation evidence gốc khi xóa notification.
- Database Impact: cần quyết định soft-delete/hard-delete và retention. Khuyến nghị soft-delete nếu có yêu cầu khôi phục; có thể cần field/index/migration, **chưa được duyệt**.
- Frontend Impact: confirmation riêng cho xóa tất cả; pending/error/retry; invalidate notifications/count/unread sau thành công; giữ khác biệt với Đánh dấu đã đọc. Không làm UI xóa giả.
- Recommended Solution: duyệt semantics/retention trước, sau đó Backend implementation + authorization/transaction tests và event invalidation theo contract được duyệt. Chưa tự thêm SignalR event.
- Approval Status: **PENDING_PRODUCT_OWNER_APPROVAL**. Ticket: **BLOCKED_BACKEND_APPROVAL**.

## BCR-002 — Unread lời mời kết bạn

- Related Ticket: **MC-002**, QA-005, QA-023.
- Current Backend Limitation: `/api/unread` có per-conversation message counts, requestCount và notificationCount; `/api/friends/requests` có Incoming/Outgoing pending requests, không có read cursor/unread count cho lời mời kết bạn. `FriendRequestsDto` không có ReadAt. Không coi số request đang hiển thị là unread.
- Proposed API/DTO/Business Change: thống nhất ý nghĩa “chưa đọc” và “đang chờ phản hồi”. Nếu cần unread: GET `/api/friends/requests/unread-count` và endpoint đánh dấu các request thực sự đã nhìn thấy bằng IDs hoặc server cursor. Không đánh dấu tất cả chỉ vì mở trang. Có thể mở rộng UnreadSummaryDto theo phê duyệt sau này.
- Security Impact: recipient-only, monotonic bounded cursor/ownership; không ghi thao tác đọc cho sender/ID không được phép. Đồng bộ nhiều tab qua invalidation hiện có hoặc event được duyệt.
- Database Impact: có thể cần read marker/cursor riêng; chưa quyết định và chưa migration.
- Frontend Impact: hiện có thể dùng server conversation counts cho Tin nhắn/Nhóm và notification count cho Thông báo. **Bạn bè chưa có unread badge**, không bịa số và không tự xóa badge khi click.
- Recommended Solution: Product Owner chốt unread hay pending. Chỉ triển khai API/DB và badge Bạn bè sau khi contract được duyệt.
- Approval Status: **PENDING_PRODUCT_OWNER_APPROVAL**. Phần Bạn bè của MC-002: **BLOCKED_BACKEND_APPROVAL**.

## Quyết định không cần Backend change trong đợt này

- MC-005: DTO đã trả token chỉ lúc tạo. Sửa lifecycle state ở Frontend, không yêu cầu Backend trả lại token trong danh sách.
- MC-008: PresenceDto.Online nullable đã biểu diễn hidden/unknown; không chuyển null thành offline.
- MC-009/QA-025: lựa chọn toast chỉ lưu trên trình duyệt theo tài khoản. Không quảng cáo đồng bộ đa thiết bị.
- MC-011/012: server hiện có authorized Blob endpoint; chưa có chứng cứ authenticated storage lỗi. Cần test account/file được cấp phép, chưa đề nghị sửa storage theo suy đoán.
