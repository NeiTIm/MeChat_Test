# Notification → exact message — 09/11/2026

**SOURCE_IMPLEMENTED / CLIENT_VERIFIED / LIVE_NEEDS_VERIFICATION.** Không sửa Backend/Database, chạy migration, live mutation, commit hoặc push. Giữ thay đổi tiêu đề checkpoint request-storm đã có trong working tree.

## Contract và URL

Notification DTO chỉ có `type/referenceId`; source tạo notification Private/Group/Forward dùng ID của message. Dùng `GET /api/messages/{id}` để kiểm quyền hiện hành và lấy `id/conversationId/sequence`; từ chối mismatch hoặc sequence ngoài safe integer. Không lấy conversationId từ text hiển thị hoặc giả định referenceId là conversationId.

URL: `/chat/{conversationId}?at={sequence}&message={messageId}` hoặc `/groups/{conversationId}?at={sequence}&message={messageId}`; MessageRequest giữ route riêng. URL reload được; target được giữ qua login bằng `returnTo` trong query, chỉ chấp nhận đường dẫn nội bộ thuộc chat/groups/message-requests, không open redirect. Auth/session/privacy contracts giữ nguyên.

History sẵn có: `GET /api/conversations/{id}/messages` hoặc `/api/groups/{id}/messages`, `beforeSequence=sequence+1&limit=30`. Source repository lọc quyền/hidden/deleted/membership và trả sequence giảm dần. Tải thẳng một trang chứa target, không tìm bằng cách tải tuần tự toàn bộ lịch sử. Nếu target đã có trong cache, dùng history hiện có và không refetch-on-mount để tìm lại. `/messages/{id}/context` chỉ cung cấp reply/reactions, không phải cửa sổ history; không thêm consumer context mới. **Không có Backend blocker cho phạm vi này.**

## Hành vi

- Dùng chung action cho trang Notifications và popover trong chat. Hủy lookup khi đổi route/account/unmount; response cũ không điều hướng sang tài khoản/hội thoại khác. Notification read chỉ invalidate notifications/count/unread projections, không toàn bộ history/metadata.
- Chỉ cuộn `.message-scroll`, focus với `preventScroll`, outline dùng ring token Light/Dark hiện có trong 2,5 giây. Không thêm animation hoặc layout shift. Chờ các panel/filter đóng trước khi đo viewport. Bấm lại cùng notification vẫn jump/highlight lại.
- Auto-scroll-bottom và ResizeObserver không ghi đè target trong chế độ anchored, kể cả khi HTTP còn pending hoặc nhận SignalR. Giữ prepend anchor; nút “Trở về tin mới nhất” thoát chế độ anchored. Không thêm SignalR subscriptions/events.
- Visible-read observer chỉ bật sau khi jump đã đặt đúng vị trí; giữ foreground/visibility/dwell và read-cursor rules. Delivery ACK vẫn theo logic hiện có, không suy ra Seen từ click notification.
- Recall chỉ hiện placeholder đã thu hồi. Unknown/Delete For Me/quyền thay đổi dùng thông báo lỗi tiếng Việt hiện có; target biến mất trong history hiển thị “Tin nhắn không còn trong phạm vi lịch sử bạn được xem.” Không inject nội dung GET đơn lẻ vào list khi history không cho phép. Source hiện render list đầy đủ, chưa virtualized; không thêm virtualization.

## Files

- `frontend/src/features/notifications/{use-notifications.js,message-target.js}`.
- `frontend/src/features/conversations/WorkspacePage.jsx`.
- `frontend/src/components/chat/{ChatWorkspace.jsx,use-thread-scroll.js}`.
- `frontend/src/app/guards.jsx`, `frontend/src/features/auth/LoginPage.jsx` — chỉ giữ target qua login.
- `frontend/tests/notification-target.test.js`, `frontend/tests/e2e/notification-jump.spec.js`.
- Báo cáo này và `roadmapFE.md`. Không đổi CSS, translations hoặc provider; tái sử dụng tokens/vi messages có sẵn.

## Checks

Build/ESLint và31 unit tests metadata/target/session/realtime **PASS / 0 FAIL**. Browser mở rộng **47 PASS / 0 FAIL** (auth/account/logout, notifications, message actions/search, scroll/composer; gồm9 ca jump tại lượt đó). Sau bổ sung race/cancel/repeat-target, lượt cuối **14 PASS / 0 FAIL**:11 ca jump và3 ca search/scroll/composer. Hai ca Private/Group sau đó bổ sung assertion read-cursor thực sự được gửi (không chỉ kiểm mảng rỗng) và không vượt target: **2 PASS**. Không cộng các lượt trùng nhau thành tổng số ca duy nhất, không chạy full suite.

Browser dùng Chromium, HTTP/WebSocket simulation, proxy cổng9 không tới MeChat. Bao phủ private desktop/light và group390px/dark/reduced-motion, loaded/old target, direct reload, recall/unavailable/permission error, login+external redirect rejection, pending-history realtime, route cancellation, repeated click, focus/highlight và container scroll. Assertion kiểm one-page cursor, một target GET/click, một SignalR connection và không ghi read-cursor vượt target trong trang lịch sử cũ. Không coi mocks là successful Backend integration. Một lint lỗi dependency dư ban đầu đã được sửa; không có browser/unit failure trong các lượt jump đã hoàn tất.

**Next Exact Action:** PO kiểm hai tài khoản Development thật: notification Private/Group mới và rất cũ → đúng target; refresh/login return, Recall/Delete For Me/membership change, nhận tin trong lúc jump. Theo dõi Network chỉ một lookup + trang bounded khi chưa cached; không xuất token/nội dung riêng tư. SQL/live storage/authorization và native screen reader vẫn NEEDS_VERIFICATION.
