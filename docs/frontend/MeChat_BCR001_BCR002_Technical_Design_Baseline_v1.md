# MeChat — Technical Design Baseline v1.0

> Quyết định cuối của Product Owner — 2026-11-08: **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED (G2)**. T01–T08 đã được duyệt; visibility tối thiểu 25% liên tục 500ms, foreground + focused, thích nghi row cao hơn viewport. G3/G4/G5 **NOT_AUTHORIZED**: không apply migration, không ghi/test SQL, không bật runtime flags. Các mô tả pending/read-only trong bản review bên dưới là lịch sử; prompt phê duyệt cuối ưu tiên. Hai flags mặc định OFF; index mới không được thêm. Xem roadmap và báo cáo G2 để biết tiến độ thực tế.


**Ngày:** 2026-11-08  
**Phạm vi:** BCR-001 (xóa/khôi phục thông báo), BCR-002 (badge lời mời kết bạn chưa xem)  
**BUSINESS_APPROVED**

**TECHNICAL_APPROVED**

**SOURCE_IMPLEMENTATION_AUTHORIZED**

**Review lịch sử trước G2 chỉ cập nhật tài liệu. G1/G2 hiện đã được PO duyệt; source/migration/tests và kết quả hiện tại xem [G2 report](bcr-001-002-g2-report.md). Database/release chưa được phép.**

**Addendum review HEAD — 2026-11-08:** đã đối chiếu source tại `E:\Learn\MeChat`, HEAD `ecb129989cb4271d05f669a4b712026f2e1200a2` và working diff controller/service/repository/interface Notifications phát sinh trong lúc review (refactor, không đổi contracts/predicates đang xét). Xem [Implementation Approval Package](bcr-001-002-implementation-approval-package.md) và [test/deployment/backup/rollback plan](bcr-001-002-test-and-rollout-plan.md). D1–D6 giữ nguyên. T01–T08 là đề xuất để PO chốt; không coi các chi tiết bổ sung là đã được duyệt. Khi có khác biệt, dùng addendum mục12 và package hiện tại cho lượt duyệt kỹ thuật; phần snapshot là lịch sử provenance.

## 1. Căn cứ và mức độ xác minh

- **Quyết định của Product Owner trong cuộc trò chuyện hiện tại** là nguồn chuẩn cho 6 chính sách nghiệp vụ dưới đây.
- Đối chiếu `backend-change-requests.md`, `roadmapFE(3).md`, `improvement-progress.md` và `live-integration-acceptance.md` được cung cấp.
- Đã kiểm tra trực tiếp **snapshot nguồn** `MeChat(4).zip` được đính kèm trước đó: C# entities, Controllers, Services, Repositories, EF mappings, `AppDbContext`, migrations và `StateEvents`. Snapshot **không được chứng minh là HEAD hiện tại tại `E:\Learn\MeChat`**. Trước khi thực thi, Codex phải đọc lại HEAD và phân tích `git diff`; nếu khác, cập nhật tài liệu và xin duyệt phạm vi thay đổi.
- **Không có truy cập live SQL Server/DB tại máy người dùng**; không xác nhận số lượng record, query plan, trạng thái migrations mới nhất hoặc backup.

### 1.1. Sáu quyết định đã chốt

| ID | Quyết định | Trạng thái |
|---|---|---|
| D1 | BCR-001: soft-delete thông báo theo người nhận | Chốt |
| D2 | BCR-001: xóa hàng loạt **chỉ thông báo đã đọc** | Chốt |
| D3 | BCR-001: có **Thùng rác và Khôi phục** | Chốt |
| D4 | BCR-002: pending cũ được xem là đã xem sau migration | Chốt |
| D5 | BCR-002: mark-seen theo **batch IDs thực sự đã được người nhận xem** | Chốt |
| D6 | Cả hai: **event SignalR account-scoped + HTTP reconciliation** | Chốt |

**Không diễn giải D2 thành “xóa tất cả kể cả chưa đọc”.** Trong UI có thể có “Xóa thông báo đã đọc” và “Xóa một thông báo” riêng biệt. Không coi mark-read và delete là cùng hành động.

