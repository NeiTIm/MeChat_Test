# BCR fast fix — 09/11/2026

## Development enablement — PO đã phê duyệt 09/11/2026

Đã đổi đúng `BcrFeatures:NotificationLifecycleEnabled=true` và `BcrFeatures:FriendRequestSeenEnabled=true` trong `backend/MeChat.Api/appsettings.Development.json`. Đã tạo `frontend/.env.development.local` với hai VITE_BCR keys tương ứng=true. File env được `.gitignore: .env.*` ignore, không tracked; không đổi Production defaults/API contracts/JWT/DatabaseTarget. Mutation chỉ do PO thao tác thủ công bằng tài khoản Development; agent không khởi động app, mở phiên đăng nhập, chạy mutation/migration hoặc seed/reset DB.

Kiểm tra lượt enablement: Backend build `BcrEnablement` PASS (0 warnings/errors), Frontend production build/ESLint PASS; Vite loadEnv xác nhận development=true và production không có true override. Không chạy lại56/22/14 hoặc full176 vì chỉ đổi cấu hình; các kết quả dưới đây thuộc lượt targeted trước enablement. Runtime/live acceptance vẫn chờ PO restart và thao tác.

Từ workspace root, dừng hai process cũ bằng Ctrl+C rồi chạy ở hai terminal riêng:

```powershell
dotnet run --project backend/MeChat.Api/MeChat.Api.csproj --launch-profile http
npm.cmd --prefix frontend run dev
```

Profile `http` đã đặt ASPNETCORE_ENVIRONMENT=Development, URL5080; Vite dùng127.0.0.1:5173. Không dùng `vite preview`/production build để kiểm UI Development. Env/process overrides đặt false từ ngoài có thể ghi đè file; nếu vẫn không thấy UI, kiểm tra các override này sau restart.

Kiểm thủ công với dữ liệu/tài khoản Development của PO:

- `/notifications`: có Thông báo hiện tại/Thùng rác, Xóa từng item và Xóa thông báo đã đọc. Xóa một item chưa đọc rồi Khôi phục trong Thùng rác: item vẫn chưa đọc; bulk chỉ chuyển các item đã đọc.
- `/friends`: badge Bạn bè lấy số chưa xem riêng, không phải tổng Pending. `/friends/incoming`: giữ row trong viewport tối thiểu25% liên tục500ms, tab foreground/focused; count refetch sau xác nhận, Pending vẫn giữ nguyên, không tự accept/reject. Thao tác xem này có thể tự ghi SeenAt theo phê duyệt.
- Nếu có hai phiên cùng tài khoản, quan sát count/list đồng bộ qua event hoặc focus/reconnect. Đây là hướng dẫn kiểm thủ công, chưa phải live PASS của agent.

Các mục bên dưới ghi lại diagnosis và targeted checks trước khi PO cấp enablement; không còn chờ duyệt bật Development. Next Exact Action: PO restart và kiểm thủ công, báo lỗi đã che thông tin nhạy cảm nếu còn JWT/SQL cancellation. Không cần gửi mật khẩu/token.

## Root cause

UI không thiếu: sáu clients/hooks, Trash/Delete/Restore, friend unread badge và ba event handlers đã có. Cấu hình repo chưa khai báo BcrFeatures hoặc VITE_BCR overrides nên dùng defaults OFF; đây là điểm chặn hiển thị đã xác định. Effective flags của process đang chạy chưa xác minh được.

GET không token tới5080 (trash, friend count) và5173 đều timeout, kể cả probe127.0.0.1 không proxy. Không có session kiểm thử được cấp phép; **JWT/SQL TaskCanceledException: NEEDS_VERIFICATION**, chưa tái hiện/xác định root cause. Không sửa JWT, SQL timeouts hoặc nuốt cancellation theo suy đoán. Migration/cột đã apply tại TEINS\MSSQLSERVER01/MeChat là xác nhận của PO, không phải SQL verification trong lượt này.

## Files changed trong lượt này

