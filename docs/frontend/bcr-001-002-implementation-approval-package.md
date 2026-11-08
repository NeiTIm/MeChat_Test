# BCR-001/BCR-002 — Implementation Approval Package

> Quyết định cuối của Product Owner — 2026-11-08: **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED (G2)**. T01–T08 đã được duyệt; visibility tối thiểu 25% liên tục 500ms, foreground + focused, thích nghi row cao hơn viewport. G3/G4/G5 **NOT_AUTHORIZED**: không apply migration, không ghi/test SQL, không bật runtime flags. Các mô tả pending/read-only trong bản review bên dưới là lịch sử; prompt phê duyệt cuối ưu tiên. Hai flags mặc định OFF; index mới không được thêm. Xem roadmap và báo cáo G2 để biết tiến độ thực tế.


Ngày review: **2026-11-08**, Asia/Bangkok. Workspace: `E:\Learn\MeChat`.

**BUSINESS_APPROVED**

**TECHNICAL_APPROVED**

**SOURCE_IMPLEMENTATION_AUTHORIZED**

Tài liệu giữ thiết kế và review trước G2, đã được Product Owner duyệt theo quyết định ở đầu trang. Source/evidence mới xem [G2 report](bcr-001-002-g2-report.md); không diễn giải source review lịch sử bên dưới là code hiện tại hoặc runtime PASS. Nguồn nghiệp vụ: [Technical Design Baseline v1.0](MeChat_BCR001_BCR002_Technical_Design_Baseline_v1.md). Phần kiểm thử/vận hành: [Test, deployment, backup và rollback plan](bcr-001-002-test-and-rollout-plan.md).

## 1. Kết luận và phạm vi xác minh

HEAD đọc trực tiếp: `ecb129989cb4271d05f669a4b712026f2e1200a2` — `fix(frontend): improve chat UX, media handling and UI consistency`. Khi bắt đầu, Git chỉ có baseline v1.0 untracked; Backend/Frontend không có diff. Không có AGENTS.md trong workspace hoặc tại các thư mục cha đã kiểm tra.

Trong lúc review, working tree xuất hiện refactor ở bốn file Notifications: `NotificationsController.cs`, `NotificationService.cs`, `NotificationRepository.cs`, `INotificationRepository.cs`. Controller đổi expression body thành block và đưa beforeId lên trước CancellationToken trong signature List; service dùng continue/switch tương đương và rút gọn comment/formatting; repository/interface rút gọn formatting/comment. Đã đọc nội dung/diff: routes/query names/status, predicates/cursor/visibility, tracked MarkRead và account events đang xét không đổi. Agent không sửa/hoàn tác các file này. Kết luận dùng cả HEAD và working diff, không gán toàn bộ workspace là clean HEAD. Cần recheck mọi source diff phát sinh tiếp trước G2.

Đã đối chiếu Entity, DTO, Controller, Service, Repository/interface, DbContext, EF configurations, hai migration và model snapshot, transaction helper, authentication/session validation, rate limiter, publisher/hub, các nơi tạo notification, unread summary, frontend consumers và nguồn test hiện có. Chưa đọc DB live, migration history live, query plan, row counts, backup hoặc cấu hình process đang chạy. Không chạy build, test, Postman, SQL hoặc EF CLI trong lượt review này.

**Kết luận:** sáu chính sách khả thi với hai cột nullable, sáu endpoint mới và ba event invalidation mới được đề xuất. Chưa đủ điều kiện viết code vì phải chốt các lựa chọn kỹ thuật T01–T08 ở mục 10, đặc biệt privacy/visibility, pagination, test isolation và cutover. Không có đề xuất đổi JWT/refresh/CSRF hoặc các event cũ.

### Sáu quyết định nghiệp vụ giữ nguyên

| ID | Quyết định đã duyệt | Bất biến thiết kế |
| --- | --- | --- |
| D1 | Notification soft-delete theo người nhận | Chỉ đổi `DeletedAt` trên notification thuộc actor; không xóa entity nguồn |
| D2 | Bulk chỉ xóa thông báo đã đọc | SQL predicate phải có `ReadAt IS NOT NULL` và `DeletedAt IS NULL` |
| D3 | Có Trash/Restore | View riêng; restore giữ `ReadAt`; không hard-delete/auto-purge |
| D4 | Pending cũ được xem là đã xem sau migration | Backfill một lần; pending tạo sau cutover mặc định `SeenAt=NULL` |
| D5 | Mark-seen theo batch IDs thực sự đã nhìn thấy | GET/route-open không ghi seen; server lọc receiver + Pending + unseen |
| D6 | Account-scoped SignalR + HTTP reconciliation | Event metadata sau commit; HTTP là nguồn count; không tính badge từ event |

## 2. Ma trận giả định baseline và source hiện tại

Các đường dẫn dưới đều tương đối với repository. Đây là xác minh **source**, không chứng minh schema deployed. Ma trận gồm cả assertion có trong v1.0 và các giả định/rủi ro bổ sung cần loại trừ khi thiết kế; một câu hỏi kiểm tra trong cột đầu không có nghĩa baseline đã khẳng định điều đó. Đặc biệt S17/S21/S23/S25/S26/S28/S29 là các kiểm tra bổ sung, không phải lỗi trích dẫn của baseline.