## 2. Đối chiếu source đã kiểm tra

| Thành phần | Điều đã xác nhận từ snapshot | Tác động |
|---|---|---|
| `Features/Notifications/Models/Notification.cs` | `Id`, `RecipientUserId`, `ActorUserId`, `Type`, `ReferenceId`, `CreatedAt`, `ReadAt`; chưa có `DeletedAt` | Thêm `DateTime? DeletedAt` vào chính entity Notification; mỗi record có một recipient |
| `Features/Friends/Models/FriendRequest.cs` | `Id`, `SenderUserId`, `ReceiverUserId`, `PairUserId1/2`, `Status`, `CreatedAt`, `RespondedAt`; chưa có `SeenAt` | Thêm `DateTime? SeenAt` |
| `NotificationConfiguration.cs` | Index `(RecipientUserId, CreatedAt)`; FK người nhận | Giữ index hiện tại trừ khi kế hoạch SQL chứng minh index mới hữu ích |
| `FriendRequestConfiguration.cs` | Unique filtered `(PairUserId1, PairUserId2)` WHERE `Status='Pending'` | Không đổi uniqueness; kiểm tra chỉ số mới có cần không |
| `AppDbContext.cs` | `DbSet<Notification> Notifications`, `DbSet<FriendRequest> FriendRequests`; ApplyConfigurationsFromAssembly, `UtcDateTimeConvention` | Migrate cùng context hiện hữu |
| Migrations | `20261028101015_InitialSchema`, `20261028101250_AuthLockoutFields` trong snapshot | Kiểm tra HEAD và history live trước tạo migration mới |
| `NotificationsController` | GET list/count, PATCH `{id}/read`, POST `read-all`; chưa có delete/restore/trash | Bổ sung endpoint, không thay HTTP contracts cũ |
| `FriendsController` | GET requests trả `FriendRequestsDto(Incoming,Outgoing)`; POST send/accept/reject/cancel | Thêm count/mark-seen; mở rộng `FriendRequestDto` tương thích |
| `NotificationRepository` | `GetNotificationPageAsync` theo `(CreatedAt DESC, Id DESC)`, query unread, get-for-update, bulk `MarkAllReadAsync` | Mọi query active phải lọc DeletedAt NULL; query trash lọc != NULL; cursor ổn định |
| `FriendRepository` | Incoming/Outgoing chỉ lọc Pending; DTO chưa có SeenAt | Đếm Pending AND SeenAt NULL; chỉ receiver có quyền mark-seen |
| `NotificationService` | `VisibleAsync` lọc quyền truy cập; `MarkAllReadAsync` hiện bao gồm cả notification bị ẩn do permission; phát `NotificationRead` | Quy tắc mới không được vô tình phá logic hiện có; soft-delete phải loại khỏi cả active count/read-all |
| `StateEvents` | `SendAsync(recipients,event,payload)` dùng `hub.Clients.User`; **caller phải thêm eventId**, SendAsync không tự sinh; `FriendshipChanged`, `NotificationRead` được gọi từ Services | Sửa mô tả snapshot sau review HEAD; account-scoped publisher sẵn có, thêm event mới sau commit, payload chỉ metadata |

**Lưu ý đặc biệt:** hiện tại `NotificationService.ListAsync` đọc một batch 200 record rồi lọc `VisibleAsync`, tính `HasMore` theo batch. Khi thêm Trash, cần kiểm thử cursor/hasMore để tránh trang trống hoặc lặp, đặc biệt khi nhiều record bị ẩn theo permission. Đừng chỉ thêm một điều kiện ở UI.

## 3. BCR-001 — Notification soft-delete và Trash/Restore

### 3.1. Trạng thái và chính sách

