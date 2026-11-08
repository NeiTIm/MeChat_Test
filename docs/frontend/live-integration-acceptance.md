# Nghiệm thu tích hợp thật — kế hoạch chờ môi trường

Ngày 07/11/2026. **BLOCKED_TEST_ENVIRONMENT**, không ghi PASS cho business API. Bộ anonymous boundary đã chạy không thay thế các bước dưới.

## Tiền đề bắt buộc trước test có ghi dữ liệu

- Product Owner/operator cung cấp hai tài khoản test đã xác minh (A/B), nhóm/quan hệ/tệp test được phép thao tác; thêm C để phân vai nhóm khi cần. Admin phải được cấp sẵn hợp lệ. Không tự cấp quyền hoặc dùng tài khoản chính.
- Operator nhập credentials ngoài chat vào process environment `MECHAT_E2E_IDENTIFIER`, `MECHAT_E2E_PASSWORD` hoặc `frontend/.env.e2e.local` đã ignore. Không đưa giá trị vào command arguments, Git, screenshots, trace/video, reporter hay báo cáo lỗi. Single-account live suite hiện có chỉ đọc hai biến này; chưa có automated two-user acceptance harness.
- Backend chạy 5080 với cấu hình hiện có; không sửa source/config/DB hoặc migration/seed. Frontend dùng cùng origin 127.0.0.1:5173 qua proxy `/api` và `/hubs`, tránh trộn localhost/127.0.0.1 cho cookie.
- Dữ liệu test có nhãn `QA-20261107-<run>` và nội dung tổng hợp. Operator ghi riêng IDs tạo mới để cleanup, không xuất nội dung chat hay token. Không đụng bản ghi có trước.
- Chốt cleanup trước khi chạy: bỏ các preference test về ban đầu; thu hồi đúng link/phiên test tạo mới, rời nhóm test theo quyền. Không reset/seed/SQL delete. File upload không có Delete API: giữ danh sách ID riêng cho operator quyết định retention/cleanup sau, không bật job xóa. Dissolve/recall/delete-for-me chỉ thực hiện trên test fixtures được cấp phép, không tự thực hiện trong đợt hiện tại.
- Suspended appeal account phải là disposable identity được phép dùng. Không tự suspend để tạo tiền đề. Retention chỉ preview/dry-run; hold cần approval riêng về test fixture; không purge/job deletion.

## Thứ tự thực thi và bằng chứng tối thiểu

| Luồng / tickets | Kiểm tra thật | Evidence được lưu |
| --- | --- | --- |
| QA-001…004/039 Auth/session | Login A/B, /users/me đúng actor; reload; expiry/refresh single-flight/CSRF rotation; logout/revoke tab khác; stale request không tràn identity | Case ID, thời điểm, endpoint pattern/status, kết luận actor đúng/sai; không token/header/cookie values |
| QA-005…008 Friends/profile | Request/cancel/reject/accept A↔B; block/unblock khi chat; cập nhật tên/avatar và cache | Pass/fail theo action, không tên/email/người thật; không nội dung response |
| QA-009…016 Messages/realtime | Negotiate/WS101, send HTTP+event không trùng, clientMessageId retry; ACK/read/unread; 100+ history/scroll; edit15phút/recall/reply/forward/reaction; offline30s/reconcile/typingTTL/privacy | Transport/status/counts kết quả; không payload hoặc message text; đối chiếu ID trong phiên operator, không công bố |
| MC-004/005, QA-017…019 Groups | Create→CTA→link→copy; resolve trước confirm; hết hạn/thu hồi/trùng/blocked; Owner/Admin/Member trái quyền bị server từ chối | Endpoint pattern/status + kết luận; không URL/token clipboard/history trong report |
| MC-011/012, QA-020…022 Files | Ảnh PNG/JPG/WebP tổng hợp, PDF/TXT/ZIP; upload→send→preview→download; sau reload và access expiry; user không có quyền, membership lost/recall/storage missing | Status, Content-Type, kích thước/decoded true, SHA256 tệp test nếu cần; không blob bytes/private IDs/URL |
| MC-002/007/008, QA-023/024 Notifications/presence | Count/page/tray/nav khớp sau read và read-all; failure giữ count; tab B cập nhật; deep link tới đúng quyền; hidden online không hiện offline | Count/status/type và pass/fail; không authored message/notification text |
| QA-026…028 Admin/appeals | Admin200/User403 trên API thực; report assignment/evidence; appeal token không gọi được user/admin; audit/dry-run giữ an toàn | Pattern/status/role và quyết định; không evidence thật hoặc token |
| MC-010/012, QA-030…034 Device/UI | Chrome/Edge true browser zoom80–200 ở 1366/1440; 5 widths×2theme; Android/iOS keyboard/Telex/IME; NVDA; CPU/memory khi lịch sử dài | Ảnh chỉ từ account/dữ liệu tổng hợp, metrics và kết quả thao tác; không snapshot account thật |

## Chạy được ngay mà không credentials

`npm.cmd run lint`, `npm.cmd test`, `npm.cmd run test:e2e`, `npm.cmd run build`, `npm.cmd run audit:localization`, `npm.cmd run audit:api`, production isolation/smoke và `npm.cmd run test:e2e:live` (4 boundary cases; account case SKIPPED nếu chưa cấu hình).

Mỗi case chỉ đổi VERIFIED khi đủ acceptance workbook, evidence thật khi phụ thuộc Backend, regressions và UI quality. Không chụp DevTools headers, mở dev-mail/private uploads hay thu thập HAR chứa tokens.