| ID / giả định baseline | Kết quả source HEAD | Tác động/khác biệt |
| --- | --- | --- |
| S01 Notification chưa có DeletedAt | [Notification.cs](../../backend/MeChat.Api/Features/Notifications/Models/Notification.cs): Id, RecipientUserId, ActorUserId?, Type, ReferenceId?, CreatedAt, ReadAt? | Đúng; thêm `DateTime? DeletedAt`; không thêm recipient mapping/table mới |
| S02 FriendRequest chưa có SeenAt | [FriendRequest.cs](../../backend/MeChat.Api/Features/Friends/Models/FriendRequest.cs): normalized pair, Status, CreatedAt, RespondedAt? | Đúng; `SeenAt` là marker riêng, không đổi Status thành Seen |
| S03 DbContext có hai DbSet | [AppDbContext.cs](../../backend/MeChat.Api/Infrastructure/Persistence/AppDbContext.cs), ApplyConfigurationsFromAssembly + UtcDateTimeConvention | Đúng; không cần DbSet mới; không có global soft-delete query filter |
| S04 Notification index recipient/time | [NotificationConfiguration.cs](../../backend/MeChat.Api/Infrastructure/Persistence/Configurations/Notifications/NotificationConfiguration.cs) | Đúng; index RecipientUserId/CreatedAt, chỉ FK recipient được khai báo ở entity này |
| S05 Friend pair unique Pending | [FriendRequestConfiguration.cs](../../backend/MeChat.Api/Infrastructure/Persistence/Configurations/Friends/FriendRequestConfiguration.cs) | Đúng; giữ nguyên filtered unique pair và FK sender/receiver |
| S06 Candidate index receiver/status/seen | [Snapshot](../../backend/MeChat.Api/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs), block FriendRequest: **Status=nvarchar(max)**; có index receiver, sender ngoài pair | **Không thể dùng Status làm index key** như candidate v1.0; không tự đổi độ dài/type Status |
| S07 Hai migration lịch sử | [Migrations](../../backend/MeChat.Api/Infrastructure/Persistence/Migrations): InitialSchema `20261028101015`, AuthLockoutFields `20261028101250` + Designer + Snapshot | Đúng trong source; không biết live history. Không sửa/đổi tên hai migration này |
| S08 Nullable date UTC/datetime2 | [UtcDateTimeConvention](../../backend/MeChat.Api/Infrastructure/Persistence/Conventions/UtcDateTimeConvention.cs); snapshot DateTime columns=`datetime2`, ProductVersion 10.0.12 | UTC Kind restored qua converter; FK NoAction. Đề xuất precision 7 cho cột mới, không đổi cột cũ |
| S09 No concurrency token cho hai entity | Config/snapshot Notification và FriendRequest không có RowVersion/concurrency token | Entity khác có RowVersion không làm hai entity này được bảo vệ; conditional SQL bắt buộc |
| S10 Notification APIs chưa delete/trash/restore | [NotificationsController](../../backend/MeChat.Api/Features/Notifications/Controllers/NotificationsController.cs) | Đúng; 6 actions hiện hữu: list/count/read/read-all và 2 conversation settings |
| S11 Friends APIs chưa unseen | [FriendsController](../../backend/MeChat.Api/Features/Friends/Controllers/FriendsController.cs), [FriendDtos](../../backend/MeChat.Api/Features/Friends/DTOs/FriendDtos.cs) | Đúng; request list Incoming/Outgoing, không phân trang, chỉ Pending; send có 201 body rỗng hoặc 200 Accepted |
| S12 Notification active queries chưa lọc delete | [INotificationRepository](../../backend/MeChat.Api/Features/Notifications/Repositories/Interfaces/INotificationRepository.cs), [NotificationRepository](../../backend/MeChat.Api/Features/Notifications/Repositories/NotificationRepository.cs) | Đúng; page/unread/get-for-read/read-all đều cần active predicate; get-for-lifecycle mới phải đọc được cả Trash |
| S13 Service scan 200 rồi lọc quyền | [NotificationService.ListAsync](../../backend/MeChat.Api/Features/Notifications/Services/NotificationService.cs) | Đúng: limit clamp 1–50, scan 200. HasMore có thể true khi items rỗng; cursor tiến theo raw cuối nếu chưa đủ visible. Không kết luận đây là skip/lặp đã xảy ra |
| S14 Visibility tương đương mọi quyền notification | NotificationService.VisibleAsync: ReferenceId null → true; Message/GroupMessage/MessageRequest → MessageActionService.CanViewAttachmentAsync; type khác → true | **Hẹp hơn giả định tổng quát**. AdminWarning không qua message guard; text được lọc riêng theo audit recipient. Không tự mở rộng rule cho type khác |
| S15 AdminWarning text recipient-only | [GetAdminWarningTextAsync](../../backend/MeChat.Api/Features/Notifications/Repositories/NotificationRepository.cs) lọc WarnUser/User/TargetId=actor | Đúng; Trash phải dùng cùng projection, không lấy audit.Details trực tiếp theo referenceId |
| S16 Count và read-all cùng visibility | NotificationService.UnreadAsync đếm visible; MarkAllReadAsync chủ ý update cả notification hidden của actor | **Khác nhau có chủ ý**. Giữ read-all semantics đối với active; bulk delete hidden scope cần T01 |
| S17 MarkRead hiện an toàn với delete race | MarkReadAsync load tracked row, VisibleAsync, `ReadAt ??= Now`, SaveChanges | Chưa có delete nên chưa xử lý race mới. Chỉ thêm DeletedAt filter vào SELECT không đủ: cần conditional UPDATE tại thời điểm ghi |
| S18 StateEvents tự sinh eventId | [StateEvents.SendAsync](../../backend/MeChat.Api/Realtime/StateEvents.cs) chỉ distinct recipients + Clients.User + catch/log warning | **Không đúng**: caller tạo payload/eventId. Không có outbox/durable delivery; GroupChangedAsync tự tạo eventId riêng |
| S19 Friend changes đều đi FriendshipChanged | [FriendService](../../backend/MeChat.Api/Features/Friends/Services/FriendService.cs) | Send/create/reverse accept/respond/cancel/unfriend/unblock dùng ChangedAsync; **Block phát PermissionsChanged**. Cần đưa block vào coverage count mới |
| S20 Friend transaction + afterCommit | [FriendRepository](../../backend/MeChat.Api/Features/Friends/Repositories/FriendRepository.cs) Serializable, retry tối đa 3 chỉ khi enableRetry; FriendService.AtomicAsync clear event callbacks mỗi attempt | Đúng; Send enableRetry=true, không mặc định retry mọi operation. Event mới phải đăng ký trong afterCommit, không phát trong transaction |
| S21 Pending list có eligibility filter | GetFriendRequestsAsync chỉ actor direction + Status Pending; incoming/outgoing CreatedAt DESC, Id ASC | Không lọc sender eligibility như SendAsync. Count mới giữ đúng Pending/SeenAt definition, không thêm filter về sender/suspension ngoài nghiệp vụ đã chốt |
| S22 DTO SeenAt không có rủi ro privacy | Cùng FriendRequestDto dùng hai projection incoming/outgoing | Có rủi ro lộ thời điểm receiver xem cho sender; đề xuất **Outgoing.SeenAt luôn null**, Incoming trả marker của recipient |
| S23 UnreadSummary.RequestCount là friend requests | [UnreadSummaryService](../../backend/MeChat.Api/Features/Messages/Services/UnreadSummaryService.cs) gọi **MessageRequestService**, không phải FriendService | Không đúng nếu diễn giải như vậy; giữ `/api/unread` nguyên semantics; count friend endpoint riêng |
| S24 Event cũ có payload đồng nhất | Source PrivateChatService/GroupService/MessageActionService/AdminService tạo NotificationCreated khác shape; forward không có eventId, AdminWarning có eventId nhưng không notificationId | Giữ nguyên payload cũ; consumer không được đòi tất cả event cũ có ID mới |
| S25 Auth chỉ cần JWT hợp lệ | [JwtSessionValidationEvents](../../backend/MeChat.Api/Configurations/JwtSessionValidationEvents.cs): sid/user/verified/deactivated/suspension checks; appeal scope chỉ `/api/appeals` | BCR APIs dùng normal active session, actor từ claims. Appeal token không được gọi BCR; không sửa refresh/CSRF |
| S26 Có rate limiting/typed error uniform | [RateLimitingExtensions](../../backend/MeChat.Api/Configurations/RateLimitingExtensions.cs): global 600/IP/phút; [Program](../../backend/MeChat.Api/Program.cs) AddProblemDetails + UseExceptionHandler | Có global 429; chưa có BCR policy/options hoặc error-code envelope đồng nhất. Không hứa Retry-After/body cho middleware hiện tại |
| S27 Disposable test DB có thể đổi connection string | [DatabaseTarget](../../backend/MeChat.Api/Infrastructure/Persistence/DatabaseTarget.cs) và [DesignTimeFactory](../../backend/MeChat.Api/Infrastructure/Persistence/MeChatDesignTimeFactory.cs) khóa `lpc:.\MSSQLSERVER01/MeChat`, encrypted Windows auth | **Blocker của baseline test plan**: đổi catalog tùy ý bị từ chối. Không nới production guard trong BCR này |
| S28 Existing integration tests đều an toàn để chạy | [FriendIntegrationTests](../../backend/MeChat.Tests/FriendIntegrationTests.cs) register/login + đọc dev-mail; [RetentionSqlAcceptanceTests](../../backend/MeChat.Tests/RetentionSqlAcceptanceTests.cs) ghi fixtures vào DefaultConnection, có seed Admin | **Không chạy**. Cần isolation/permission riêng, không coi tên “test” hay dry-run là read-only |
| S29 Startup tự migrate | Program/Infrastructure/DesignTimeFactory không gọi Migrate hoặc EnsureCreated trong source đã rà soát | Không có auto-migration ở các entrypoint này. Không suy ra process/live DB đã đúng schema |
| S30 Retention hiện có xử lý Trash | Retention repositories xử lý audit/evidence/files; chưa có notification trash fields/job | Không tái dùng purge; giữ cả hai feature ngoài retention hiện tại |
| S31 FE tự hiểu event/API mới | [RealtimeProvider](../../frontend/src/app/providers/RealtimeProvider.jsx) có 18 names cũ; notifications/friends adapters chưa có BCR endpoints | Đúng là chưa có. Đề xuất 21 names sau 3 event mới, chưa được triển khai; query key friend riêng, không dùng key message request |