- **ACTIVE_UNREAD**: `DeletedAt == null && ReadAt == null`.
- **ACTIVE_READ**: `DeletedAt == null && ReadAt != null`.
- **TRASH_UNREAD**: `DeletedAt != null && ReadAt == null`.
- **TRASH_READ**: `DeletedAt != null && ReadAt != null`.
- Xóa 1 item: hỗ trợ cả read và unread, sau thao tác đưa vào Trash; unread count giảm nếu trước đó unread. **Xóa hàng loạt**: chỉ ACTIVE_READ chuyển vào Trash; ACTIVE_UNREAD không bị ảnh hưởng.
- Khôi phục: chỉ khôi phục item thuộc người gọi, đang ở Trash; **giữ nguyên ReadAt**. Vì vậy khôi phục item trước đó unread làm badge tăng lại theo server truth nếu item vẫn đủ điều kiện visibility.
- Thùng rác là **view riêng** với phân trang; các endpoint read/read-all/list/count mặc định chỉ thấy ACTIVE.
- Xóa notification không xóa message, invitation, friend request, admin audit hoặc moderation evidence nguồn. Restore không khôi phục quyền truy cập tới resource đã bị block/recalled/deleted. Chỉ trả dữ liệu phù hợp với kiểm tra `VisibleAsync`.
- **Chưa duyệt xóa vĩnh viễn/auto-purge.** Không tạo endpoint hard-delete, scheduler hoặc retention job mới. Chính sách lưu trữ lâu dài là điểm cần quyết định riêng khi có nhu cầu; tránh tự ý tái dùng retention purge của Admin.

### 3.2. API mới — đề xuất cho phê duyệt

| Method | Route | Thành công | Chính sách |
|---|---|---|---|
| `DELETE` | `/api/notifications/{id:guid}` | `204` | Soft-delete **một** notification thuộc người nhận, kể cả unread |
| `DELETE` | `/api/notifications/read` | `204` | Soft-delete hàng loạt **chỉ ACTIVE_READ** của actor |
| `GET` | `/api/notifications/trash?before=...&beforeId=...&limit=30` | `200` | Trash paginated, DTO trang tương tự list; không bao gồm item không còn được phép xem |
| `POST` | `/api/notifications/{id:guid}/restore` | `204` | Khôi phục từ Trash, giữ ReadAt, kiểm tra quyền sở hữu |

**Routing:** `DELETE /api/notifications/read` phải có route literal để tránh xung đột với `{id:guid}` (route constraint loại trừ `read`). Không dùng `DELETE /api/notifications` cho bulk trong bản này vì dễ gây hiểu nhầm “xóa mọi thông báo”.

**HTTP semantics dự kiến:** không có JWT -> 401; ID không tồn tại/không thuộc actor -> 404; xóa lặp cùng ID thuộc actor đã xóa -> 204; restore lặp từ trạng thái ACTIVE -> 204 nếu vẫn thuộc actor; invalid UUID route -> 404. Idempotency không tiết lộ đối tượng của tài khoản khác. Với trường hợp VisibleAsync false, chính sách list/trash/redaction và status của action phải được kiểm tra thống nhất để không tiết lộ resource; đề xuất 404 đối với thông báo mà actor không được phép thao tác theo visibility rule.

**DTO:** giữ nguyên `NotificationDto(Id,Type,ReferenceId,CreatedAt,ReadAt,Text)` ở active list; định nghĩa riêng `TrashedNotificationDto` bao gồm `DeletedAt` (và các field đã được lọc quyền). `NotificationPageDto` cũ không bị thay đổi shape. Mẫu response trash:

```json
{
  "items": [{
    "id": "11111111-1111-1111-1111-111111111111",
    "type": "Message",
    "referenceId": null,
    "createdAt": "2026-11-08T12:00:00Z",
    "readAt": "2026-11-08T12:01:00Z",
    "deletedAt": "2026-11-08T12:02:00Z",
    "text": null
  }],
  "nextCursor": null,
  "nextCursorId": null,
  "hasMore": false
}
```

**Bảo mật:** không log Notification text hoặc reference sensitive content. Giữ nguyên authorization dựa trên actor từ JWT; client không được gửi `RecipientUserId` trong request.

### 3.3. Database/Migration

