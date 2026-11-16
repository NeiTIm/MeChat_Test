# Message notification grouping — 16/11/2026

**SOURCE_IMPLEMENTED / CLIENT_VERIFIED / LIVE_NEEDS_VERIFICATION.** Không migration, schema/configuration change, live mutation hoặc commit/push. Bảo toàn thay đổi Friend Request Notifications đang có trong working tree.

## Root cause và strategy

Private send, group send và forward đang lưu một Notification cho mỗi message/recipient. NotificationService trước đây đếm/trả từng bản ghi. Chúng tiếp tục lưu các bản ghi gốc trong transaction hiện có và phát NotificationCreated sau commit; grouping là **Backend read projection**, không phải xóa lịch sử hoặc distinct trên một trang Frontend.

- Key: `RecipientUserId + Message.ConversationId + Notification.Type`; chỉ `Message` và `GroupMessage`. Sender/display name không tham gia key. MessageRequest, FriendRequest và các type khác giữ nguyên.
- Chỉ nhóm `ReadAt == null && DeletedAt == null`. Lấy metadata toàn bộ message notifications chưa đọc của actor, sắp Sequence tăng dần và chọn notification đầu tiên vượt qua kiểm tra quyền xem hiện có. Recall/Delete For Me/hidden/membership vẫn qua MessageActionService; không trả nội dung bị ẩn.
- Representative ID/ReferenceId không chuyển sang tin mới nhất. Cùng quy tắc dùng cho count và feed; SQL lọc representative **trước** CreatedAt/Id keyset và Take, không distinct sau phân trang. Raw feed scan vẫn tối đa 200; Trash scan/validation không thay đổi.
- Read và Delete một đại diện chưa đọc xử lý các notification active-unread cùng actor/conversation/type bằng **một conditional UPDATE**. Seed phải vẫn active-unread ngay trong SQL. Replay seed đã đọc/xóa không xử lý chu kỳ mới. Các type khác và notification đã đọc vẫn thao tác riêng lẻ.
- Read-all/Delete-read giữ nguyên conditional updates. Không chỉnh Chat read cursor, Delivered/Seen, Chat unread, subscription hoặc notification creation transaction.
- Trash giữ từng bản ghi gốc. Restore vẫn khôi phục riêng ID, không đổi ReadAt; notification khôi phục còn unread được ghép vào projection hiện hành, chọn mốc sớm nhất còn xem được. Không tự restore các bản ghi khác. History đã đọc vẫn giữ từng bản ghi; grouping chỉ áp dụng active-unread.
- SignalR vẫn là invalidation account-scoped hiện có. NotificationCreated có thể tiếp tục phát cho mỗi bản ghi mới; client refetch HTTP projection, không cộng badge thủ công hoặc dùng reference từ event làm target.

## Files của lượt này

Backend source:
- Thêm `backend/MeChat.Api/Features/Notifications/MessageNotificationGrouping.cs`: key/order và SQL conditional action predicate.
- Sửa `.../Notifications/Repositories/Interfaces/INotificationRepository.cs`, `.../Repositories/NotificationRepository.cs`: unread metadata query, representative filter, grouped read/delete.
- Sửa `.../Notifications/Services/NotificationService.cs`: resolve first visible representative và count/feed chung quy tắc.

Tests:
- Thêm `backend/MeChat.Bcr.UnitTests/MessageNotificationGroupingTests.cs` (7 cases): private/group keys, multiple senders/conversations, pagination/global count, hidden first target, cycle replay, ownership/type isolation, delete/restore, SQL Server predicate translation không kết nối DB.
- Cập nhật doubles trong `NotificationLifecycleTests.cs`, `FriendNotificationTests.cs`, `HttpContractTests.cs` cho repository query mới.
- Thêm `frontend/tests/e2e/message-notification-grouping.spec.js` và `frontend/playwright.message-notifications.config.js` (proxy unreachable port9, không tái sử dụng runtime thật).
- Cập nhật `roadmapFE.md` và báo cáo này.

Frontend runtime source **KEPT AS-IS**: Center/Preview/navigation đã dùng server count và server items; nội dung Message/GroupMessage đã là tiếng Việt chung, không có số đếm giả. Existing deep link lấy authorized message metadata từ representative ReferenceId rồi mở `/chat/{conversationId}?at={sequence}&message={messageId}` hoặc `/groups/...`. Không cần endpoint, DTO field, event hoặc client grouping mới. Các thay đổi Frontend/Friends/Realtime đã có trước lượt này được bảo toàn.