Không thể chứng minh diff byte-for-byte giữa HEAD và `MeChat(4).zip` vì ZIP không có trong input hiện tại. Các assertion snapshot được đối chiếu theo từng member như trên; không gán snapshot cũ là HEAD hiện nay.

## 3. Contract BCR-001 đề xuất để chốt

### 3.1. State và quyền

| Trạng thái | Predicate | List active/count | Trash |
| --- | --- | --- | --- |
| Active unread | DeletedAt null, ReadAt null | List nếu visible; count nếu visible | Không |
| Active read | DeletedAt null, ReadAt non-null | List nếu visible, không tăng count | Không |
| Trash unread | DeletedAt non-null, ReadAt null | Không | Chỉ nếu visible |
| Trash read | DeletedAt non-null, ReadAt non-null | Không | Chỉ nếu visible |

T01 đề xuất thứ tự single-item: normal auth → recipient ownership → VisibleAsync → trạng thái. Unknown/alien/invisible cùng `404 NOTIFICATION_UNAVAILABLE`, kể cả khi action sẽ là no-op. Vì vậy “ownership là đủ để replay204” trong baseline phải được làm rõ: chỉ replay204 khi **vẫn visible**. Không trả 403 hoặc NOT_OWNED làm oracle đối tượng.

Bulk đề xuất giữ phạm vi **tất cả active-read của recipient**, kể cả hidden: không trả số lượng/IDs nên không mở nội dung nguồn; phù hợp read-all hiện hành. Đây là lựa chọn cần PO chốt T01, không được tự suy từ D2 rằng bulk chỉ áp dụng phần đang render hoặc phần visible.

