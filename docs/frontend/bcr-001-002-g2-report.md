# BCR-001/BCR-002 — G2 Implementation Report

Ngày: **2026-11-08**, Asia/Bangkok. Base HEAD: `ecb129989cb4271d05f669a4b712026f2e1200a2`; working refactors Notifications đã có được giữ và mở rộng tại chỗ.

**BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. T01–T08 và G1/G2 do Product Owner duyệt. **G2 COMPLETE — SAFE_BOUNDARY_ONLY** tại checkpoint này. **G3/G4/G5 NOT_AUTHORIZED**. Không commit/push, apply migration, mở SQL để kiểm tra dữ liệu hoặc chạy SQL/Postman integration.

## 1. Backend đã triển khai

| Phạm vi file dưới backend/MeChat.Api | Nội dung |
| --- | --- |
| Features/Notifications/Models/Notification.cs | DeletedAt nullable |
| Features/Notifications/DTOs/NotificationDtos.cs | Trash item/page riêng; active DTO giữ nguyên |
| Features/Notifications/Controllers/NotificationsController.cs | 4 routes mới; flags trước binding, safe404/validation; legacy active validation giữ nguyên |
| Features/Notifications/Services/NotificationService.cs | Ownership + visibility trước replay; bounded scan200/cursor; conditional read/delete/restore, metadata events sau statement commit |
| Features/Notifications/Repositories/NotificationRepository.cs; Repositories/Interfaces/INotificationRepository.cs | Active predicates cho page/count/read/read-all; no-tracking snapshots; conditional writes, bulk một UPDATE chỉ active-read |
| Features/Notifications/DependencyInjection.cs | Bind BcrFeatures options, default OFF |
| Features/Friends/Models/FriendRequest.cs | SeenAt nullable, no current-time default |
| Features/Friends/DTOs/FriendDtos.cs | Optional SeenAt, count DTO, raw1–100 batch validation/dedupe |
| Features/Friends/Controllers/FriendsController.cs | Count + mark-seen, 8KiB body metadata; no-store response; không trả affected IDs/count |
| Features/Friends/Services/FriendService.cs | Conditional seen orchestration; create/reverse/respond/cancel/block invalidation đúng receiver, sau commit/retry callback cleanup |
| Features/Friends/Repositories/FriendRepository.cs; Repositories/Interfaces/IFriendRepository.cs | Receiver+Pending+unseen COUNT/UPDATE; Incoming marker thật, Outgoing luôn null |
| Features/Friends/DependencyInjection.cs | Bind options; existing workflow giữ nguyên khi OFF |
| Configurations/BcrFeatureOptions.cs | Hai bool mặc định false, không sửa appsettings/runtime environment |
| Configurations/BcrInputValidationAttribute.cs | Resource-level flags, local sanitized validation, safe domain ProblemDetails; không đổi global auth/error middleware |
| Infrastructure/Persistence/Configurations/Notifications/NotificationConfiguration.cs; Configurations/Friends/FriendRequestConfiguration.cs | Precision7, không thêm/drop index hoặc đổi Status type |
| Infrastructure/Persistence/Migrations/20261108144829_NotificationTrashFriendSeen.cs + Designer; AppDbContextModelSnapshot.cs | EF-generated columns/model; backfill chỉnh trong migration mới, không sửa migration cũ |

Program, DatabaseTarget, design-time factory, JWT/CSRF/refresh/session contracts, FK/PK/pair uniqueness và retention source không đổi. StateEvents hiện có được reuse, không cần sửa hub/publisher. Các comment/formatting/method refactor Notifications của người dùng được bảo toàn; chỉ đổi behavior theo BCR được duyệt.

## 2. API và SignalR coverage

| HTTP mới | Implemented source | Integration acceptance |
| --- | --- | --- |
| DELETE /api/notifications/{id:guid} | Owned visible active/trash;204 hoặc generic404 | BLOCKED_G3 |
| DELETE /api/notifications/read | Actor + ReadAt non-null + DeletedAt null;204 | BLOCKED_G3 |
| GET /api/notifications/trash | Separate DTO, paired cursor, scan200, empty+hasMore hợp lệ | BLOCKED_G3 |
| POST /api/notifications/{id:guid}/restore | Giữ ReadAt/CreatedAt;204 hoặc generic404 | BLOCKED_G3 |
| GET /api/friends/requests/unread-count | unreadCount riêng; không dùng message requestCount | BLOCKED_G3 |
| POST /api/friends/requests/mark-seen | Validated raw1–100; receiver+Pending+null update;204 | BLOCKED_G3 |