- Model mới `Notification.DeletedAt: DateTime?`, SQL `datetime2(7) NULL`; toàn bộ record cũ mặc định NULL -> giữ nguyên hiển thị.
- Cân nhắc index `(RecipientUserId, DeletedAt, CreatedAt DESC, Id DESC)` hoặc filtered index, **chỉ** khi chứng minh query plan và không trùng index hiện hữu; giữ nguyên index cũ cho tới khi có bằng chứng.
- Sửa `GetNotificationPageAsync`, `GetUnreadNotificationsAsync`, `GetNotificationAsync`, `MarkAllReadAsync` lọc `DeletedAt == null`; thêm repository Trash, one soft-delete, bulk-read soft-delete, restore, ownership/idempotency. Dùng `ExecuteUpdateAsync` có filter và một UTC timestamp per operation để tránh tải hàng loạt vào memory.
- Bulk delete/read đồng thời: update có điều kiện `ReadAt != NULL && DeletedAt == NULL` tại SQL transaction/statement, không đọc IDs trước rồi update toàn bộ về sau. Notification mới đến không bị xoá do `ReadAt == NULL`. Nếu concurrent read xảy ra trong lúc bulk delete thì hành vi được xác định bởi thứ tự commit và các điều kiện tại statement; test race.
- Trash retention: **chưa xóa vĩnh viễn**. Nếu sau này cần auto-purge, phải là BCR mới với thời hạn và security review.

### 3.4. SignalR

- Event mới `NotificationDeleted` `{ eventId, scope: "Single"|"ReadBatch", notificationId: Guid|null }` gửi đến **các sessions của actor**, không chứa text/token; event chỉ phát sau DB commit thành công.
- Event mới `NotificationRestored` `{eventId, notificationId}` gửi actor; client invalidate/refetch `notifications/list`, `notifications/unread-count`, `notifications/trash`.
- Không dựa vào event để tự tính count; server là source of truth. Reconnect/focus phải refetch. Fanout failure không rollback business DB đã commit; cần ghi nhận/đo lường, recovery bằng HTTP.
- Giữ các event `NotificationRead`, `NotificationCreated` đang có; không thay payload hiện hữu.

### 3.5. Frontend

- Notification page: action **“Xóa”** trên từng item, riêng **“Xóa thông báo đã đọc”** với confirm; tab **“Thùng rác”**, nút **“Khôi phục”**, empty/error/loading states, toast vi-VN.
- Không hiển thị **“Xóa tất cả”** nếu hành vi thực tế chỉ xóa đã đọc. Có thể dùng câu “Xóa tất cả đã đọc”.
- Dùng Axios API module hiện có, TanStack Query invalidate các query liên quan khi server xác nhận; không fake local deletion.
- Privacy: item trong Trash có resource bị mất quyền phải được ẩn/không tiết lộ nội dung (reuse visibility checks).

## 4. BCR-002 — Friend Request unseen badge

### 4.1. Định nghĩa

- `Pending`: yêu cầu chưa accept/reject/cancel/block (từ trường `Status`).
- `Unseen`: `ReceiverUserId == actor && Status == "Pending" && SeenAt == NULL`.
- Badge trên rail **Bạn bè** = UnseenCount (không dùng PendingCount). Tab lời mời có thể hiển thị pending count riêng, đặt nhãn tách biệt.
- `mark-seen` chỉ nhận **IDs thực tế đã render trong vùng có thể nhìn khi tab foreground**; không tự mark toàn bộ khi vào route Bạn bè, không mark item trang chưa tải.
- Accept/Reject/Cancel/Blocked khiến record không còn Pending, tự loại khỏi badge dù `SeenAt` vẫn NULL. Reverse send tự accept như source hiện có, không biến thành unseen.

### 4.2. API mới và DTO tương thích

| Method | Route | Response | Nguyên tắc |
|---|---|---|---|
| `GET` | `/api/friends/requests/unread-count` | `200 {"unreadCount": 3}` | Đếm Pending && unseen của actor |
| `POST` | `/api/friends/requests/mark-seen` | `204` | Body `{"requestIds":["guid",...]}`; batch tối đa 100 distinct IDs |

