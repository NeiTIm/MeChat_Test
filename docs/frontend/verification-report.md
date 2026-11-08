# Báo cáo kiểm tra Frontend — 07/11/2026

Checkpoint bug fix theo workbook: **52 ticket gốc +4 phát hiện mới**. [Trạng thái từng ticket](improvement-progress.md), [machine evidence](improvement-verification.json), [57 ảnh tổng hợp](improvement-screenshots.md). Không sửa Backend/Database hoặc ghi dữ liệu business. Không nghiệm thu100%.

| Kiểm tra hiện hành | Kết quả |
| --- | --- |
| Full Chromium client regression | **176 PASS,0 FAIL,0 flaky** — improvement-final-regression.json |
| Vitest/RTL | **40 PASS trong11 files** |
| Build / lint | PASS / PASS |
| Localization / API consumer audit | **687 static calls /122 source mappings PASS**, không phải live API coverage |
| Production isolation / built smoke | PASS /1 PASS |
| Actual browser zoom | **56 PASS** bằng Chromium chrome.tabs.setZoom; tách responsive5 widths×2theme |
| Actual Backend anonymous boundaries | **4 PASS; successful auth1 SKIPPED** vì thiếu credentials được cấp phép |
| Privacy scan | Không phát hiện JWT-shaped/private-key/sensitive tracked path/production console trong scope; workbook riêng tư ignored |
| Synthetic performance sampling | 500 tin/17 cursor pages, bubble anchor shift0px; 4xCPU input/heap sample ở improvement-client-performance.json; chưa chứng nhận performance production |

Source fixes: authorized image MIME/decode/fallback/zoom, lazy thumbnail, one-time invite URL qua cache reset, invite CTA, real unread badges, privacy-safe presence dot, shared notification tray/read/count, success toast local/inline fallback, search context cursor/highlight, role-null group guard, stable idle typing và memo message rows. Light/Dark, Vietnamese, focus/modal/overflow/composer có browser evidence. Lỗi HTTP403/404/401/network được giả lập có chủ đích để kiểm tra client; không phải chứng cứ Backend từ chối đúng quyền thực.

**MC-011/012 còn FIXED_NEEDS_VERIFICATION**: không có test identity/file thật được phép để xác nhận storage/authorization/original file; mobile vật lý chưa chạy. **MC-006 và phần Friends unread MC-002** chờ [BCR-001/002](backend-change-requests.md). Native Chrome/Edge/NVDA/Android/iOS và authenticated performance còn NEEDS_VERIFICATION. [Live acceptance plan](live-integration-acceptance.md).

Các kết quả dưới đây thuộc checkpoint06/11, giữ để truy nguyên, không cộng các lượt trùng thành số ca hiện hành.

## Checkpoint triển khai trước — 06/11/2026

**CLIENT_IMPLEMENTED; AUTHENTICATED_ACCEPTANCE_NEEDS_VERIFICATION.** Design Gate đã APPROVED. Không tuyên bố frontend hoàn thành 100% hoặc PORTFOLIO_READY.

| Kiểm tra | Kết quả |
| --- | --- |
| Build / production scan / smoke | PASS / PASS / 1 PASS |
| Lint / static localization / API function census | PASS / 646 calls PASS / 122 functions PASS |
| Unit/component | 33 PASS in 9 files |
| Chromium full regression | 128 PASS, 0 FAIL, 0 flaky |
| Final copy/partial-preview regression | 2 PASS |
| Message/media sau sửa cache | 29 PASS, targeted rerun |
| Safe live-input helper | 1 PASS, client simulation |
| Real Backend anonymous boundaries | 4 PASS; successful account 1 SKIPPED |
| npm audit | 0 vulnerabilities |

## Phạm vi bằng chứng

Full regression gồm 49 ca baseline/design/guard, 12 Auth/account, 11 Friends, 13 Groups, 13 Media/Notifications, 14 Messages/report/read cursor, 2 mobile session, 14 Admin/moderation. Sau đó 2 ca tập trung xác minh bản dịch metadata audit và cảnh báo TXT preview một phần/escape nội dung. Bộ full chạy trước hai thay đổi copy cuối; focused tests chạy sau, rồi build/lint/unit/production được chạy lại. Không gộp thành một lần full 130 ca.