| Method / route mới | Request | Success | State transition |
| --- | --- | --- | --- |
| DELETE `/api/notifications/{id:guid}` | Không body | 204, body rỗng | Owned visible Active → DeletedAt=opUtc; đã Trash →204 không đổi DeletedAt |
| DELETE `/api/notifications/read` | Không body; không nhận IDs/recipient/confirm flag | 204, kể cả không có record phù hợp | Chỉ actor AND ReadAt non-null AND DeletedAt null → Trash |
| GET `/api/notifications/trash` | before?, beforeId?, limit=30 | 200 TrashedNotificationPageDto | Read-only; không mark-read/restore |
| POST `/api/notifications/{id:guid}/restore` | Không body | 204, body rỗng | Owned visible Trash → DeletedAt=null; đã Active →204; giữ ReadAt |

`read`/`trash` là literal, `{id:guid}` loại trừ chúng; UUID route không parse được →404. Guid.Empty parse được nhưng không phải entity hợp lệ →404. Không thêm `DELETE /api/notifications`, không nút “Xóa tất cả” chung, không hard-delete. Không nhận actor từ query/body.

Active APIs giữ HTTP method/routes/DTO shape hiện tại. GET count vẫn `{ "count": n }`. PATCH read trả404 cho Trash; POST read-all chỉ update active unread, vẫn gồm active hidden như source. `/api/unread.notificationCount` phải đi cùng service đã lọc DeletedAt. Conversation mute/settings không đổi.

### 3.2. DTO và pagination — T02 đề xuất

Active NotificationDto và NotificationPageDto giữ nguyên. New trash item: `id:guid`, `type:string`, `referenceId:guid?`, `createdAt:UTC datetime`, `readAt:UTC datetime?`, `text:string?`, `deletedAt:UTC datetime` non-null. Không trả ActorUserId/RecipientUserId, storage path, audit entity hoặc raw source content. Text chỉ AdminWarning qua recipient-scoped helper như active projection. Không tạo tombstone trả metadata của hidden item trong items.

Trash page: `{items: TrashedNotificationDto[], nextCursor: string|null, hasMore: boolean, nextCursorId: guid|null}`. Khi hasMore=false, cả hai cursor null. Timestamp wire ISO8601 UTC, giữ tối đa precision của server; FE giữ nguyên string cursor, không round qua JS Date. Không có total/trashCount mới.

Đề xuất Trash sắp xếp **CreatedAt DESC, Id DESC**, kế thừa active cursor; DeletedAt chỉ hiển thị metadata. Khác với “mới xóa gần đây nhất”; nếu muốn DeletedAt DESC cần chọn tại T02 và đổi toàn bộ cursor semantics trước code.

Predicate: recipient + state + `(CreatedAt < before OR (CreatedAt = before AND Id < beforeId))`, thực thi/order cùng SQL Server. Không sort Guid trong .NET/JS để tạo cursor: SQL uniqueidentifier order khác System.Guid.CompareTo. EF translation của LINQ CompareTo hiện có **chưa được kiểm thử ở lượt này**; sau phép G3 phải kiểm generated SQL và tie-order trên SQL Server thật, không thay bằng client evaluation.

Trash mới: cả before/beforeId đều vắng hoặc cùng hiện; timestamp không parse được, sai UUID, thiếu nửa cặp hoặc Guid.Empty →400 INVALID_NOTIFICATION_CURSOR. limit parseable int clamp1–50 như active; non-int →400 validation. Active API vẫn nhận legacy before-only như hiện tại, không âm thầm siết validation gây vỡ client cũ.

**Bounded-scan đề xuất cho bản này:** kế thừa scan200 raw rows + VisibleAsync, không thay thuật toán active ngoài predicate state. HasMore=true nghĩa còn batch có thể quét tiếp, không bảo đảm có thêm visible item. Khi visible >limit: cursor=visible cuối được trả, không consume item lookahead. Khi visible≤limit và raw=200: cursor=raw cuối, có thể items rỗng; client vẫn cho tải tiếp, không coi “rỗng” là hết thông báo. Cursor raw chỉ là ID notification thuộc actor + time, không referenceId/content. Đây là metadata privacy cần T02 chấp thuận.

V1.0 chưa chốt cách tránh empty pages. Đề xuất trên ưu tiên bounded work/backward compatibility, **không hứa loại bỏ mọi trang rỗng**. Nếu PO yêu cầu không có empty-success hoặc không trả cả owner hidden cursor metadata, phải chọn thiết kế khác (visible scan nhiều batch + work budget, hoặc opaque cursor mới); đánh giá lại latency/privacy và error contract trước G2. Không bỏ page cursor, không set HasMore=false để che hidden rows.

## 4. Contract BCR-002 đề xuất để chốt

| Method / route mới | Input / output | Điều kiện |
| --- | --- | --- |
| GET `/api/friends/requests/unread-count` | 200 `{ "unreadCount": 3 }` | COUNT actor ReceiverUserId AND Status=`Pending` AND SeenAt null; GET không ghi |
| POST `/api/friends/requests/mark-seen` | JSON `{ "requestIds": ["<guid>"] }`; 204 | Conditional UPDATE cùng predicate + IDs; set SeenAt=opUtc một lần |