- Validate nonempty list, <=100, GUID không empty, reject duplicate hoặc deduplicate theo contract; đề xuất deduplicate đầu vào và 400 nếu count sau dedupe = 0. Input sai => 400; chưa auth => 401.
- Receiver ownership + Pending là **điều kiện của SQL UPDATE**, không phải chỉ check phía controller. IDs không thuộc actor bị bỏ qua, không tiết lộ tồn tại; replay `204` idempotent.
- Backward compatibility: giữ route và cấu trúc `GET /api/friends/requests`; **mở rộng append optional `SeenAt` vào `FriendRequestDto`** (response JSON field mới không phá client hiện có). Chỉ Incoming SeenAt có ý nghĩa; Outgoing có thể null và tuyệt đối không coi đó là unseen của sender.
- Có thể tận dụng summary/unread API hiện có *sau* khi đối chiếu đầy đủ consumer; bản đề xuất ưu tiên endpoint riêng để không đổi semantics của `/api/unread`.

### 4.3. Migration lịch sử

- Model mới `FriendRequest.SeenAt: DateTime?`, SQL `datetime2(7) NULL`.
- **Các Pending cũ** backfill `SeenAt` bằng một thời điểm UTC nhất quán trong migration; không làm badge tăng đột ngột.
- **Pending mới** mặc định `SeenAt = NULL` khi tạo; sau rollout có event tạo request làm mới count.
- Non-Pending cũ có thể giữ NULL vì không được tính unread; nếu khôi phục Pending ngoài workflow hiện có, phải xác định SeenAt theo nghiệp vụ.
- Tránh race giữa backfill và request mới: triển khai migration trong maintenance window ngắn hoặc triển khai qua chiến lược cutover/version marker; review trình tự migration, feature enablement và test tạo request quanh cutover.
- Candidate index `(ReceiverUserId, Status, SeenAt)`; xem xét filtered index `WHERE Status = 'Pending' AND SeenAt IS NULL` khi query thật phù hợp. Không thay unique filtered pair index.

### 4.4. SignalR và UI

- Giữ `FriendshipChanged` hiện hành cho friendship/request changes, thêm `FriendRequestsSeen` `{eventId}` gửi **chỉ recipient actor** sau mark-seen thành công. Có thể thêm `FriendRequestsUnreadChanged` invalidation khi request create/cancel/accept/reject/block nếu nguồn cũ không đảm bảo cập nhật toàn bộ clients; ưu tiên một **event chuẩn** `FriendRequestsUnreadChanged` `{eventId}` phát sau mọi thay đổi có thể làm đổi count, kể cả mark-seen, sender/receiver fanout đúng quyền.
- Để tránh event dư thừa, khuyến nghị **chọn duy nhất `FriendRequestsUnreadChanged`** (thay vì hai event đồng thời) cho badge; frontend refetch count khi nhận. Không làm thay ý nghĩa `FriendshipChanged`.
- Frontend dùng IntersectionObserver/visibility tracking thực sự, chỉ mark khi danh sách tải xong và document visible; batch <=100; không mark khi hover/menu click/route open; retry idempotent; request thất bại không tự giảm badge.
- Multi-tab/device: event invalidate count; fallback on focus/reconnect; PendingCount vẫn lấy từ danh sách/trạng thái server.

## 5. File changes dự kiến (không thay đổi trong giai đoạn này)

**Backend** (xác nhận lại HEAD trước):

- `Features/Notifications/Models/Notification.cs`
- `Features/Notifications/DTOs/NotificationDtos.cs`
- `Features/Notifications/Controllers/NotificationsController.cs`
- `Features/Notifications/Services/NotificationService.cs`
- `Features/Notifications/Repositories/Interfaces/INotificationRepository.cs`
- `Features/Notifications/Repositories/NotificationRepository.cs`
- `Features/Friends/Models/FriendRequest.cs`
- `Features/Friends/DTOs/FriendDtos.cs`
- `Features/Friends/Controllers/FriendsController.cs`
- `Features/Friends/Services/FriendService.cs`
- `Features/Friends/Repositories/Interfaces/IFriendRepository.cs`
- `Features/Friends/Repositories/FriendRepository.cs`
- `Infrastructure/Persistence/Configurations/Notifications/NotificationConfiguration.cs`
- `Infrastructure/Persistence/Configurations/Friends/FriendRequestConfiguration.cs`
- `Infrastructure/Persistence/Migrations/` (chỉ sau duyệt riêng)
- `Realtime/StateEvents.cs` và/hoặc publisher tương ứng.

