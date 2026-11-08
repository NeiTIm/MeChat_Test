# MeChat Frontend — Roadmap hiện hành

Cập nhật: **2026-11-09**, Asia/Bangkok. Đây là trạng thái đã đối chiếu source và bằng chứng; [roadmap/checkpoints trước đó](docs/frontend/history/roadmapFE-before-final-recovery.md) được giữ lại.

## Typing Indicator trên Composer — 09/11/2026

Đã chuyển trạng thái đang nhập khỏi Header xuống hàng cố định28px giữa Message Panel và Composer, dùng chung Private/Group. Header giữ presence/last seen/privacy và thông tin nhóm hiện có. Hiển thị tên từ profile/member cache; nhóm tối đa hai tên + số người khác, fallback “Thành viên” khi chưa có tên; không thêm API/subscription. Giữ nguyên SignalR events, TTL và Start/StopTyping. Có polite live region, ba chấm animation nhẹ, reduced-motion và ellipsis chống overflow. Không sửa scroll hooks, Backend/Database hoặc commit/push.

Files: `frontend/src/components/chat/{ChatWorkspace,TypingIndicator}.jsx`, `frontend/src/features/conversations/WorkspacePage.jsx`, `frontend/src/styles/index.css`, `frontend/src/i18n/locales/vi/messages.json`; tests `frontend/tests/typing-indicator.test.jsx`, `frontend/tests/e2e/typing-indicator.spec.js`.

Checks: build/ESLint PASS; Vitest typing/realtime **9 PASS**; browser cô lập HTTP/WebSocket mô phỏng **4 PASS** (private320/light,1366/dark; group390/dark,1440/light). Kiểm tên/nhiều người/self+other-room filtering, header không đổi khi typing, StopTyping/TTL, một connection, vị trí slot/composer, scroll lịch sử không đổi, overflow và reduced-motion. Hai lượt browser đầu lỗi fixture thiếu username rồi assertion chụp header trước profile load/so sánh innerText với textContent; đã sửa fixture/synchronization, không nới tiêu chí UI. Không chạy full regression hoặc live mutation. Native screen reader/hai tài khoản thật vẫn NEEDS_VERIFICATION.

**Next Exact Action:** PO tải lại giao diện và kiểm typing hai tài khoản Development thủ công; phần BCR vẫn tạm dừng theo yêu cầu trước. Request-storm evidence bên dưới được giữ nguyên.

## Ưu tiên hiện hành — Chat request storm / HTTP 429, 09/11/2026

**SOURCE_FIXED / CLIENT_VERIFIED / LIVE_NEEDS_VERIFICATION. Tạm dừng công việc BCR.** Đã xác nhận global workspace invalidation trên từng Delivered/Seen/Reaction làm tải lại lịch sử/context/receipts hàng loạt, kể cả ACK/read hints của chính actor. Sửa Frontend tối thiểu: metadata cache account/conversation/message, viewport-lazy, reply đúng điều kiện, invalidation theo mục tiêu, gom event bursts80ms, giữ in-flight request và cooldown429. HTTP reconciliation focus/online/reconnect/60s, permission reset, Delivered/Seen/Edit/Recall/Reply được giữ; không sửa limiter/JWT/Backend/DB.

Đo cùng browser mô phỏng30 tin: mở+idle60s **230 → 58 API requests**, context+receipts **180 → 26**; gửi/nhận+events+reconnect **539 → 96**. Riêng idle window **56 → 23** API requests. Đây là mock HTTP/WebSocket, không phải phép đo lại phiên491/min của PO hoặc Live Integration PASS. Backend source xác nhận global600/min/IP, không tăng giới hạn; JWT/attachment SQL cancellation chưa có bằng chứng root cause.

Checks: production build/ESLint **PASS**; full Vitest **67 PASS**, focused24 PASS (subset); request-storm browser **3 PASS**, related Messages/Hybrid/Groups/Media60 PASS. Extra BCR compatibility **13 PASS/1 FAIL** (Escape focus1366dark), focused rerun không sửa source1 PASS; giữ failure và ghi nhận cần kiểm chứng độ ổn định, không tuyên bố full14 PASS. Không chạy full176, SQL/live mutation hoặc Backend tests. [Báo cáo, file list, lệnh và giới hạn](docs/frontend/request-storm-report.md), [before](docs/frontend/request-storm-before.json), [after](docs/frontend/request-storm-after.json).