T03 đề xuất **1–100 raw entries**, reject null/missing/empty list, malformed/empty GUID; deduplicate trong batch hợp lệ. 101 entries →400 dù chỉ có100 distinct hoặc nhiều duplicate. Đây là giới hạn đầu vào chặt hơn “100 distinct” chưa chốt ở v1.0, nhằm tránh payload không giới hạn. Đề xuất body cap8KiB tại endpoint, không đổi global JSON settings. Unsupported media type →415; host/gateway body-too-large →413 theo contract cần kiểm thử.

Unknown, actor là sender, alien, non-Pending, already-seen IDs bị ignore; mixed hợp lệ + IDs không được phép vẫn204, chỉ own Pending unseen đổi. Không trả affectedCount/list lỗi theo từngID. Batch invalid →400 trước bất kỳ UPDATE nào. Replay hợp lệ/all-no-op →204 và không overwrite SeenAt. Không thêm endpoint seen-all, không đổi PendingCount hoặc auto accept khi xem.

Giữ nguyên FriendRequestsDto(Incoming,Outgoing), list order và toàn bộ send/accept/reject/cancel/block/unblock routes/status values. Append optional `DateTime? SeenAt = null` vào FriendRequestDto: **Incoming có marker thật; Outgoing luôn null** (T04). Không công khai việc receiver đã xem cho sender. HTTP JSON additive thường tương thích client cũ; C# record constructor/deconstruct/binary consumers cần recompile/audit riêng. Hai `new FriendRequestDto` projection đang có phải được cập nhật đúng hướng; constructor optional không tự lấy marker Incoming.

Badge Bạn bè chỉ dùng `unreadCount` endpoint riêng. `/api/unread.requestCount` và frontend `requestCount` key hiện thuộc **message requests**, không được dùng lại. Các request được accept/reject/cancel/blocked tự rời predicate theo Status; SeenAt không cần reset. Reverse send auto-accept vẫn như source; không thêm status “Seen”, không hồi sinh historical request.

Frontend sau G2: gom các row thực sự visible ở foreground/focus; queue theo account, batch≤100, clear/abort khi đổi identity, revalidate IDs còn hiển thị trước retry. Không ghi do GET, route-open, hover hoặc item trang chưa tải; không giảm badge trước204/count refetch. Threshold/dwell cần ghi trong UI spec, đề xuất tái dùng ý tưởng visibility của message read hook nhưng không dùng sequence cursor cho friend IDs.

## 5. Errors, feature switches và backward compatibility

| HTTP | Contract đề xuất / ràng buộc |
| --- | --- |
| 200 | Chỉ GET; JSON count/page như trên |
| 204 | Mutation thành công/no-op hợp lệ; không response body, không counts phỏng đoán |
| 400 | New endpoints: ValidationProblemDetails, code `INVALID_REQUEST_IDS` hoặc `INVALID_NOTIFICATION_CURSOR`; lỗi model binding có thể ở errors; không echo IDs/body/token |
| 401 | Giữ JwtBearer challenge hiện hành; normal session revoked/expired hoặc appeal-scope không được dùng BCR. Không thay refresh/CSRF handling; không hứa middleware có custom code/body |
| 403 | Không dùng cho notification alien ownership. Có thể phát sinh authorization infrastructure trong tương lai, FE phải xử lý an toàn; không thêm Admin restriction cho recipient APIs |
| 404 | Single unknown/alien/invisible → ProblemDetails code `NOTIFICATION_UNAVAILABLE`; invalid route có thể body framework. Feature disabled →404 code `BCR_FEATURE_DISABLED` trên endpoint mới |
| 409 | Không bắt buộc cho replay hay empty bulk. Không có optimistic version/If-Match ở đề xuất hiện tại |
| 413/415 | Body quá lớn/Content-Type không hỗ trợ; phải kiểm host/gateway và không echo payload |
| 429 | Global limiter hiện hữu; không hứa Retry-After nếu server không cấp. FE không tự replay mutation vô hạn |
| 500 | Generic ProblemDetails, không exception/stack/SQL/PII; nếu outcome commit không rõ, GET reconciliation trước retry |

Ví dụ safe domain failure: `{ "type":"about:blank", "title":"Notification unavailable", "status":404, "code":"NOTIFICATION_UNAVAILABLE" }`. Code là contract kỹ thuật, FE map vi-VN. Không đưa raw Backend message lên UI; không đổi error shape của endpoints cũ hàng loạt.

Envelope mới khi controller/service có thể xử lý: `type:string`, `title:string`, `status:int`, `code:string`; `errors:object<string,string[]>` chỉ khi validation, trace correlation nếu có phải opaque và không chứa PII. Domain errors không trả `detail`/IDs/nội dung đã gửi. Các mã đề xuất chỉ gồm INVALID_REQUEST_IDS, INVALID_NOTIFICATION_CURSOR, NOTIFICATION_UNAVAILABLE, BCR_FEATURE_DISABLED. Error do route binding/auth/media-size/limiter/exception infrastructure có thể là ProblemDetails framework hoặc body rỗng và chưa chắc có code; FE fallback theo status + thông báo vi-VN an toàn. Không cam kết thêm custom envelope vào mọi middleware trong BCR. Test phải ghi actual shape ở từng lớp; không chỉ assert status rồi nhận error contract PASS.