**Frontend** (sau Backend approval và contract freeze): `features/notifications`, `features/friends`, `components/layout/NavigationLinks.jsx`, realtime provider, i18n `vi`, tests; không đổi MeChat Hybrid.

## 6. Migration & rollout plan (đề xuất, chưa thực thi)

1. Preflight read-only: Git HEAD/diff, EF model snapshot, migrations history, table sizes, index definitions, query plans, cleanup jobs, current date/time conventions.
2. Review backup + restore drill trên cơ sở dữ liệu test; bảo toàn dữ liệu người dùng.
3. Review SQL idempotent script/migration *sau khi được phép tạo migration*, including `SeenAt` backfill and nullable columns.
4. Test apply trên **database riêng** được Product Owner cho phép, không dùng `WebChatDb`; record count and stability checks.
5. Additive schema migration trước application feature enablement; deploy Backend code có guard mới và read filtering.
6. Frontend feature gate enable sau khi Backend deployed và Postman integration PASS.
7. Monitoring read/delete/unseen counts, 401/404/429, duplicate/fanout and performance; documented rollback trigger.

**Mối nguy deployment:** nếu bật `DeletedAt` filtering rồi rollback về backend cũ, deleted notifications có thể xuất hiện lại. Để rollback an toàn, giữ backend compatible có filter, **tắt mutating endpoints bằng feature flag** thay vì rollback code về phiên bản hoàn toàn không biết DeletedAt. Không drop cột khi đã có dữ liệu mới.

## 7. Test matrix (Postman cho Backend)

### Notifications

- N01 owner delete unread => 204; active absent, trash present, unread count giảm.
- N02 owner delete read => 204, count không đổi.
- N03 delete read-only bulk => **chỉ** ReadAt != NULL and DeletedAt NULL chuyển sang Trash; unread giữ nguyên.
- N04 user B delete/restore A notification => 404; no changes.
- N05 retry delete => 204; không thay DeletedAt lần thứ hai.
- N06 restore unread/read => 204; ReadAt giữ nguyên, count phù hợp.
- N07 restore lặp => 204 idempotent nếu ownership hợp lệ.
- N08 trash pagination with >50, hidden resources, before/beforeId; không lặp/skip sai.
- N09 mark-read/read-all không áp dụng với Trash.
- N10 report/message/invite evidence và entities gốc vẫn còn.
- N11 race create/read/bulk-delete; notification chưa đọc mới vẫn còn.
- N12 two browser same actor: events + refetch, reconnect repair.
- N13 401 invalid token, 404 unauthorized object, 429 rate limit nếu policy áp dụng; no leaked PII.

### Friends

- F01 request new => recipient unseenCount+1, sender unchanged.
- F02 GET Pending shows SeenAt for incoming, never counts outgoing.
- F03 mark visible batch IDs => count giảm, PendingCount không giảm.
- F04 request không render/hidden tab/background => không mark.
- F05 alien IDs mixed with own => only own Pending update; no leak.
- F06 duplicate/replay batch => idempotent.
- F07 accept/reject/cancel/block/reverse accept => unseen count đúng theo status.
- F08 old pending backfill => badge không tăng sau migration.
- F09 new request during cutover => không đánh dấu nhầm đã xem.
- F10 multi-tab, SignalR event drop/reconnect => HTTP server truth convergence.
- F11 API 400 validations, 401 auth, unrelated permissions 403/404 as policy.
- F12 query/index plan and concurrent request updates.

### General

- Existing `GET /api/notifications`, `/unread-count`, `PATCH /read`, `POST /read-all`, `GET /api/friends/requests`, send/accept/reject/cancel and the 18 existing SignalR events regression PASS.
- No account secrets/tokens in logs/screenshots/traces. Tests only on approved disposable fixtures/test DB; never reset or delete existing user data.

## 8. Rollback strategy