Files sửa: message metadata/events helpers + MessageActions/cache, WorkspacePage, RealtimeProvider, errors/vi translations; message-metadata/realtime tests, isolated browser benchmark/config và testIgnore, docs. Các diff Backend/BCR chưa commit trước lượt này được bảo toàn. Không migration/DB writes, commit/push; flags Development được PO phê duyệt trước đó giữ nguyên.

**Next Exact Action:** PO tải lại frontend, đo Network cùng hội thoại mở+idle60s và tự kiểm gửi/nhận/Seen/Delivered/Reply/Edit/Recall/reconnect trên Development. Không xuất tokens/nội dung riêng tư. Nếu cancellation còn tái hiện, thu status/timing/correlation đã che dữ liệu để điều tra Backend; kiểm lại BCR dialog focus riêng khi tiếp tục BCR. Agent không tự đăng nhập hay chạy ACK/read/mutation trên MeChat.

## BCR Development enablement — PO APPROVED, 09/11/2026

Theo phê duyệt mới: hai Backend flags trong appsettings.Development.json và hai FE flags trong `.env.development.local` đã **true**; Production defaults giữ nguyên. Env local được Git ignore, không tracked. Không đổi API/JWT/DatabaseTarget, không commit/push. Chỉ cho phép PO thao tác mutation thủ công bằng tài khoản Development; agent không chạy mutation/SQL/migration/seed/reset hoặc tự mở app đã đăng nhập.

Checks cấu hình + Backend build BcrEnablement (0 warnings/errors), Frontend production build/ESLint **PASS**. Vite development loadEnv=true, production không true override. Không rerun56/22/14/full176 vì chỉ đổi flags. [Kết quả và lệnh restart/manual checklist](docs/frontend/bcr-fast-fix-report.md).

**Next Exact Action:** PO dừng process cũ, chạy Backend profile `http` (Development,5080) và `npm.cmd --prefix frontend run dev` (127.0.0.1:5173); kiểm Notifications Trash/Delete/Restore/read-only bulk và Friends unread/viewport mark-seen thủ công. Cấu hình đã bật, runtime cần restart; chưa nhận Live Integration PASS hoặc xác minh JWT cancellation. Các checkpoint flags OFF bên dưới là lịch sử đã được phê duyệt mới thay thế, không yêu cầu lại quyền enablement này.

## BCR fast diagnosis / enablement preparation — 09/11/2026 (trước phê duyệt)

Không thiếu sáu clients/UI; repo không có flags overrides nên defaults OFF đang chặn hiển thị. Đã thêm Backend Development flags explicit false và FE development-only example false; **chưa bật tính năng**, production defaults không đổi. Thêm chế độ BCR browser không PNG và đợi tab transition trước axe; không sửa business/JWT hoặc shared infrastructure. [Báo cáo ngắn](docs/frontend/bcr-fast-fix-report.md).

Targeted checks cuối: Backend build PASS,56 BCR safe tests PASS; Frontend build/lint PASS,22 tests liên quan PASS,14 BCR browser PASS (simulation). Không chạy full176 hoặc SQL write tests, không screenshot mới. Lượt browser đầu9 PASS/5 transition-contrast FAIL đã ghi rõ; sửa synchronization rồi14 PASS, không giảm assertion.

PO xác nhận migration đã apply tại TEINS\MSSQLSERVER01/MeChat và hai cột tồn tại; agent không truy vấn/apply SQL trong lượt này. GET anonymous5080/5173 timeout; effective runtime flags và JWT/SQL cancellation **NEEDS_VERIFICATION**. Checkpoint G2 “NOT_APPLIED” bên dưới là lịch sử08/11, không phủ nhận xác nhận mới của PO.

**Next Exact Action:** PO phê duyệt riêng bật hai cặp Backend/Frontend flags Development cho MeChat (gồm auto mark-seen writes), rồi restart app và cung cấp phương thức test session/data an toàn. Chưa có quyền enablement/mutation thật; không bật production, không DB changes/commit/push.

## BCR G2 implementation — checkpoint 08/11/2026 (hoàn tất phạm vi source)

**BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. PO đã duyệt T01–T08/G1/G2. **G2 COMPLETE — SAFE_BOUNDARY_ONLY**. **G3/G4/G5 NOT_AUTHORIZED**; dừng trước SQL/apply/release, không hỏi lại cùng technical/business approval.

Đã triển khai6 endpoints BCR,3 events account-scoped; conditional writes/visibility/ownership; active-read bulk, Trash/Restore giữ ReadAt; SeenAt privacy/backfill/receiver-only count/update. Backend refactors Notifications của người dùng được giữ. Frontend có vi-VN lifecycle UI/account cache, friend badge theo server và viewport queue25%/500ms/foreground/focus, chunk100, abort/retry guard. Cả Backend options và frontend build flags default OFF; không bật trên app hiện có.

