# MeChat Frontend — Roadmap hiện hành

Cập nhật: **2026-11-06**, Asia/Bangkok. Đây là trạng thái đã đối chiếu source và bằng chứng; [roadmap/checkpoints trước đó](docs/frontend/history/roadmapFE-before-final-recovery.md) được giữ lại.

## Visual Baseline và quyền phê duyệt

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**, ngày **2026-11-06**. Product Owner phê duyệt phương án C — MeChat Hybrid (Modern Messenger + Minimalist SaaS) và ba mẫu Login, Private Chat, Group Chat làm Visual Baseline. Không yêu cầu phê duyệt lại cùng hướng. Màn hình sau dùng design system đã chốt; thay đổi đáng kể Visual Direction mới cần phê duyệt. Phê duyệt UI không phải nghiệm thu chức năng/API.

## Phase / Stage / task hiện tại

Giữ nguyên lộ trình 6 Phase chính thức. Source client của **Phase 05**, các Stage 01–06 đã có consumer; hiện **Phase 06 / Stage 07 Final QA**. Trạng thái: **CLIENT_IMPLEMENTED; AUTHENTICATED_ACCEPTANCE_NEEDS_VERIFICATION**. Không PORTFOLIO_READY và không đánh dấu chức năng business DONE.

Vietnamese-first bắt buộc cho toàn bộ User/Admin, vi-VN fallback vi, HTML vi, Inter, Light mặc định/Dark, responsive, motion có mục đích. Không dịch contracts hoặc nội dung người dùng. [Implementation contract](docs/frontend/implementation-contract.md).

| ID | Task | Dependencies | Trạng thái / bằng chứng |
| --- | --- | --- | --- |
| FE-001 | HTTP/SignalR source + runtime audit | none | Source 19 controllers/123 HTTP/18 events mapped; runtime business NEEDS_VERIFICATION |
| FE-002 | Sitemap, flows, state/responsive specifications | FE-001 static | Documents exist; runtime journeys NEEDS_VERIFICATION |
| FE-003 | Architecture + secure clients | FE-001/002 | Source + session/realtime unit checks PASS; live lifecycle NEEDS_VERIFICATION |
| FE-004 | Design system + three samples | FE-002/003 | Visual Baseline APPROVED; actual features NEEDS_VERIFICATION |
| FE-005 | Prepare original design review submission | FE-004 | DONE; historical review/screenshot evidence preserved |
| FE-006 | Product Owner approval | FE-005 | DONE; explicit approval 2026-11-06 |
| FE-007 | Auth/account/privacy/sessions | FE-006/017 | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION Backend |
| FE-008 | Users/Friends/blocks/presence | FE-007 client | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION Backend/two-user |
| FE-009 | Private messages/actions/requests/receipts | FE-008 client | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION Backend/realtime |
| FE-010 | Groups/roles/invitations/links/pins | FE-009 client | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION Backend |
| FE-011 | Files/avatar/notifications/mute | FE-010 client | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION storage/authorization |
| FE-012 | Reports/isolated appeals | FE-011 client | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION Backend |
| FE-013 | Admin/moderation/audit/retention | FE-007–012 clients | IMPLEMENTED_CLIENT; NEEDS_VERIFICATION DB role/service checks |
| FE-014 | Final QA / portfolio acceptance | FE-013 client | Client audit PASS; authenticated/device/performance NEEDS_VERIFICATION |
| FE-015 | Hybrid design review improvements | FE-004 | DONE historical UI review; advisory scores 91/89/88 |
| FE-016 | Revised samples review submission | FE-015 | DONE; Product Owner approved FE-006 |
| FE-017 | Vietnamese localization | FE-006 | Client audit646 + browser evidence PASS; remaining native/manual quality verification |

## Feature source thực tế

Auth lifecycle + recovery/CSRF/rotation/logout; profile/username cooldown/email/privacy/security/sessions. Friends search cursor/profile/incoming/outgoing/accept/reject/cancel/unfriend/block/unblock/open chat. Messaging history/send/reply/edit/recall/delete/forward/context/reaction/search/receipts/unread/requests/read-cursor/reconnect/presence/typing. Groups create/profile/avatar/members/roles/invite/link resolve+join/pin/leave/transfer/dissolve/hide. Files upload/progress/retry/cancel/preview/download/avatar; Notifications/count/read/read-all/mute. Context Reports/own list, isolated appeal access/history/create. Admin dashboard/users/groups/report claim/evidence/resolve/appeals/audit/retention previews/dry-run/hold. No channels, production mock dataset or purge execution UI.

## API coverage và SignalR

**122 controller actions có existing JavaScript function + named consumer, cộng 1 health diagnostic = 123 HTTP endpoints**. [Inventory](docs/frontend/api-inventory.json), [matrix](docs/frontend/api-to-feature-map.md), [static audit](docs/frontend/api-consumer-audit.json). Đây là source coverage; tất cả successful business endpoints vẫn NEEDS_VERIFICATION.

18 events trong một provider; 4 hub invocations join/leave/start/stop typing. Client xử lý dedupe, permission reset, typing expiry, HTTP reconciliation, revoke và cleanup. 3 unit cases kiểm tra hành vi chính; actual authenticated WebSocket, hai-user receipt/presence/reconnect và missed-event recovery NEEDS_VERIFICATION.

