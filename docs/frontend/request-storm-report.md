# Chat context/receipts request storm — 09/11/2026

Trạng thái: **SOURCE_FIXED / CLIENT_VERIFIED / LIVE_NEEDS_VERIFICATION**. Tạm dừng triển khai BCR theo yêu cầu mới. Không sửa Backend, limiter, JWT, schema hoặc dữ liệu MeChat; không chạy live mutation, migration, commit/push.

## Nguyên nhân và sửa tối thiểu

- `RealtimeProvider` invalidate toàn bộ `workspace` cho từng Delivered/Seen/Reaction. Backend gửi delivery/read hints cả về tài khoản thực hiện ACK/read, khuếch đại thành nhiều lần tải lịch sử và metadata của các hàng đang mount. Invalidation có thể hủy rồi chạy lại request đang tải.
- Reply từng tải context cả khi không có reply; context/receipt dùng staleTime chung 15 giây và focus refetch. Reply và Reactions đã chia sẻ query key nên TanStack có in-flight dedupe; không coi hai observers tự thân là hai HTTP requests. Row keys ổn định, entry được memo, app không bật StrictMode; không có polling riêng từng receipt.
- Đã thêm metadata cache theo account/conversation/message; chỉ tải các hàng gần viewport, reply chỉ tải khi có nguồn. Cache fresh cho tới invalidation, GC 5 phút; tắt retry/focus/reconnect tự động riêng metadata. HTTP reconciliation của provider khi focus/online/reconnect và mỗi 60 giây foreground vẫn giữ để sửa missed events.
- Delivered/Seen chỉ invalidate receipts phù hợp, self-Seen chỉ cập nhật unread projections; Reaction chỉ context liên quan. Gom bursts 80ms, giữ read sequence lớn nhất và bỏ queue khi cleanup/đổi identity. Refetch đang chạy được dùng lại với `cancelRefetch:false`. Edit cập nhật source/replies/search/history phiên bản liên quan; Recall/Delete vẫn sanitize nội dung ngay; permission changes vẫn reset cache bảo vệ riêng tư.
- Axios chỉ replay 401 một lần; không replay 429. Metadata không retry tự động và tôn trọng `Retry-After` (mặc định 30 giây nếu thiếu). Cooldown gắn vòng đời query, không mất khi TanStack xóa error trước refetch. Background GET 429 dùng thông báo tải/đồng bộ tạm hoãn, không đổ lỗi thao tác người dùng; mutation 429 giữ thông báo riêng.

## Số đo browser trước/sau

Cùng Chromium 1366×900, 30 tin tổng hợp (15 tin của actor, mỗi tin thứ 6 có reply), app/React Query/SignalR client thật nhưng HTTP + WebSocket **mô phỏng hoàn toàn**, 60 giây idle thực. Proxy backend của test trỏ cổng 9, không dùng tài khoản hoặc dữ liệu MeChat. Số tổng là API calls của fixture, gồm ACK/read mô phỏng; không gồm assets/frames WebSocket.

| Kịch bản | Trước | Sau |
| --- | ---: | ---: |
| Mở chat, tổng API sau settle 2 giây | 174 | 35 |
| Mở + idle 60 giây, tổng API cộng dồn | 230 | 58 |
| Context + receipts cộng dồn cuối idle | 180 | 26 |
| Riêng cửa sổ idle 60 giây, tổng API tăng thêm | 56 | 23 |
| Gửi/nhận + burst receipt/reaction, tổng cộng dồn | 480 | 73 |
| Cùng luồng trên sau reconnect, tổng cộng dồn | 539 | 96 |
| 429: receipts trước → sau 3 delivery hints | 45 → 90 | 5 → 5 |

Mở + idle giảm khoảng 75% API, 86% metadata. Receipt/reaction hints sau sửa không tăng history requests (4 → 4); reconnect có tải lại history (4 → 5). UI thực sự hiện “Đã xem” và reaction sau hints, không chỉ kiểm đếm request. [Before JSON](request-storm-before.json), [After JSON](request-storm-after.json). Đây không phải phép đo lại chính xác 491 requests/phút từ phiên người dùng.

## Backend rate limiter và cancellation

