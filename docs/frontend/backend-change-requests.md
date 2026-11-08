# Backend Change Requests — 08/11/2026, G2 source handoff

**BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. Product Owner đã duyệt D1–D6, T01–T08 và G1/G2 theo prompt cuối. **G3/G4/G5 NOT_AUTHORIZED**. Không xin duyệt lại cùng nghiệp vụ/technical direction; không apply DB hoặc bật runtime features.

Trạng thái code/evidence hiện tại: [G2 report](bcr-001-002-g2-report.md), [verification](bcr-001-002-g2-verification.json), [73-case ledger](bcr-001-002-case-evidence.md), [G3 approval package](bcr-001-002-g3-approval-package.md). [Baseline v1.0](MeChat_BCR001_BCR002_Technical_Design_Baseline_v1.md), [technical package](bcr-001-002-implementation-approval-package.md) và [test/rollout plan](bcr-001-002-test-and-rollout-plan.md) giữ lịch sử đối chiếu HEAD; quyết định cuối của PO ưu tiên.

## BCR-001 — Notification Lifecycle

- Related Tickets: **MC-006, QA-023**.
- Approval: **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. Source đã triển khai; ticket chưa VERIFIED, cần G3/backend thật.
- Backend đã có DeletedAt nullable datetime2(7), DTO Trash riêng, active filters và conditional writes. Giữ refactor controller/service/repository/interface trước đó.
- Contracts: DELETE `/api/notifications/{id:guid}`; DELETE `/api/notifications/read` **chỉ active-read**; GET `/api/notifications/trash`; POST `/api/notifications/{id:guid}/restore`. Mutations204; single unknown/alien/invisible404; owned visible replay204. Active API routes/status/DTO shape giữ nguyên.
- T01: actor từ validated claims, recipient+current visibility trước replay; bulk gồm hidden thuộc actor nhưng chỉ ReadAt non-null/DeletedAt null, không trả IDs/count. Không xóa resource nguồn/audit/evidence.
- T02: CreatedAt/Id DESC keyset tại SQL; Trash validate cặp before/beforeId, bounded200, empty+hasMore hợp lệ/cursor tiến; active legacy không siết validation.
- T05: read/delete/restore dùng conditional UPDATE, không stale tracked SaveChanges; MarkRead zero-affected không trả success nếu item vừa restore mà vẫn unread. Restore giữ ReadAt/CreatedAt. Không RowVersion/ETag/exactly-once.
- SignalR: NotificationDeleted `{eventId,scope:Single|ReadBatch,notificationId:guid|null}`; NotificationRestored `{eventId,notificationId}`. Caller ID, actor account only, sau effective committed statement, HTTP reconciliation.
- Frontend: Thùng rác/Xóa/Khôi phục và confirmation **Xóa thông báo đã đọc**, account-scoped feed/count/cache, precise cursor strings, loading/error/empty/success, vi-VN/Light/Dark/responsive. Không Mark Read giả Delete hoặc fake counter.
- Migration: `20261108144829_NotificationTrashFriendSeen` GENERATED/NOT_APPLIED; no defaults/indexes. Không hard-delete/auto-purge. Schema cutover là prerequisite chạy compatible Backend mới, kể cả flags OFF.
- Runtime acceptance: **BLOCKED_G3 / NEEDS_VERIFICATION**, không coi unit/HTTP simulation là successful SQL integration.

## BCR-002 — Friend Request Unread

- Related Tickets: phần Bạn bè của **MC-002, QA-005, QA-023**.
- Approval: **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. Source đã triển khai; ticket chưa VERIFIED, cần G3/backend thật.
- Backend đã có SeenAt nullable datetime2(7), GET `/api/friends/requests/unread-count` → `{unreadCount:n}`, POST `/api/friends/requests/mark-seen` →204. `/api/unread.requestCount` vẫn là Message Requests.
- T03: raw1–100 IDs, validate whole request, dedupe; reject missing/null/empty/malformed/Guid.Empty, local8KiB. Update receiver actor+Pending+SeenAt null; ignore alien/outgoing/unknown/non-Pending/already-seen, không IDs/count response. GET không ghi.
- T04: Incoming SeenAt thật, Outgoing luôn null; không sender receipts. Không đổi Status/RespondedAt hoặc send/reverse accept/respond/cancel/block business rules.
- Realtime: đúng FriendRequestsUnreadChanged `{eventId}` tới affected receiver accounts sau commit; gồm create/reverse/respond/cancel/block/mark-seen; giữ FriendshipChanged/PermissionsChanged và18 events cũ. HTTP count là truth, không cộng trừ badge từ event.
- Frontend: dedicated server badge, tách Pending tab counts; viewport25% liên tục500ms, foreground/focused, tall-row equivalent; chunks100, account queue/dedupe/inflight guard, abort/clear logout/identity, visibility recheck trước explicit retry. Badge chỉ refetch sau confirmation.
- Backfill: một DB UTC instant cho historical Pending+SeenAt null, maintenance full writer quiescence; không CreatedAt cutoff hoặc startup/toggle backfill. Pending mới null kể cả flag OFF. Non-Pending không đổi.
- Indexes: không thêm; không dùng Status nvarchar(max) làm key, không đổi Status type/length hoặc pair uniqueness.
- Runtime acceptance: **BLOCKED_G3 / NEEDS_VERIFICATION**.

## Flags, tests và quyền còn khóa

Hai BcrFeatures options và hai VITE_BCR counterparts mặc định **false**. G2 chỉ bật frontend flags trong simulation server5183/proxy9; không thay runtime settings hiện có. Backend options dùng IOptions, rollout/kill switch qua compatible restart, không tự bật lúc code xong.

SQL test project `MeChat.Bcr.SqlTests` có allowlist/fail-closed guard, catalog riêng `MeChat_BcrTest_*`, không fallback MeChat/DefaultConnection. Production DatabaseTarget/design-time factory giữ nguyên. Chỉ build SQL tests; không chạy. [Postman](postman/MeChat-BCR-001-002.postman_collection.json) có47 folders/104 requests, secrets để trống, NOT_RUN.

**Next Exact Action:** PO review G2 source/results và [G3 manifest](bcr-001-002-g3-approval-package.md) để quyết định exact isolated target/fixtures/backup/cleanup/quyền apply-test riêng. Không tự cấp G3, không đề nghị G4 trước G3 evidence.

## Lịch sử đã được thay thế

Đề xuất07/11 còn pending business approval và cân nhắc DELETE `/api/notifications` xóa tất cả, soft/hard-delete, pending/unread. D1–D6 đã thay thế các đề xuất đó; T01–T08/G1/G2 đã duyệt08/11. Không triển khai delete-all-unread hoặc yêu cầu PO chốt lại. Review trước G2 chỉ sửa tài liệu; G2 hiện được phép sửa source/sinh migration, chưa được ghi DB.

Các quyết định đợt bug fix trước giữ nguyên: MC-005 token invite chỉ trả lúc tạo; MC-008 Online nullable privacy; MC-009 toast preference local theo account; MC-011/012 authorized Blob endpoint cần test account/file được cấp phép, chưa có bằng chứng storage lỗi để tự sửa Backend.