## API impact

Routes/status/DTO shape giữ nguyên. `GET /api/notifications`, `/api/notifications/unread-count` và notificationCount trong `/api/unread` sử dụng grouped projection. `PATCH /api/notifications/{id}/read` và `DELETE /api/notifications/{id}` xử lý active-unread group khi seed đủ điều kiện. Read-all, delete-read, Trash/Restore giữ contracts; ReadAt không đổi bởi Delete/Restore. Chỉ recipient được thao tác; visibility seed/404 hiện có giữ nguyên. Events chỉ phát sau conditional statement hoàn tất; StateEvents giữ transport failure isolation.

## Tests thực sự chạy

| Check | Kết quả |
|---|---|
| Backend build, output `.artifacts/message-grouping-build` | PASS, 0 warnings/errors |
| Pure targeted Backend unit/in-process HTTP | **65 PASS, 0 FAIL/SKIP** |
| Frontend build / lint / Prettier targeted / Git diff check | PASS |
| Vitest notification-target, friend-request-target, realtime | **23 PASS / 3 files** |
| Grouped Center/Preview, 390/1366px, Light/Dark, contrast/overflow | **8 PASS** |
| Notification jump/reload/realtime/privacy/cancellation | **11 PASS** |
| BCR Trash/Delete/Restore/visible-seen/responsive | **14 PASS** |
| Request-storm idle60s, send/receive/reconnect, 429 | **3 PASS** |
| Live SQL writes / real concurrent sends / live SignalR acceptance | **NOT_RUN — BLOCKED_TEST_ENVIRONMENT** |

36 distinct browser cases PASS; không chạy full191. Lượt browser mới đầu tiên 8 FAIL vì fixture unread summary thiếu `type: Private`, làm Chat badge giả lập biến mất; sửa fixture, chạy lại 8/8 PASS. Không sửa production code để né assertion. Rollup chỉ có annotation warnings từ dependencies.

Evidence: `.artifacts/message-grouping-tests/message-grouping-final.trx`; `frontend/test-results/message-notifications-final-results.json`, `message-notifications-jump-results.json`, `message-notifications-bcr-results.json`, `message-notifications-storm-results.json`. Fresh request measurements lưu riêng `message-notifications-storm-measurements.json`; báo cáo request-storm cũ được phục hồi đúng bytes. Idle synthetic 60s: total33→56, context8→16, receipts4→8, history1→2 (bounded reconciliation); 429 metadata test không tăng request khi phát thêm hints. Đây là browser HTTP/WebSocket simulation, không phải live API/SQL PASS.

## Risks, migration và Next Exact Action

**Migration impact: NONE.** Không thêm column/index/constraint, không sửa migration/snapshot, không sửa database hay production flags. Giữ physical per-message rows nên không cần unique grouping constraint hoặc backfill; concurrent inserts luôn chiếu về một unread group. Atomic predicates tránh việc thao tác cũ đóng chu kỳ mới, nhưng actual locking/deadlock/insert-read-delete races vẫn NEEDS_VERIFICATION trên SQL Server.

Projection đọc toàn bộ active-unread message metadata của actor để tính đúng toàn cục và chọn target được phép xem. Đây không phải bounded200 cho toàn bộ backlog: cần đo latency/memory/query plan với backlog lớn trên môi trường được duyệt; không tự thêm index khi thiếu evidence. Một response có thể cũ khi có mutation đồng thời; focus/reconnect/network HTTP reconciliation hiện có vẫn áp dụng.

**Next Exact Action:** PO restart Backend đã build và kiểm thử thủ công bằng các tài khoản Development được phép: 5 tin/1 hội thoại →1 nhóm; hai hội thoại →2; nhiều sender/1 group →1; đọc/xóa rồi gửi mới; restore unread và read; target đầu tiên/Recall/Delete For Me; friend notifications riêng; Chat unread giữ số thật. SQL write integration/concurrency/load tests chỉ chạy sau phê duyệt môi trường test cô lập. Chưa xác nhận live acceptance và không tự thực hiện mutation MeChat.