- **Before schema apply:** revert commit/feature flags; no schema rollback.
- **After additive migration, before feature traffic:** disable features, keep harmless nullable columns; optionally revert test DB via rehearsed backup.
- **After soft-delete traffic:** do not deploy old backend unaware of `DeletedAt`; use compatible code filtering DeletedAt and feature switches off. Preserve Trash state.
- **After SeenAt writes:** keep column to preserve seen state; return old badge behavior only through compatible feature flag; do not drop field as hot rollback.
- **SignalR outage:** HTTP refresh/reconnect maintains eventual consistency; notify transport failures don't imply DB rollback.
- **Database failure/corrupt schema:** use tested restore plan with clear RPO/RTO decision; do not promise lossless recovery without verified backup.

## 9. Approvals & release gates

| Gate | Required decision | Current |
|---|---|---|
| G0 Business requirements (six decisions) | PO | **APPROVED** |
| G1 Technical contract + migration semantics + rollback + feature flags | PO2026-11-08 | **APPROVED** |
| G2 Permission to edit Backend/Frontend source and create migration file | PO2026-11-08 | **SOURCE_IMPLEMENTATION_AUTHORIZED** |
| G3 Permission to apply migration to named disposable test DB | PO, explicit target & backup | **NOT AUTHORIZED** |
| G4 Permission to apply to real MeChat DB | PO, explicit DB/instance, backup and acceptance evidence | **NOT AUTHORIZED** |
| G5 Live/Postman + two-user acceptance and release | PO | **NOT STARTED** |

**Recommended implementation order after G2:** (1) code + unit tests/migration script (no application), (2) test DB migration, (3) Backend endpoints + Postman acceptance, (4) FE + SignalR + client tests, (5) live two-user regression, (6) document and release review. Keep BCR-001/002 independently switchable.

## 10. Outstanding technical details requiring source/live validation

1. HEAD vs snapshot drift since uploaded ZIP; do not assume names/behavior unchanged.
2. Real DB schema/indexes/size and applied migration list; no live SQL inspection performed.
3. `VisibleAsync` semantics for Trash when referenced content becomes inaccessible, especially AdminWarning and message references.
4. Need to settle notification restore retention period / permanent purge **only if** desired; excluded from this BCR version.
5. Exact notification real-time event naming, dedupe and grouping in current frontend provider.
6. SQL Server EF `Guid.CompareTo` translation/cursor strategy and Trash page correctness.
7. Data backfill cutover for FriendRequest records created concurrently with migration; verify with test scenario.
8. Actual risk of index overhead before adding candidate indexes.
9. Whether system-wide `UnreadSummary` should expose friend request unread; proposed separate endpoint preserves compatibility.

---

## 11. Deliverable for Codex handoff (read-only design validation)

Codex should compare this document with current workspace at `E:\Learn\MeChat`; check Git diff and DB schema **read-only if authorized**, then produce a final file-level implementation plan, API contract spec, migration SQL *design text only*, Postman matrix, rollback procedure and discrepancies. No source implementation, no migration creation/apply, no DB write, no push.

## 12. Addendum — kết quả technical validation HEAD 2026-11-08

**BUSINESS_APPROVED / TECHNICAL_REVIEW_PENDING / IMPLEMENTATION_NOT_AUTHORIZED.** Đây là review tĩnh source, không xác minh deployed DB/runtime hoặc cấp quyền implementation. Agent không sửa source, chạy tests ghi dữ liệu, EF CLI/migrations/SQL/Postman hoặc commit/push. Working diff bốn file Notifications được đọc và giữ nguyên.

