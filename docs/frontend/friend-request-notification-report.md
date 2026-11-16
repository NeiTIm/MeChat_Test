# Friend Request Notification Integration — 16/11/2026

**SOURCE_IMPLEMENTED / CLIENT_VERIFIED / LIVE_NEEDS_VERIFICATION.**

## Root cause

`FriendService.SendAsync` chỉ lưu FriendRequest và phát FriendshipChanged/FriendRequestsUnreadChanged. Không có Notification được tạo. Frontend cũng chưa nhận diện loại FriendRequest hoặc deep link tới lời mời; badge Bạn bè hoạt động độc lập nên không chứng minh Notification Center có dữ liệu.

## Implementation và contracts tái sử dụng

- Nhánh tạo Pending mới thêm FriendRequest và Notification qua cùng FriendRepository/AppDbContext, một SaveChanges trong transaction Serializable hiện có. Kiểm tra eligibility, pending pair, uniqueness và retry cũ được giữ nguyên. Duplicate trả Conflict; reverse request auto-accept không tạo thêm notification. Retry rollback cả hai record và xóa callbacks của attempt cũ.
- Mapping: `Type = FriendRequest`, `RecipientUserId = ReceiverUserId`, `ActorUserId = SenderUserId`, `ReferenceId = FriendRequest.Id`, CreatedAt cùng thời điểm, ReadAt/DeletedAt mặc định NULL. Không sửa model, schema, migration hoặc DTO.
- Sau commit, dùng StateEvents account-scoped phát NotificationCreated chỉ tới receiver, payload metadata `{eventId, notificationId, type, referenceId}` giống cấu trúc notification hiện có. Publisher hiện có bắt lỗi transport nên không đổi kết quả lưu thành công.
- FriendshipChanged ở nhánh tạo mới thêm metadata tùy chọn `friendRequestId`. Frontend dùng dấu hiệu này để invalidation riêng friendRequests/friendUnread; nhánh auto-accept và các event cũ giữ hành vi cũ. HTTP send tạo Pending mới cũng chỉ invalidation hai projection này. NotificationCreated/read tiếp tục invalidation notifications/notificationCount/unreadSummary. Không cộng badge thủ công, không thêm subscription; giữ reconciliation focus/online/reconnect hiện có.
- NotificationService dùng Text hiện có cho nội dung tiếng Việt. Tên người gửi được đọc qua request ID + recipient, chỉ lấy display name/username công khai; không trả tên khi tài khoản đã deactivate hoặc có block đang hiệu lực. Thiếu tên dùng nội dung chung. Active/Trash dùng cùng resolver; sender không được cung cấp SeenAt.
- Reuse: POST `/api/friends/requests`, GET `/api/friends/requests` và unread-count/mark-seen; GET notifications/unread-count, PATCH notification read và Trash/Delete/Restore hiện có. Không thêm endpoint.
- URL reload được: `/friends/incoming?request={FriendRequestId}`. Friends API hiện trả toàn bộ Pending incoming/outgoing, không phân trang: một query danh sách là đủ, không duyệt vô hạn hoặc thêm target endpoint. Khi có target, revalidate server kể cả cache còn mới và chờ fetch xong trước khi focus; tránh trỏ vào lời mời đã xử lý trên thiết bị khác.
- Scroll/focus đúng article, highlight 2,5 giây, keyboard accessible, không animation/cuộn mượt nên tương thích reduced-motion. Seen confirmation/refetch không kéo lại vị trí đọc. Target mất/đã xử lý/không có quyền có thông báo tiếng Việt; không đoán chính xác trạng thái lịch sử mà API không trả về. Mark-read notification độc lập SeenAt; giữ observer 25%/500ms và batch queue hiện có. Message notification deep link giữ nguyên resolver riêng.

## Files changed

Backend:
- `backend/MeChat.Api/Features/Friends/Services/FriendService.cs`
- `backend/MeChat.Api/Features/Friends/Repositories/FriendRepository.cs`
- `backend/MeChat.Api/Features/Friends/Repositories/Interfaces/IFriendRepository.cs`
- `backend/MeChat.Api/Features/Notifications/Services/NotificationService.cs`
- `backend/MeChat.Api/Features/Notifications/Repositories/NotificationRepository.cs`
- `backend/MeChat.Api/Features/Notifications/Repositories/Interfaces/INotificationRepository.cs`

Frontend:
- `frontend/src/features/friends/FriendsPage.jsx`
- `frontend/src/features/friends/components/FriendRow.jsx`
- `frontend/src/features/friends/hooks/use-friend-request-target.js` (mới)
- `frontend/src/features/notifications/use-notifications.js`
- `frontend/src/features/notifications/NotificationPreview.jsx`
- `frontend/src/features/notifications/components/NotificationCard.jsx`
- `frontend/src/app/providers/RealtimeProvider.jsx`
- `frontend/src/i18n/locales/vi/{friends,notifications}.json`