Migration `20261108144829_NotificationTrashFriendSeen` sinh bằng EF10.0.12: hai nullable datetime2(7), no defaults/indexes, một UTC Pending/null backfill dưới maintenance write quiescence. Generated/NOT_APPLIED; SQL review-only NOT_EXECUTED; model/snapshot check PASS. Không sửa production DatabaseTarget/Program/design factory/migrations cũ/JWT/CSRF/retention. Build mới yêu cầu schema BCR, flags OFF không thay schema gate.

Kết quả cuối: **56 Backend unit/in-process HTTP PASS**, **58 Frontend unit/component PASS**, **14 BCR browser PASS**, **176 existing client browser PASS** ở server cô lập, production isolation/smoke1 PASS; lint/build/localization693/API128 consumers PASS. Tổng inventory129 HTTP gồm health,21 SignalR names; đây là SOURCE/client evidence, không API/SQL integration PASS. SQL test project BUILD PASS nhưng16 source cases NOT_RUN, Postman47 folders/104 requests NOT_RUN. 30 ảnh tổng hợp BCR,5 viewport × Light/Dark ×3 states. Lần regression đầu175 PASS/1 anchor FAIL được giữ lịch sử; focused và isolated full sau PASS, không sửa source Chat/threshold.

Files/evidence: [G2 report](docs/frontend/bcr-001-002-g2-report.md), [verification JSON](docs/frontend/bcr-001-002-g2-verification.json), [73-case ledger](docs/frontend/bcr-001-002-case-evidence.md), [screenshots](docs/frontend/bcr-001-002-screenshots.md), [failure history](docs/frontend/bcr-001-002-g2-failure-history.json), [G3 package](docs/frontend/bcr-001-002-g3-approval-package.md). File list đầy đủ trong verification JSON, gồm Backend21 source files, Frontend changes và test/SQL/Postman artifacts. Không commit/push, không đọc private uploads/dev-mail hoặc tự cấp Admin; không ghi SQL/apply/reset/seed DB.

**Blockers:** real SQL schema/history/backfill/provider/tie-order/races/locking; true JWT/refresh/CSRF/sessions, actual multi-account SignalR và business acceptance; exact isolated G3 target/backup/credentials/cleanup. MC-006 và phần friend unread MC-002 có SOURCE_FIX_NEEDS_REAL_VERIFICATION, không VERIFIED. Các số liệu ticket/client lịch sử bên dưới không nghiệm thu BCR mới.

**Next Exact Action:** Product Owner review source/G2 evidence và G3 Approval Package; định danh isolated SQL host/catalog/permission/fixtures/backup/cleanup rồi cấp G3 riêng nếu đồng ý. Agent dừng tại đây; không tự đề nghị G4/apply hoặc tiếp tục các module khác.

## BCR-001/BCR-002 — checkpoint technical validation 08/11/2026

**BUSINESS_APPROVED**

**TECHNICAL_REVIEW_PENDING**

**IMPLEMENTATION_NOT_AUTHORIZED**

Product Owner đã chốt D1–D6: recipient soft-delete, bulk chỉ thông báo đã đọc, Trash/Restore; old Pending friend requests backfill seen, mark-seen theo batch IDs thật sự visible, account-scoped SignalR + HTTP reconciliation. Không cần duyệt lại nghiệp vụ hoặc MeChat Hybrid. Phê duyệt kỹ thuật và quyền implementation/DB vẫn riêng biệt.

Đã đối chiếu source HEAD `ecb129989cb4271d05f669a4b712026f2e1200a2`, working diff bốn file controller/service/repository/interface Notifications phát sinh trong review (refactor, không đổi contracts/predicates đang xét), Entity/DbContext/EF configurations/hai migrations/snapshot/DTO/controller/service/repository, auth/session, SignalR, unread summary và test source. [Package trình duyệt](docs/frontend/bcr-001-002-implementation-approval-package.md) có 31 mục S01–S31, contracts/errors/file-level scope và 8 lựa chọn kỹ thuật T01–T08; [test/deployment/backup/rollback plan](docs/frontend/bcr-001-002-test-and-rollout-plan.md) có 73 case IDs duy nhất (47 Postman/HTTP/security/realtime, 12 unit/component, 14 SQL integration), tất cả chưa chạy.