## Kiểm tra đã hoàn tất

| Check | Kết quả |
| --- | --- |
| Unit/component | **33 PASS**, 9 files |
| Full Chromium client/design regression | **128 PASS / 0 FAIL / 0 flaky** |
| Focused sau 2 chỉnh copy cuối | **2 PASS**, không gộp thành một full130 |
| Recovery Groups / messages / media suites | 13/13 PASS mỗi bộ; full cuối bao gồm regression lại |
| Build / lint / localization / API function audit | PASS / PASS / 646 calls PASS / 122 mappings PASS |
| Production fixture isolation / smoke | PASS / 1 PASS |
| npm audit | 0 vulnerabilities |
| Actual Backend boundaries | **4 PASS**; successful auth **1 SKIPPED** theo lựa chọn người dùng |

[Verification report](docs/frontend/verification-report.md), [machine evidence](docs/frontend/implementation-verification.json), [full cases](docs/frontend/final-client-regression.json), [focused cases](docs/frontend/final-copy-regression.json), [screenshots](docs/frontend/screenshot-index.md). 289 PNG tiếng Việt thực tế từ Chromium và các ảnh baseline cũ được giữ riêng. Client integration screenshots dùng HTTP simulation, không phải tài khoản thật. Không lưu authenticated traces/screenshots.

Entry 158.60 kB, gzip 55.61 kB; largest chunk 337.65 kB. Vendor split bỏ warning >500 kB. Third-party PURE comments informational; không coi bundle size là chứng nhận performance.

## Lịch sử lỗi / sửa lỗi

Đã sửa: bản dịch/labels/contrast destructive/heading mobile; blob chưa tải gây runtime; unread aria; DEV navigation string trong production; pending/nested dialog timing; retry signature reply+attachment; header 320px và mobile session exit. Full trung gian 110 PASS/16 FAIL (header + fixture /unread sai shape), sau sửa full128 PASS; lịch sử có trong recovery-full-failure-history.json. Final copy thêm map metadata audit và cảnh báo TXT preview một phần, focused2 PASS. Không dùng retry/skip che failure.

## Chưa xác minh / blockers

Người dùng đã chọn **tiếp tục, giữ đăng nhập thật ở NEEDS_VERIFICATION** khi thiếu credentials. Không hỏi lại hoặc đọc private uploads/dev-mail. Successful Auth/cookie/refresh/logout/profile/session, two-user friends/chat/requests/groups/realtime, file/storage authorization, reports/evidence/appeals/Admin/retention permissions, email delivery và demo thật chưa nghiệm thu. Native Chrome/Edge, screen reader, thiết bị mobile/keyboard/IME/autofill, full WCAG/performance/load vẫn NEEDS_VERIFICATION. [Backend constraints](docs/frontend/backend-gaps.md); chưa có backend change request bắt buộc.

## Files và an toàn workspace

Source: frontend/src/i18n, features/auth/users/friends/conversations/messages/groups/files/notifications/moderation/admin; shared forms/layout/chat/Avatar; app routing/providers/guards; lib clients; CSS tokens. Checks: frontend/tests, Playwright configs, npm scripts; API census generators và docs/frontend. README/spec/acceptance/design-review/coverage/verification/final-acceptance/screenshot index đã cập nhật, lịch sử lưu ở docs/frontend/history.

Không sửa Backend/Database, migrate/seed hoặc thực hiện destructive QA. Không tạo commit. Giữ nguyên các xóa có trước của người dùng: REFACTOR_STATUS.md, STATIC_AUDIT.json. Backend chạy 5080 từ build hiện có với cả FileRetention/RetentionMaintenance scheduled/deletion/purge false. Chỉ boundary requests an toàn được chạy với Backend thật.

## Next Exact Action và recovery

Khi cấu hình kiểm thử an toàn sẵn sàng: chạy successful Auth/session lifecycle trước, rồi profile/privacy, hai-user Friends/chat/request/group/reconnect/media và role Admin/appeal/retention; bổ sung native/device/performance/demo. Chưa có quyền ghi Backend/DB hoặc dùng dữ liệu riêng tư để tự lấy token. Không khôi phục Design Approval block.

Phiên sau đọc AGENTS.md nếu có, file này, implementation-contract và verification-report; kiểm tra git status và source trước khi code. Source clients đã có, không viết lại. Dùng npm.cmd vì npm.ps1 bị execution policy chặn. Các lệnh: npm.cmd --prefix frontend run lint; run audit:localization; run audit:api; test; run test:e2e; run build; run verify:production; run test:e2e:production; run test:e2e:live. Static census: python scripts/frontend-inventory.py, kiểm tra evidence timestamp trước khi dùng kết quả lịch sử.


Bằng chứng bổ sung cuối: secure-auth-input-client.json **1 PASS** với dữ liệu giả lập; actual account vẫn SKIPPED. final-message-media-regression.json **29 PASS** sau khi giới hạn sanitization vào message pages, giữ nguyên Blob và xóa reply/detail/history phụ thuộc. Các lần chạy tập trung trùng một phần với full128; không cộng thành số ca duy nhất. Unit cuối **33 PASS trong 9 files**.