Tests/config:
- `backend/MeChat.Bcr.UnitTests/FriendNotificationTests.cs` (mới, pure doubles, không DB)
- `backend/MeChat.Bcr.UnitTests/{FriendSeenTests,HttpContractTests}.cs`
- `frontend/tests/friend-request-target.test.jsx` (mới)
- `frontend/tests/realtime.test.jsx`
- `frontend/tests/e2e/friend-notifications.spec.js` (mới)
- `frontend/tests/e2e/moderation-admin-client.spec.js` (chỉ sửa fixture expiry, xem dưới)
- `frontend/playwright.friend-notifications.config.js` (mới, child server cô lập, proxy cổng 9)
- `roadmapFE.md` và báo cáo này.

## Tests

| Check | Kết quả thực chạy | Evidence |
| --- | --- | --- |
| Backend build | PASS, 0 warnings/errors | Output build cô lập; không khởi động Program |
| Backend focused unit + in-process HTTP | 58 PASS / 0 FAIL / 0 SKIP | `.artifacts/friend-notification-tests/friend-notifications-final.trx` |
| Frontend lint/build | PASS | npm.cmd run lint / build |
| Frontend focused unit/component | 58 PASS / 8 files | Target, card/preview, read/seen independence, realtime dedup/invalidation, message cache/metadata, session và BCR queue |
| Browser mới | 9 PASS | `frontend/test-results/friend-notifications-final-results.json` |
| Browser regression | 25 PASS | `frontend/test-results/friend-notifications-regression-final-results.json` |
| BCR browser | 14 PASS | `frontend/test-results/friend-notifications-bcr-results.json` |
| Request storm/reconnect/429 | 3 PASS | `frontend/test-results/friend-notifications-storm/request-storm.json` |
| Localization static audit | PASS, 697 calls resolved | npm.cmd run audit:localization |
| Git diff / formatting source mới sửa | PASS | Không stage/commit/push |
| Live API / SQL concurrency / real two-account realtime | NOT_RUN / NEEDS_VERIFICATION | Chưa cấp phép automatic mutation vào MeChat |

Backend tests dùng interface doubles và TestServer tách Program/DbContext: tạo đúng một notification, recipient/reference, duplicate/reverse/invalid/blocked, commit failure, retry, transport failure, text ownership và unauthenticated HTTP. Đây không phải bằng chứng SQL transaction/concurrency đã PASS thực tế.

Browser mới bao phủ 390px/1366px Light/Dark, contrast/overflow, dữ liệu chưa tải/cache sẵn/reload, stale cache đã hoàn tất, unavailable/403/404, focus/highlight và visible-seen. Regression gồm Private/Group message jump, message actions/requests, Friends actions, auth/profile/session, Admin/retention và appeal isolation. Tổng 51 browser cases riêng biệt (9 + 25 + 14 + 3); các lượt rerun trùng không cộng lại. Không chạy full 191 suite.

Lịch sử kiểm thử: lượt browser mới đầu có 3 FAIL do test giả định chỉ một seen batch và dùng exact nav label bỏ qua badge; sửa kiểm tra GET theo số batch được server xác nhận và chọn link bằng route, sau đó 8 PASS, cuối cùng mở rộng cache case thành 9 PASS. Regression đầu 24 PASS/1 FAIL: fixture Appeals cấp expiry năm 2099 vượt giới hạn timer JavaScript nên phiên kết thúc ngay. Chỉ đổi fixture thành 30 phút, focused case PASS rồi chạy lại 25/25 PASS; không sửa Appeals production source hoặc nới assertion. Báo cáo request-storm cũ được giữ nguyên, số đo mới lưu trong test-results.

## Backend blockers / Next Exact Action

Không cần API/schema/migration mới. SQL race, rollback trên SQL Server và realtime hai tài khoản thật vẫn chưa nghiệm thu. Không chạy migration, seed/reset, DB test ghi dữ liệu hoặc sử dụng tài khoản người dùng; không đổi JWT/CSRF/roles/flags hoặc business rules.

PO khởi động lại Backend/Vite để nạp source rồi thao tác bằng tài khoản Development được phép: gửi lời mời mới, kiểm tra đúng một thông báo ở receiver, nội dung/timestamp/count, Xem lời mời tới đúng ID, ReadAt và SeenAt độc lập, duplicate/reverse, target đã xử lý và Trash/Restore. Nếu cần tự động hóa SQL writes, phải cấp phép riêng với môi trường test xác định.