Inventory: **129 HTTP endpoints = 128 controller actions + health**, 128 existing JS functions/consumers mapped. Static coverage không phải runtime PASS. New errors chỉ INVALID_REQUEST_IDS, INVALID_NOTIFICATION_CURSOR, NOTIFICATION_UNAVAILABLE, BCR_FEATURE_DISABLED. Binding errors mới không echo submitted values. Host-level413 và actual429/auth middleware responses còn cần G3 kiểm chứng.

Thêm đúng **NotificationDeleted**, **NotificationRestored**, **FriendRequestsUnreadChanged** theo approved payloads; tổng21 events, 18 cũ giữ nguyên. Caller sinh eventId, account-scoped metadata; no-op không bắt buộc event mới, không exactly-once/outbox. Frontend invalidation + focus/reconnect/online/foreground HTTP sync. Actual authenticated multi-session fanout/lost-event repair chưa chạy.

## 3. Frontend files và behavior

- `features/notifications/api.js`, `use-notifications.js`, `NotificationsPage.jsx`: lifecycle adapters, account-scoped feed/count keys, precise cursor strings; Thùng rác/Khôi phục/Xóa/Xóa thông báo đã đọc; confirmation, loading/error/empty/success, no optimistic fake deletion. Preview hiện có dùng hook chung nên được invalidate/refetch cùng feed.
- `features/friends/api.js`, `use-friend-unread.js`, `seen-queue.js`, `use-request-seen.js`, `FriendsPage.jsx`: dedicated count, viewport queue25% liên tục500ms, tall-row threshold theo viewport, focused/foreground; chunk100, dedupe/inflight guard, explicit retry revalidates visibility; clear/abort identity/logout. Pending tab count không giảm vì seen.
- `components/layout/NavigationLinks.jsx`, `app/providers/RealtimeProvider.jsx`: badge lấy server count riêng và 3 event mới; không cộng/trừ từ event hoặc số row đang render.
- `lib/bcr-features.js`, `lib/errors.js`, i18n `vi/{notifications,friends,errors}.json`, `styles/index.css`: build switches default OFF, safe Vietnamese errors/copy, toolbar responsive/tokens hiện có. MeChat Hybrid giữ nguyên.
- `scripts/frontend-inventory.py`: thêm 6 adapter/consumer mappings và extract DTO signature có helper body đúng; regenerate inventory/contracts/maps. `.gitignore` chỉ thêm local `.artifacts/` build outputs.

Hai Backend options `BcrFeatures:NotificationLifecycleEnabled`, `BcrFeatures:FriendRequestSeenEnabled` default false. Frontend build counterparts `VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED`, `VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED` cũng default false. **Không bật trên runtime đang sử dụng**. IOptions được đọc theo app lifetime; rollout/kill switch Backend cần compatible restart theo manifest, FE cần approved rebuild, không có capability API hoặc live toggle UI giả.

**Schema gate bắt buộc:** Backend build mới luôn đọc/lọc DeletedAt và project SeenAt, kể cả flags OFF. Không chạy/deploy build mới lên schema cũ để thử tính năng. Flags không thay thế schema cutover. Không thêm fallback che SQL lỗi.

## 4. Migration safety review

[Migration](../../backend/MeChat.Api/Infrastructure/Persistence/Migrations/20261108144829_NotificationTrashFriendSeen.cs) sinh bằng EF10.0.12; [SQL review artifact](sql/bcr-001-002-review-only.sql) sinh offline, **NOT_APPLIED / NOT_EXECUTED**.