Khác biệt quan trọng: Status của FriendRequest map nvarchar(max), candidate index chứa Status key không hợp lệ; StateEvents không tự thêm eventId; Block dùng PermissionsChanged; notification scan200 có thể trả empty+hasMore; MarkRead cần conditional write khi thêm delete để tránh race. DatabaseTarget khóa instance/catalog MeChat và tests hiện có ghi target mặc định/có seed Admin: phải có VM SQL cô lập hoặc test-only fixture được duyệt trước test ghi. Backfill khuyến nghị drain toàn bộ writers, một DB UTC instant, không timestamp cutoff hoặc rerun mỗi startup.

**Files do agent tạo/cập nhật:** baseline v1.0 addendum, backend-change-requests.md, bcr-001-002-implementation-approval-package.md, bcr-001-002-test-and-rollout-plan.md và roadmap này. Không sửa Backend/Frontend source, không hoàn tác working diffs Notifications của workspace; không tạo/chạy migration, SQL, test ghi hoặc commit/push. Kết quả lượt này là static source review và kiểm tra tài liệu, **không có runtime/API PASS mới**. Tất cả BCR test cases **PLANNED_NOT_RUN**; DB deployed schema/history/backup/scale/runtime **NEEDS_VERIFICATION**.

API coverage hiện vẫn 123 source endpoints/18 events của checkpoint trước; 6 endpoints và 3 event BCR là **PROPOSED, NOT_IMPLEMENTED**, không cộng vào coverage hiện có. MC-006 và phần Bạn bè MC-002 vẫn BLOCKED_BACKEND_APPROVAL ở gate implementation, **không còn business approval pending**. Các số liệu test/ticket ngày07 dưới đây được giữ là lịch sử, không phải nghiệm thu BCR mới.

**Next Exact Action:** Product Owner review T01–T08 và package, sau đó cấp G2 nếu đồng ý viết source/sinh migration file. G3 test DB apply/write, G4 real DB apply và G5 release cần quyền riêng. Theo yêu cầu hiện tại, agent dừng sau tài liệu; không tiếp tục implementation Frontend/Backend.

## Đợt bug fix theo workbook — checkpoint 07/11/2026

Đã đọc đủ 6 sheet, xem 12 ảnh gốc và lập [ledger đầy đủ 52 ticket](docs/frontend/improvement-progress.md), [JSON giữ nguyên acceptance](docs/frontend/improvement-tickets.json). Đây là bug fix/quality pass trong Phase 06, không xây lại Feature hoặc thay Phase. Workbook riêng tư giữ nguyên, không publish ảnh/nội dung chat gốc.

Baseline mới: lint/build PASS, unit33 PASS, browser132 PASS, Groups gốc13/13 PASS. Backend thật qua proxy: boundary4 PASS, auth1 SKIPPED (thiếu credentials, theo lựa chọn tiếp tục của người dùng). Không coi health hoặc client simulation là successful integration.

Đã sửa: image MIME/decode/error/retry/zoom + thumbnail authorized/lazy; URL invite một lần bị mất khi GroupChanged reset cache; server unread navigation; invitation CTA; privacy-safe presence; notification preview và toast local theo account; search context jump; role-null group guard; idle typing và message-row render. Giữ nguyên API/SignalR/business contracts.

Kết quả cuối: **40 unit/component PASS trong 11 tệp; 176 browser PASS / 0 FAIL / 0 flaky; 56 true browser zoom cases PASS; 57 ảnh tổng hợp mới.** Build/lint/localization 687/API 122/production isolation PASS, production smoke 1 PASS, actual anonymous boundaries 4 PASS + auth 1 SKIPPED. npm audit: 0 vulnerabilities. [Evidence](docs/frontend/improvement-verification.json), [ảnh](docs/frontend/improvement-screenshots.md). Browser integration vẫn là HTTP simulation; số đo 500 tin/17 trang không phải production load acceptance.

52 ticket gốc: **8 VERIFIED, 12 FIXED_NEEDS_VERIFICATION, 2 BLOCKED_BACKEND_APPROVAL, 29 BLOCKED_TEST_ENVIRONMENT, 1 NOT_REPRODUCIBLE**. Thêm 4 FND (3 VERIFIED, 1 FIXED_NEEDS_VERIFICATION); tổng 56, còn 44 chưa nghiệm thu. MC-011/012 vẫn chưa xác minh upload/storage/device thật; không gọi original image issue đã hoàn tất.