- `backend/MeChat.Api/appsettings.Development.json`: hai flags explicit **false**, chuẩn bị Development-only; giữ connection/settings khác.
- `frontend/.env.development.example`: template không tự load, hai keys **false**, hướng dẫn chỉ dùng development.local sau approval.
- `frontend/tests/e2e/bcr-client.spec.js`: cho phép chạy không PNG; đợi tab transition ổn định trước axe. Không bỏ assertions/giảm chuẩn contrast.
- `roadmapFE.md` và báo cáo này. Không viết lại feature hoặc thay shared auth/query/SignalR infrastructure.

## Six API / UI status

| API | Source/UI check | Live |
| --- | --- | --- |
| DELETE /api/notifications/{id} | Client remove → Xóa; ownership/visibility/204/404 contracts có source và safe tests | NEEDS_VERIFICATION |
| DELETE /api/notifications/read | Client deleteRead → confirmation “Xóa thông báo đã đọc”; active-read only | NEEDS_VERIFICATION |
| GET /api/notifications/trash | Client trash → Thùng rác, DTO riêng, paired precise cursor, empty+hasMore | TIMEOUT anonymous probe |
| POST /api/notifications/{id}/restore | Client restore → Khôi phục; giữ ReadAt | NEEDS_VERIFICATION |
| GET /api/friends/requests/unread-count | Client unreadCount → dedicated badge; DTO unreadCount, không message requestCount | TIMEOUT anonymous probe |
| POST /api/friends/requests/mark-seen | Client markSeen → visible25%/500ms/focus queue, chunk100, receiver-only source | NEEDS_VERIFICATION |

Authorize/session validation giữ nguyên. Incoming SeenAt thật/Outgoing null; loading/error/success, cache account scope/invalidation và NotificationDeleted/NotificationRestored/FriendRequestsUnreadChanged đã nối. UI simulation PASS Light/Dark và5 viewport; UI hiện tại5173 chưa xác minh, flags chưa bật. Không nhận HTTP mocks là live integration PASS.

## Targeted tests thực chạy

| Check | Result |
| --- | --- |
| Backend build configuration BcrFastFix | PASS,0 warnings/errors |
| MeChat.Bcr.UnitTests (pure/in-process HTTP, không SQL) |56 PASS,0 FAIL |
| Frontend build / ESLint | PASS / PASS |
| bcr-components + bcr-seen-queue + realtime Vitest |22 PASS,0 FAIL |
| BCR Playwright, proxy9, flags ON chỉ trong simulation |14 PASS,0 FAIL cuối;0 PNG mới, giữ30 ảnh G2 |

Browser lượt đầu9 PASS/5 contrast FAIL do audit chạy giữa transition sau khi bỏ screenshot synchronization; thêm settleMotion trước audit và rerun14 PASS. Build/lint lần gọi đầu sai cwd (`frontend/frontend/package.json`); chạy lại từ frontend cwd đúng và PASS. Không chạy full176, SQL tests, migration hoặc mutation thật. Backend TRX: `backend/MeChat.Bcr.UnitTests/TestResults/bcr-fast.trx`; browser output: `frontend/test-results/bcr-fast`.

## Feature flags / blockers trước khi PO duyệt enablement (lịch sử)

Backend Development: `BcrFeatures:NotificationLifecycleEnabled=false`, `BcrFeatures:FriendRequestSeenEnabled=false`. FE template: `VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED=false`, `VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED=false`. Production appsettings/defaults không đổi; không tạo active env override hoặc restart app đang sử dụng.

**Cần PO phê duyệt riêng trước khi bật cả hai phía trên Development/MeChat**: đặc biệt friend mark-seen tự ghi SeenAt khi row đủ điều kiện hiển thị. Sau approval mới đổi các flags Development được duyệt sang true, tạo FE development.local tương ứng và restart Backend/Vite; không dùng production env. Sau đó cần app reachable và sessions/test data được cấp phép an toàn để kiểm sáu API thật/JWT cancellation. Không gửi password/token vào chat. Không sửa DB, chạy migration, seed/reset hoặc commit/push trong lượt này.