- Up có hai AddColumn nullable datetime2(7), no defaults và một SqlOperation transaction-enabled. Một database UTC instant được capture, chỉ old Pending + SeenAt null backfill. Non-Pending không đổi; pending mới luôn null. Không CreatedAt cutoff/startup backfill/toggle backfill.
- Backfill dùng constant `EXEC(N'...')` để hoãn compile reference tới cột vừa thêm khi xuất script; không nhận input người dùng. Đây là thiết kế phòng parser/batch issues, không phải SQL-runtime PASS. [Microsoft guidance về EXEC trong migration scripts](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing) là cơ sở kỹ thuật; rehearsal thực vẫn BLOCKED_G3.
- Không indexes mới, đặc biệt không Status nvarchar(max) key. Không đổi FK/PK/pair unique hoặc migration lịch sử. Snapshot có reorder metadata do EF generator; Up không có schema operations ngoài scope.
- Down drop hai cột sẽ mất Trash/seen state; **không dùng hot rollback sau feature traffic**. Giữ compatible BE + flags off, không quay BE cũ làm Trash xuất hiện lại. Chưa chạy Down/backup/restore.
- Chỉ maintenance write quiescence sau G3 riêng; thời gian lock/log/volume, schema/history deployed và restore safety chưa xác minh. Không tự hứa downtime/RPO/RTO.

## 5. Tests/artifacts và kết quả thực tế

| Check | Kết quả tại checkpoint |
| --- | --- |
| Backend build BcrG2 | PASS, 0 warnings/errors |
| `MeChat.Bcr.UnitTests` | **56 PASS / 0 FAIL / 0 SKIP**; gồm service doubles và19 in-process HTTP cases với test auth; không DbContext/SQL/DefaultConnection |
| `MeChat.Bcr.SqlTests` | BUILD PASS; **16 executable cases NOT_RUN / BLOCKED_G3**; guard/fixture/DI helper fail-closed |
| Frontend unit/component | **58 PASS / 0 FAIL**,13 files; gồm identity loss/error scope guards đã sửa |
| BCR Playwright | **14 PASS / 0 FAIL**; server5183/proxy9, HTTP simulation; modal+empty+active, 5 widths × Light/Dark, focus/Escape/axe/no overflow/page errors |
| Existing full Playwright, initial run | **175 PASS / 1 FAIL**; 500-message anchor23px >3px; giữ failure history |
| Isolated focused performance case | **1 PASS**, giữ nguyên source Chat và ngưỡng; không coi là full176 PASS |
| Isolated full176 rerun | **176 PASS / 0 FAIL / 0 SKIP / 0 flaky** ở final isolated run; first failure vẫn giữ lịch sử |
| Frontend lint/build | PASS sau final source guards; kết quả/timestamp trong verification JSON |
| Localization/API static audit | PASS693 calls /128 controller consumers; không runtime integration |
| Production fixture isolation / browser smoke | PASS /1 PASS sau final build |
| EF has-pending-model-changes | PASS: model và snapshot đồng bộ; không đọc DB history |
| Postman |47 folders/104 requests, **NOT_RUN / BLOCKED_G3**; auth/ownership/invalid/replay/concurrency notes; no secrets |

Kết quả cuối và timestamp trong [verification JSON](bcr-001-002-g2-verification.json); [failure history](bcr-001-002-g2-failure-history.json) không bị đổi thành PASS. [73-case ledger](bcr-001-002-case-evidence.md) giữ47 P +14 I cases blocked cho full real-backend acceptance;12 U groups có safe evidence. Không cộng các group thành executable count hoặc nhận SQL PASS từ HTTP simulation.

Safe commands đã dùng (không lệnh database update):

```text
dotnet build backend/MeChat.Api/MeChat.Api.csproj --configuration BcrG2 --no-restore -p:UseAppHost=false
dotnet ef migrations add NotificationTrashFriendSeen --project backend/MeChat.Api/MeChat.Api.csproj --configuration BcrG2 --no-build
dotnet test backend/MeChat.Bcr.UnitTests/MeChat.Bcr.UnitTests.csproj --configuration BcrG2 -p:UseAppHost=false --logger trx
dotnet build backend/MeChat.Bcr.SqlTests/MeChat.Bcr.SqlTests.csproj --configuration BcrG2 -p:UseAppHost=false
dotnet ef migrations script 20261028101250_AuthLockoutFields 20261108144829_NotificationTrashFriendSeen --project backend/MeChat.Api/MeChat.Api.csproj --configuration BcrG2 --no-build --output docs/frontend/sql/bcr-001-002-review-only.sql
dotnet ef migrations has-pending-model-changes --project backend/MeChat.Api/MeChat.Api.csproj --configuration BcrG2 --no-build
python scripts/frontend-inventory.py
npm.cmd --prefix frontend test
npm.cmd --prefix frontend run lint
npm.cmd --prefix frontend run build
npm.cmd --prefix frontend run audit:api
npm.cmd --prefix frontend run audit:localization
npm.cmd --prefix frontend run verify:production
npm.cmd exec -- playwright test --config playwright.bcr.config.js
npm.cmd exec -- playwright test --config playwright.g2-regression.config.js
npm.cmd --prefix frontend run test:e2e:production
```