GET count/Trash và DTO có SeenAt là dữ liệu theo tài khoản: đề xuất response `Cache-Control: private, no-store`, không shared CDN cache; client query cache account-scoped và reset logout/session revoke. Đây là header bổ sung ở response BCR/DTO liên quan sau G2, không thay auth contract. Không log mark-seen request body hay các cursor/ID batches trong access telemetry mới.

T08 đề xuất options riêng: `BcrFeatures:NotificationLifecycleEnabled` và `BcrFeatures:FriendRequestSeenEnabled`, mặc định false; chưa có trong source. Tắt BCR-001 chỉ tắt new endpoints/UI, **không tắt DeletedAt filter** ở APIs cũ. BCR-002 off không ghi SeenAt cho new request: new marker vẫn null để giữ cutover semantics. Không quảng cáo capability endpoint có sẵn; FE/new APIs được enable theo deployment manifest, xử lý404 an toàn.

Old FE + compatible new BE vẫn đọc list/count cũ, không thấy Trash; new FE chỉ enable sau BE/schema readiness. Old BE **không compatible về hiển thị sau khi có soft-delete traffic**, dù cột additive không làm INSERT cũ lỗi. Không cho rollout một phần node cũ còn serve GET/count khi bật delete.

## 6. Atomicity, concurrency và sự kiện

### Điều kiện ghi tối thiểu

- Delete-one: actor + id + DeletedAt null; chỉ set DeletedAt. ReadAt/CreatedAt/reference không đổi.
- Delete-read-batch: actor + DeletedAt null + ReadAt non-null trong **một conditional SQL UPDATE**, một UTC timestamp cho operation. Không SELECT IDs rồi update tất cả; không EF Remove/ExecuteDelete.
- Restore: actor + id + DeletedAt non-null; chỉ set DeletedAt=null, giữ ReadAt.
- MarkRead hiện hữu: sau visibility check, UPDATE actor+id+DeletedAt null+ReadAt null. Affected0 cần re-read owner/state để phân biệt visible active replay204 với Trash/unavailable404; không suy từ0 thành404 cho mọi trường hợp.
- MarkAllRead: actor+DeletedAt null+ReadAt null, không per-row visibility mới.
- Friend mark-seen: receiver actor+Pending+SeenAt null+IDs trong SQL. Affected0 vẫn204, không tiết lộ IDs tồn tại.

Không trộn tracked stale entity với ExecuteUpdate rồi SaveChanges có thể ghi đè marker. Repository lifecycle dùng no-tracking read + conditional writes; preserve existing FriendService Serializable transactions cho send/respond/cancel/block. MarkSeen không đổi Status/RespondedAt/unique pair. Hai operation phải khóa/đánh giá cùng row predicate tại SQL; kiểm deadlock order/bounded retry với transaction riêng từng request, không dùng chung DbContext song song.

T05 đề xuất không thêm rowversion/ETag trong BCR này: conflicting delete/restore có kết quả theo thứ tự state UPDATE được SQL áp dụng; replay idempotent khi không có thao tác đối nghịch xen giữa. **Không mô tả là “theo thứ tự commit” hoặc exactly-once.** Bulk có thể xóa một notification vừa được mark-read nếu nó đã thỏa predicate lúc UPDATE đánh giá; notification vẫn ReadAt null tuyệt đối không bị xóa, cả khi mới đến. Không biến D2 thành snapshot “đã đọc trước khi nhấn nút” nếu chưa được duyệt thêm cutoff policy.

Ownership/predicate atomic không làm visibility của nhiều bảng tự động linearizable. Đề xuất giữ reauthorization tại request như source; nếu cần khóa quyền xuyên toàn bộ check+update phải duyệt thêm serializable scope/lock order và test deadlocks (T05). Không tự tuyên bố block/revoke sau một permission-check đã hoàn tất có thể thu hồi response đang bay trên mạng.

### Event contract mới — T06

| Event đề xuất | Payload | Người nhận / trigger |
| --- | --- | --- |
| NotificationDeleted | `{eventId:guid, scope:"Single"|"ReadBatch", notificationId:guid|null}` | Actor account; Single có ID, batch null; sau actual state transition |
| NotificationRestored | `{eventId:guid, notificationId:guid}` | Actor account sau actual restore |
| FriendRequestsUnreadChanged | `{eventId:guid}` | Receiver account có count đổi; mark-seen chỉ actor receiver |

Chọn **duy nhất FriendRequestsUnreadChanged**, không thêm FriendRequestsSeen đồng thời. Existing FriendshipChanged/PermissionsChanged/NotificationRead/NotificationCreated giữ nguyên tên/payload/recipient semantics. New request →receiver target; reverse auto-accept/respond →request receiver; cancel →receiver của row; block →collect distinct ReceiverUserId từ Pending rows thật bị đổi; mark-seen →actor. Unfriend/unblock không tăng unseen theo workflow hiện tại nên không cần thêm event này nếu không có Pending transition.

Caller tạo eventId một lần cho event sau commit, cùng payload cho mọi session; StateEvents không tự thêm. Khi zero affected/no-op không cần new event, nhưng response vẫn204 và client vẫn có thể refetch. FriendService afterCommit callbacks phải được clear khi retry như hiện tại. Không đụng user event names cũ để tránh break client. Không phát event trước rollback hoặc kèm nội dung request/notification, counts hoặc thông tin người gửi.

StateEvents hiện best-effort, nuốt lỗi delivery rồi log; không có exactly-once/outbox. Sau DB commit, disconnect/cancellation có thể mất event. Thiết kế giữ fallback focus/reconnect/online và periodic foreground HTTP sync hiện có; delivery failure không đảo mutation thành failed DB transaction. New counters/trash/feed dùng query keys riêng nhưng cùng account cache cleanup. Nếu cần durable event guarantee phải duyệt outbox/schema riêng, không tự bổ sung trong gói này.