| Chủ đề | Kết quả khác biệt/làm rõ so với v1.0 | Quyết định kỹ thuật trình duyệt |
| --- | --- | --- |
| Provenance | ZIP cũ không có trong input hiện tại nên không thể diff byte-for-byte; đã đối chiếu từng assertion với HEAD và working diff hiện có | Package mục2 có S01–S31 và đường dẫn Entity/DbContext/config/migrations/DTO/controller/service/repository/auth/SignalR/test source |
| Schema/live DB | Source vẫn chưa có DeletedAt/SeenAt hoặc 6 BCR endpoints; hai migrations nguồn không đổi | Live migration history/schema/index/volume/backup chưa kiểm tra; không coi source snapshot là schema deployed |
| Candidate friend index | Snapshot map Status thành nvarchar(max), không hợp lệ làm index key trong candidate Receiver/Status/SeenAt | T08: loại candidate key đó; đánh giá Receiver/SeenAt với INCLUDE(Status) hoặc variant sau plan evidence; không tự đổi Status type/length |
| Visibility | VisibleAsync chỉ kiểm resource với Message/GroupMessage/MessageRequest; AdminWarning text dùng recipient-scoped lookup riêng | T01: chốt owner+visible trước single-item no-op; bulk active-read kể cả hidden là đề xuất, chưa mặc định được duyệt |
| Trash pagination | Active source bounded scan200; trang rỗng+hasMore có thể hợp lệ, cursor raw tiến | T02: CreatedAt/Id DESC, bounded scan và cursor metadata cần PO chấp thuận; khác mong muốn tránh mọi trang rỗng. Nếu không chấp thuận phải thiết kế lại trước G2 |
| Read/delete race | Tracked MarkRead hiện chưa bảo vệ race delete mới | T05: conditional UPDATE tại thời điểm ghi, không chỉ lọc SELECT; không thêm rowversion/ETag ngoài scope |
| Friend transitions | Block dùng PermissionsChanged; reverse send tự accept; không phải mọi Pending transition đi FriendshipChanged | T06: count invalidation phải bao phủ receiver thật sự bị đổi; giữ 18 events cũ |
| Event identity | StateEvents không tự thêm eventId; old NotificationCreated payload không đồng nhất; delivery best-effort | T06: ba event mới metadata-only, caller sinh ID sau commit; chỉ chọn FriendRequestsUnreadChanged cho BCR-002, HTTP repair bắt buộc |
| DTO privacy/input | Cùng FriendRequestDto cho incoming/outgoing; v1.0 chưa ràng buộc outgoing và raw batch cap đủ rõ | T03/T04: đề xuất raw1–100, bounded body; outgoing SeenAt luôn null, incoming marker thật; không tạo sender seen receipt |
| Backfill | Timestamp cutoff đơn lẻ không an toàn với skew/inflight transaction | T07: maintenance drain tất cả writers, một UTC instant cho old Pending null; new Pending null sau cutover kể cả flags off; không rerun backfill |
| Test environment | DatabaseTarget khóa instance/catalog MeChat; đổi tên test DB tùy ý bị chặn; existing integration tests ghi DefaultConnection, có seed Admin | T07: VM SQL cô lập cùng instance/catalog hoặc test-only DI harness duyệt riêng; không chạy bộ test cũ trên DB mặc định, không nới production guard |
| Errors/rollout | Chưa có BCR options/coded error envelope đồng nhất | T08: endpoint-local safe errors và hai flags off; DeletedAt filters không được tắt khi rollback feature |

Routes/payload/status codes, concurrency, file-level scope, G0–G5 và T01–T08 nằm trong [package chính](bcr-001-002-implementation-approval-package.md); Postman/unit/SQL test cases và runbook nằm trong [plan](bcr-001-002-test-and-rollout-plan.md). Tất cả test đề xuất là **PLANNED_NOT_RUN**. Retention/purge Trash không thuộc phạm vi; không tự chốt thời hạn. RPO/RTO, maintenance window, DB target/backup và technical choices vẫn cần PO/operator duyệt trước gate tương ứng.

**Next Exact Action:** PO duyệt hoặc điều chỉnh T01–T08 và cấp quyền G2 riêng trước khi viết code; G3/G4 không tự đi kèm G2. Agent dừng sau khi trình tài liệu.

**END — G1/G2 APPROVED; G3/G4/G5 NOT_AUTHORIZED**

## 13. G2 source handoff

T01–T08 được PO duyệt theo prompt cuối; source/migration/tests đã thực hiện trong G2. Chi tiết hiện tại: [G2 report](bcr-001-002-g2-report.md), [73-case evidence](bcr-001-002-case-evidence.md), [G3 package](bcr-001-002-g3-approval-package.md). Các đoạn read-only/pending trước mục này là lịch sử review, không phải blocker viết source hiện hành. Không apply DB hoặc tự mở G3.