Các client tests dùng HTTP simulation trong thư mục tests, có nhãn trong tên/báo cáo; production code gọi API thật. Screenshots từ Chromium thật, không phải bằng chứng authenticated Backend. Xem [128 ca](final-client-regression.json), [2 ca cuối](final-copy-regression.json), [báo cáo máy đọc](implementation-verification.json), [chỉ mục ảnh](screenshot-index.md).

Backend thật 5080 được chạy từ build có sẵn với cả scheduled retention/deletion/purge tắt. Health 200 đúng response; protected/hub anonymous 401; auth validation 400 và missing-CSRF 401. Các ca này không chứng minh SQL/business success. Không tạo tài khoản, thay đổi dữ liệu nghiệp vụ, migrate/seed hoặc đọc private uploads/dev-mail. [Boundary evidence](runtime-boundary-evidence.json).

## Kiểm tra kỹ thuật

- Source API census: 19 controllers, 122 actions + health. 122 functions/modules có thật và named consumers; api-consumer-audit.json. Toàn bộ successful business API vẫn NEEDS_VERIFICATION.
- SignalR: 18 event names, một provider, join/leave/start/stop typing, reconnect/focus/online/periodic sync; unit kiểm tra dedupe, permission cache reset, typing TTL, revoke và cleanup. Không coi SignalR giả lập là actual WebSocket PASS.
- Memory JWT; refresh cookie/rotating CSRF; không persist credentials. Session tests kiểm tra refresh single-flight, stale response, rotated CSRF, không replay mutation sang identity khác và malformed response. Appeal token/transport/cache riêng, hết hạn/kết thúc có cleanup.
- vi-VN/fallback vi, HTML vi, Inter Vietnamese, Intl Asia/Ho_Chi_Minh; không dịch user content/contracts. Source Backend đã có UtcDateTimeConvention; frontend không sửa thời gian gửi API.
- Chỉ theme lưu localStorage; source không có dangerouslySetInnerHTML/eval. Blob URL cleanup được test; tệp tải qua JWT và Backend reauthorization; ký hiệu cảm xúc, counts và receipts không suy từ kết nối.
- Layout/axe 320/390/768/1366/1440 × Light/Dark, modal/focus/keyboard/scroll/IME simulation/reduced motion. Kiểm tra tự động có giới hạn và không chứng nhận full WCAG.
- Bundle entry 158.60 kB / gzip 55.61 kB; chunk lớn nhất 337.65 kB. Không còn warning >500 kB. PURE-comment warnings của dependency còn mang tính thông tin; không phải kết quả Lighthouse/load.

## Lịch sử lỗi được giữ

Recovery build/lint/26 unit và Groups13 đã được chạy lại trước khi mở rộng. Đã sửa blob null runtime, unread aria, contrast destructive, headings mobile, DEV navigation trong production, pending/dialog timing, 320px header overflow. Full trung gian 110 PASS/16 FAIL do header và fixture /unread sai shape; [lịch sử](recovery-full-failure-history.json). Sau sửa, full128 PASS. Không dùng retry/skip để che các lỗi này.

## Chưa xác minh

Successful auth/refresh/logout/profile/sessions; hai tài khoản realtime/receipts/reconnect/block/membership; upload/download/avatar/file permissions thật; report/evidence/appeal/admin/retention quyền thật; email delivery; native Chrome/Edge, screen reader, zoom toàn ứng dụng, thiết bị mobile/IME/password manager; performance/load và demo hai người dùng. User đã chọn tiếp tục và hoãn credentials, không cần hỏi lại trong checkpoint này. Các mục trên giữ NEEDS_VERIFICATION.


Bằng chứng bổ sung cuối: secure-auth-input-client.json **1 PASS** với dữ liệu giả lập; actual account vẫn SKIPPED. final-message-media-regression.json **29 PASS** sau khi giới hạn sanitization vào message pages, giữ nguyên Blob và xóa reply/detail/history phụ thuộc. Các lần chạy tập trung trùng một phần với full128; không cộng thành số ca duy nhất. Unit cuối **33 PASS trong 9 files**.