## 7. Migration và backfill — thiết kế, không phải migration file

Hai column mới: Notifications.DeletedAt và FriendRequests.SeenAt, nullable `datetime2(7)`, **không default current time**. Notification cũ toàn NULL, không tự chuyển Trash. Không đổi FK, pair normalization, enum/status, index uniqueness, source message/audit/evidence hoặc migration IDs cũ. Snapshot mới chỉ được sinh sau G2.

T07 khuyến nghị **maintenance window có write quiescence đầy đủ**, không rolling backfill theo CreatedAt:

1. Chặn ingress writers, drain transaction cũ trên mọi API instance/job/other writer; chỉ “tắt badge” không đủ. Không nhận friend Send từ backend cũ trong lúc backfill.
2. Trong migration transaction được review: thêm hai cột nullable; capture **một** DB UTC instant; UPDATE FriendRequests chỉ Status Pending AND SeenAt null thành instant đó. Không predicate theo CreatedAt vì source timestamp từ app có thể skew/backdate.
3. Non-Pending cũ giữ NULL; kiểm chứng old Pending không còn SeenAt null. Khi failed/rollback migration không publish events. Không chạy lại backfill trên mỗi deploy/startup hoặc sau feature toggle.
4. Deploy toàn bộ compatible backend (flags off), xác minh schema/code parity và filter trước resume traffic; sau cutover các pending mới null kể cả feature UI chưa enable.
5. Enable mỗi BCR sau test và approval release riêng. Không có default SeenAt=now trong entity/model/DB làm new request tự thành seen.

Alternative zero-downtime phải thiết kế cutover marker/generation có writer participation và snapshot ID set bền vững; timestamp-only không phân biệt chắc chắn in-flight commits. Alternative đó **chưa được duyệt**, không được tự thêm metadata table/cột generation.

Candidate index Notification `(RecipientUserId, DeletedAt, CreatedAt DESC, Id DESC)` hợp kiểu dữ liệu nhưng chưa có plan/cost evidence: giữ index cũ, chưa mặc định thêm/drop. Friend candidate sửa thành `(ReceiverUserId, SeenAt)` có thể `INCLUDE(Status)`, hoặc filtered variant sau SQL provider/plan validation; **không đặt Status nvarchar(max) vào key**, không tự sửa Status thành nvarchar(20). Nếu chưa có sizing/plan, ship schema correctness trước và duyệt index riêng. Không thay unique filtered Pending pair index.

Schema scale, row count, log headroom, backup, live history, SQL version/compatibility, generated SQL/locking/index choices đều **NEEDS_VERIFICATION**; source đủ để loại candidate key sai, chưa đủ để cam kết thời gian migration hay hiệu năng.

## 8. File-level implementation scope dự kiến — chưa được phép sửa

Backend paths dưới bắt đầu tại `backend/MeChat.Api/`:

| Files | Thay đổi dự kiến sau G2 |
| --- | --- |
| Features/Notifications/Models/Notification.cs | DeletedAt nullable |
| Features/Notifications/DTOs/NotificationDtos.cs | Trash item/page DTO riêng; active shapes giữ nguyên |
| Features/Notifications/Controllers/NotificationsController.cs | 4 new endpoints, validation/errors/flags, existing routes giữ nguyên |
| Features/Notifications/Services/NotificationService.cs | Active/trash projection cùng visibility; lifecycle conditional writes + post-commit events; read race guard |
| Features/Notifications/Repositories/Interfaces/INotificationRepository.cs | Explicit active/lifecycle/trash queries; affected-row mutation methods |
| Features/Notifications/Repositories/NotificationRepository.cs | State/owner predicates cho page/count/read/read-all/lifecycle; không Remove |
| Features/Friends/Models/FriendRequest.cs | SeenAt nullable; Status/normalized pair giữ nguyên |
| Features/Friends/DTOs/FriendDtos.cs | Optional SeenAt response + unread-count DTO + batch DTO |
| Features/Friends/Controllers/FriendsController.cs | 2 new endpoints; không đổi send201/Accepted200/action204 |
| Features/Friends/Services/FriendService.cs | Count/mark-seen orchestration + afterCommit coverage cho mọi Pending transition |
| Features/Friends/Repositories/Interfaces/IFriendRepository.cs; FriendRepository.cs | COUNT/update receiver+Pending+unseen; incoming SeenAt / outgoing null projections |
| Infrastructure/Persistence/Configurations/Notifications/NotificationConfiguration.cs; Configurations/Friends/FriendRequestConfiguration.cs | Cột precision và chỉ index đã duyệt; giữ indexes/FKs cũ |
| Infrastructure/Persistence/Migrations/&lt;new-id&gt;_NotificationTrashFriendSeen.cs + .Designer.cs; AppDbContextModelSnapshot.cs | Chỉ sau G2 sinh migration, review nullable/backfill/rollback hazards; không apply bởi G2 |
| Configurations/BcrFeatureOptions.cs (new); Features/Notifications/DependencyInjection.cs; Features/Friends/DependencyInjection.cs | Bind/validate hai feature switches nội bộ, không thêm SDK/dependency ngoài |
| Realtime/StateEvents.cs (conditional) | Chỉ nếu chọn typed helper mới; existing SendAsync đủ để reuse, không bắt buộc sửa publisher/ChatHub |

Không cần đổi AppDbContext DbSets, DatabaseTarget, DesignTimeFactory, authentication, refresh cookie/CSRF, repositories message/evidence hoặc retention jobs để thực hiện nghiệp vụ đã chốt. Nếu implementation cần vượt danh sách này phải review scope lại.