Đã đối chiếu `Configurations/RateLimitingExtensions.cs`, các Messages/MessageReceipts controllers và thứ tự middleware trong `Program.cs`: global fixed window **600/phút/RemoteIpAddress**, queue 0; named `messages` 120/phút/user (fallback IP), auth 30/IP, upload 20/IP. Context/receipts GET không gắn named `messages`. Giữ nguyên toàn bộ cấu hình. Nhiều tab/client/proxy có thể chia sẻ partition IP; 491 Network requests không tự chứng minh đã vượt 600 API requests trong window.

Authentication chạy trước rate limiter, do đó validation JWT có thể truy cập SQL trước khi request bị reject. Chưa có trace/correlation để kết luận JWT/attachment SQL TaskCanceledException do storm hay đã hết sau fix. Không tự thay timeout, JWT, SQL hoặc limiter.

## Checks và phạm vi còn lại

- Frontend production build, ESLint: PASS.
- Full Vitest: **67 PASS / 0 FAIL**; focused metadata/cache/realtime/session: **24 PASS / 0 FAIL**. Artifact local: `frontend/test-results/request-storm-unit.json`, `request-storm-targeted.json`.
- Browser request storm: **3 PASS / 0 FAIL** (idle, send/receive/reconnect, 429).
- Related browser regression: **60 PASS / 0 FAIL** (Messages, Hybrid interactions, Groups, Media/Notifications; gồm responsive Light/Dark, accessibility, composer/IME/scroll, Reply/Edit/Recall). BCR provider compatibility: **13 PASS / 1 FAIL** do focus không quay lại bulk-delete trigger ở 1366px dark; chạy lại riêng không đổi source **1 PASS**. Giữ failure tại `frontend/test-results/request-storm-bcr/.../error-context.md`; chưa xác định nguyên nhân không ổn định, không báo full14 PASS. Không sửa BCR hoặc nới focus assertion trong lượt này; không chạy full176.
- Trong quá trình sửa, phiên bản cooldown đầu có 1 unit và 1 browser failure (receipts vẫn 5 → 8 khi invalidation); đã sửa lưu deadline theo query object, các ca cuối PASS. Không skip hoặc nới giới hạn để che failure.
- Live request counts, hai tài khoản thật, JWT/SQL cancellation: **NEEDS_VERIFICATION**. Backend build không chạy vì không sửa Backend.

## Files của lượt sửa

Source: `frontend/src/features/messages/{message-metadata.js,message-events.js,MessageActions.jsx,cache.js}`, `features/conversations/WorkspacePage.jsx`, `app/providers/RealtimeProvider.jsx`, `lib/errors.js`, `i18n/locales/vi/{errors,messages}.json`.

Tests/config: `frontend/tests/{message-metadata.test.jsx,realtime.test.jsx,e2e/request-storm.spec.js}`, `frontend/playwright.request-storm.config.js`, `frontend/playwright.config.js` (tách minute benchmark khỏi suite mặc định). Tài liệu: báo cáo này, hai JSON số đo và `roadmapFE.md`. Các diff BCR đã có từ trước được giữ nguyên.

**Next Exact Action:** PO tải lại Vite/chat bằng tài khoản Development, dùng DevTools Preserve log và đo cùng hội thoại: mở + idle 60 giây, manual gửi/nhận, đổi focus và reconnect. Kiểm tra Seen/Delivered/Reply/Edit/Recall, 429 và request cancellation; chỉ ghi counts/status/timing, không xuất token hoặc nội dung riêng tư. Nếu SQL cancellation còn tái hiện, thu correlation/timing đã che dữ liệu để điều tra Backend riêng; chưa có căn cứ tăng rate limit.

Lệnh tái chạy an toàn (tại `frontend/`): `npm.cmd run build`, `npm.cmd run lint`, `npm.cmd test`; `npx.cmd playwright test --config playwright.request-storm.config.js`; `npx.cmd playwright test --config playwright.g2-regression.config.js messages-client.spec.js hybrid-interactions.spec.js media-notifications-client.spec.js groups-client.spec.js`. BCR compatibility dùng config `playwright.bcr.config.js` với `BCR_CAPTURE_SCREENSHOTS=false`, proxy test không trỏ MeChat.