Playwright exec commands chạy trong frontend cwd. Chỉ pure Backend test project được thực thi; không chạy nguyên backend/MeChat.Tests hoặc SQL project. In-process host dùng test auth, không dùng Program/real JWT; constructor doubles không gọi DB. Test-only flags được bật trên simulation server5183 với proxy127.0.0.1:9; không bật Backend/current app. Regression server5184 cũng proxy9, flags OFF, không reuse app người dùng.

## 6. Security/concurrency findings và fixes

- MarkRead chuyển khỏi tracked SaveChanges sang conditional UPDATE. Affected0 chỉ replay success nếu active row thực sự đã read; delete/restore xen giữa không trả read success cho item còn unread.
- Single lifecycle kiểm ownership + current visibility trước cả replay; hidden message test dùng real visibility orchestration với repository double trả message unavailable. Bulk cố ý gồm hidden read-owned, không có counts/IDs response.
- Seen update không ghi Status/RespondedAt; existing friend serializable transactions/retry scope giữ nguyên. Capture affected receivers trước mutation, include Block/PermissionsChanged. SQL deadlock/outcome ordering vẫn cần G3.
- Sanitized local binding failures không echo submitted IDs/body. Feature guard trước binding. Extra receiver fields không quyết định actor.
- Query keys account-scoped cho BCR data; queue abort/logout/identity guard, in-flight dedupe; lỗi cũ không có retry giả khi incoming view inactive. Null failure state lúc logout đã được thêm test và sửa.
- Không sửa source nghiệp vụ gốc/audit/moderation/retention, không hard-delete notification, không seed Admin hoặc đọc private uploads/dev-mail. Không nhận exactly-once/atomic multi-table authorization ngoài scope.

## 7. Visual evidence

[Screenshot index](bcr-001-002-screenshots.md):30 screenshots tổng hợp, active/modal/Trash ở320/390/768/1366/1440 Light/Dark. Đã nhìn trực tiếp mobile320 light và desktop1440 dark modal; modal capture chờ animation settle. Browser axe không thay thế full WCAG/device/screen-reader acceptance.

## 8. Remaining blockers / Next Exact Action

Chưa có G3 exact SQL target, backup/history/maintenance/test identities/HTTP test-host manifest. Actual migration/backfill, conditional SQL provider translation/tie-order, real races/deadlocks, true JWT/refresh/CSRF/revocation, A/A2/B/C SignalR và real storage/business regression còn **BLOCKED / NEEDS_VERIFICATION**. Không tuyên bố MeChat hoặc61 integration-plan cases đã hoàn tất.

[G3 Approval Package](bcr-001-002-g3-approval-package.md) đã chuẩn bị manifest, allowlist/fixture restrictions và cleanup requirements. Safe regression cuối đã xong; agent dừng cho PO review source và quyết định G3; không xin lại G1/G2, không tự đề nghị G4 hoặc apply DB.

## 9. G2 Definition of Done và kết luận

Backend/Frontend source, migration chưa apply, contracts,3 events, safe checks cuối, SQL/Postman artifacts, diff/migration review và G3 package đã có bằng chứng trong phạm vi G2. **G2 COMPLETE — SAFE_BOUNDARY_ONLY**; không có current safe-test FAIL. Source/test projects tách riêng và flags default OFF. Source readiness không phải DB migration acceptance hoặc release approval. Còn61 P/I groups BLOCKED_G3 theo ledger, không gọi cả MeChat hoàn thành100%.