**BLOCKED_BACKEND_APPROVAL:** MC-006/BCR-001 Delete Notifications; phần unread Bạn bè của MC-002/BCR-002. [Đề xuất cụ thể](docs/frontend/backend-change-requests.md), chưa chỉnh Backend/DB/SignalR contracts. **BLOCKED_TEST_ENVIRONMENT:** business Auth/two-user/roles/storage/realtime/native-device acceptance. Không khôi phục các file người dùng đã xóa, không stage/commit/push.

Next Exact Action tại checkpoint07 (đã được checkpoint08 phía trên thay thế đối với BCR): Product Owner xem BCR-001/BCR-002; khi có test identities qua cấu hình an toàn, chạy Auth/session trước, rồi two-user friends/messages/groups/unread/presence/reconnect/files và Admin/appeal/retention theo quyền được cấp. [Live acceptance](docs/frontend/live-integration-acceptance.md) gồm cleanup plan, điều kiện native/device/NVDA/performance. Source tại checkpoint07 không còn thay đổi dang dở; không tự dùng credentials thật hoặc private uploads.

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

## Bằng chứng checkpoint trước — 06/11/2026

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

Người dùng đã chọn **tiếp tục, giữ đăng nhập thật ở NEEDS_VERIFICATION** khi thiếu credentials. Không hỏi lại hoặc đọc private uploads/dev-mail. Successful Auth/cookie/refresh/logout/profile/session, two-user friends/chat/requests/groups/realtime, file/storage authorization, reports/evidence/appeals/Admin/retention permissions, email delivery và demo thật chưa nghiệm thu. Native Chrome/Edge, screen reader, thiết bị mobile/keyboard/IME/autofill, full WCAG/performance/load vẫn NEEDS_VERIFICATION. [Backend constraints](docs/frontend/backend-gaps.md); BCR-001/002 đã BUSINESS_APPROVED, technical/implementation gates còn khóa như checkpoint08.

## Files và an toàn workspace

Source: frontend/src/i18n, features/auth/users/friends/conversations/messages/groups/files/notifications/moderation/admin; shared forms/layout/chat/Avatar; app routing/providers/guards; lib clients; CSS tokens. Checks: frontend/tests, Playwright configs, npm scripts; API census generators và docs/frontend. README/spec/acceptance/design-review/coverage/verification/final-acceptance/screenshot index đã cập nhật, lịch sử lưu ở docs/frontend/history.

Không sửa Backend/Database, migrate/seed hoặc thực hiện destructive QA. Không tạo commit. Giữ nguyên các xóa có trước của người dùng: REFACTOR_STATUS.md, STATIC_AUDIT.json. Backend chạy 5080 từ build hiện có với cả FileRetention/RetentionMaintenance scheduled/deletion/purge false. Chỉ boundary requests an toàn được chạy với Backend thật.

## Next Exact Action và recovery

**Ưu tiên hiện tại: checkpoint BCR technical validation08/11 phía trên. Không chạy các lệnh implementation/test cũ dưới đây trong lượt review tài liệu.** PO phải duyệt kỹ thuật/cấp quyền riêng trước code hoặc ghi test DB; không chạy bộ Backend tests vào DefaultConnection.

Khi cấu hình kiểm thử an toàn sẵn sàng: chạy successful Auth/session lifecycle trước, rồi profile/privacy, hai-user Friends/chat/request/group/reconnect/media và role Admin/appeal/retention; bổ sung native/device/performance/demo. Chưa có quyền ghi Backend/DB hoặc dùng dữ liệu riêng tư để tự lấy token. Không khôi phục Design Approval block.

Phiên sau đọc AGENTS.md nếu có, file này, implementation-contract và verification-report; kiểm tra git status và source trước khi code. Source clients đã có, không viết lại. Dùng npm.cmd vì npm.ps1 bị execution policy chặn. Các lệnh: npm.cmd --prefix frontend run lint; run audit:localization; run audit:api; test; run test:e2e; run build; run verify:production; run test:e2e:production; run test:e2e:live. Static census: python scripts/frontend-inventory.py, kiểm tra evidence timestamp trước khi dùng kết quả lịch sử.


Bằng chứng bổ sung cuối: secure-auth-input-client.json **1 PASS** với dữ liệu giả lập; actual account vẫn SKIPPED. final-message-media-regression.json **29 PASS** sau khi giới hạn sanitization vào message pages, giữ nguyên Blob và xóa reply/detail/history phụ thuộc. Các lần chạy tập trung trùng một phần với full128; không cộng thành số ca duy nhất. Unit cuối **33 PASS trong 9 files**.