Frontend sau contract freeze: `frontend/src/features/notifications/api.js`, `use-notifications.js`, `NotificationsPage.jsx`, `NotificationPreview.jsx`; `features/friends/api.js`, `FriendsPage.jsx`, visibility batching hook mới; `components/layout/NavigationLinks.jsx`; `app/providers/RealtimeProvider.jsx`; `lib/errors.js`, i18n vi/errors/friends/notifications và feature tests. Không đổi MeChat Hybrid hoặc dùng mark-read giả delete. Global HTTP/auth transport không thuộc scope sửa trừ khi phát hiện gap và được duyệt riêng.

Tests được triển khai trong hai project riêng `backend/MeChat.Bcr.UnitTests/` và `backend/MeChat.Bcr.SqlTests/` để không discovery bộ SQL cũ khi chạy pure tests. Unit/HTTP in-process đã chạy; SQL tests chỉ build, chưa chạy. Frontend có existing suites + Trash/Restore/count/observer/identity regression; không bỏ ownership/privacy acceptance. Xem G2 report và G3 fixture manifest.

## 9. Release gates

| Gate | Điều kiện | Trạng thái |
| --- | --- | --- |
| G0 | D1–D6 của PO | BUSINESS_APPROVED |
| G1 | PO chốt T01–T08, ngày2026-11-08 | TECHNICAL_APPROVED |
| G2 | Source/migration creation, không apply DB | SOURCE_IMPLEMENTATION_AUTHORIZED |
| G3 | Cho apply/test ghi trên target SQL cô lập đã định danh, có cleanup/backup | NOT_AUTHORIZED |
| G4 | Cho apply trên DB thực, exact instance/catalog/host, backup và accepted SQL evidence | NOT_AUTHORIZED |
| G5 | Release/feature enablement sau live acceptance | NOT_AUTHORIZED |

Trạng thái trước implementation là PLANNED_NOT_RUN. Trạng thái mới theo [73-case ledger](bcr-001-002-case-evidence.md) và G2 report; safe tests không nhận thay SQL/runtime acceptance. Source permission đã được cấp riêng ở G2; DB/release permission vẫn khóa.

## 10. T01–T08 — APPROVED BY PRODUCT OWNER, 2026-11-08

| ID | Quyết định cuối đã được duyệt |
| --- | --- |
| T01 | Single owner+visible trước replay; unknown/alien/invisible404, visible owned no-op204; bulk actor active-read gồm hidden, không counts/IDs |
| T02 | Trash CreatedAt/Id DESC theo SQL keyset, paired cursor, bounded200, empty+hasMore và raw cursor metadata được chấp thuận; active legacy không siết validation |
| T03 | Raw1–100, validate whole body, dedupe, reject missing/null/empty/malformed/Guid.Empty, local8KiB; viewport25% liên tục500ms, foreground+focused, tall rows dùng tương đương theo viewport, account queue/chunk100/abort/retry visibility |
| T04 | Incoming SeenAt thật, Outgoing luôn null; không sender receipts |
| T05 | Conditional SQL, no RowVersion/ETag; read/delete race guard, preserve fields, after-commit only; no exactly-once/multi-table auth expansion |
| T06 | Đúng3 metadata events account-scoped; caller eventId; HTTP focus/reconnect/online reconciliation; no outbox |
| T07 | Hai nullable datetime2(7), no defaults; one DB UTC Pending/null backfill under maintenance write quiescence; new Pending null; test-only fail-closed allowlist fixture/DI, không fallback MeChat, không đổi production guard |
| T08 | Hai flags OFF; new routes404 khi OFF, active filters always-on sau schema; safe local errors; no unproven indexes/Status type changes; no incompatible old/new rollout |

G1/G2 không cấp G3/G4/G5. Source/tests/migration và safe results hiện tại ở [G2 report](bcr-001-002-g2-report.md); [G3 manifest](bcr-001-002-g3-approval-package.md) chưa được duyệt. Các mô tả pre-G2 trong mục1–8 là historical review, không ghi đè phê duyệt cuối.

## 11. Nguồn kỹ thuật đối chiếu

- [Microsoft — ExecuteUpdate](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete): update trực tiếp không đồng bộ change tracker; không tự áp optimistic concurrency hoặc gom nhiều call thành một transaction. Áp dụng vào conditional writes và after-commit design ở mục6.
- [Microsoft — SQL Server CREATE INDEX](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql?view=sql-server-ver17) và [included columns](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-indexes-with-included-columns?view=sql-server-ver17): LOB/max không dùng làm index key; included columns có quy tắc khác. Chưa có benchmark hoặc query plan của MeChat.
- [Microsoft — Guid/uniqueidentifier ordering](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql/compare-guid-uniqueidentifier-values?view=sql-server-ver17): không đồng nhất order SQL với System.Guid.CompareTo trong memory. Chưa chứng minh translation EF10.0.12 bằng runtime.

## 12. Kiểm tra hồ sơ bàn giao

Đã kiểm tra liên kết file tương đối trong 5 tài liệu bàn giao (baseline, BCR register, package, test/rollout plan, roadmap): không có link hỏng tại checkpoint. Có đủ ba status bắt buộc. Ma trận test có **73 IDs duy nhất**: 47 Postman/HTTP/security/realtime, 12 unit/component, 14 SQL integration. Đây là kiểm tra cấu trúc tài liệu, không phải 73 test đã chạy. Không chạy build/test/API/EF/SQL; không có successful integration evidence mới.

**G1/G2 đã được Product Owner duyệt. G2 source hoàn thiện và safe checks được báo cáo riêng; dừng trước G3.**
