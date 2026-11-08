# BCR-001/BCR-002 — Test, deployment, backup và rollback plan

> Quyết định cuối của Product Owner — 2026-11-08: **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED (G2)**. T01–T08 đã được duyệt; visibility tối thiểu 25% liên tục 500ms, foreground + focused, thích nghi row cao hơn viewport. G3/G4/G5 **NOT_AUTHORIZED**: không apply migration, không ghi/test SQL, không bật runtime flags. Các mô tả pending/read-only trong bản review bên dưới là lịch sử; prompt phê duyệt cuối ưu tiên. Hai flags mặc định OFF; index mới không được thêm. Xem roadmap và báo cáo G2 để biết tiến độ thực tế.


Ngày: **2026-11-08**, Asia/Bangkok. Thiết kế kèm [Implementation Approval Package](bcr-001-002-implementation-approval-package.md), source HEAD `ecb129989cb4271d05f669a4b712026f2e1200a2` và working diff bốn file controller/service/repository/interface Notifications (refactor, không đổi contracts/predicates đang xét).

**BUSINESS_APPROVED**

**TECHNICAL_APPROVED**

**SOURCE_IMPLEMENTATION_AUTHORIZED**

**Ma trận gốc trước G2: PLANNED_NOT_RUN.** Kết quả safe tests mới theo [G2 report](bcr-001-002-g2-report.md) và [73-case ledger](bcr-001-002-case-evidence.md); toàn bộ SQL/Postman execution vẫn NOT_RUN/BLOCKED_G3. Không có Postman collection, migration, fixture, SQL script hoặc source test được tạo/chạy trong lượt review. Expected result là tiêu chí sau khi T01–T08 được PO chốt, không phải bằng chứng PASS hiện tại.

## 1. Điều kiện an toàn trước bất kỳ test ghi dữ liệu nào

### Target isolation là blocker phải giải quyết

`DatabaseTarget` hiện khóa server `lpc:.\MSSQLSERVER01`, catalog `MeChat`, Windows Authentication có encryption. Runtime DI và design-time factory dùng guard này. Chỉ đặt connection string `MeChat_BcrTest` sẽ không hoạt động; không đề xuất nới guard production để chạy test.

T07 phải chọn và định danh một phương án:

1. **Khuyến nghị: Windows VM/host SQL cô lập**, SQL instance/catalog có cùng tên để giữ guard, nhưng máy vật lý/VM, volume và dữ liệu tách hoàn toàn môi trường hiện có. Manifest ghi host/VM identifier, instance, catalog, SQL version/compatibility, backup path được bảo vệ, operator và quyền được cấp. Chỉ dùng fixtures tổng hợp đã được phép, không clone private uploads hoặc DB chứa dữ liệu thật tùy tiện.
2. **Test-only DI/DbContext fixture override được duyệt riêng**, cấu hình SQL disposable cụ thể và fail-closed khi thiếu allowlist/test marker. Không thay `DatabaseTarget` hoặc fallback sang DefaultConnection. WebApplicationFactory phải thay đúng registration của context/options và kiểm tra host không vô tình khởi chạy worker thật. Design-time migration target cho phương án này cũng cần quy trình test riêng được review, không mặc định CLI hiện có nhận catalog tùy ý.

Không chạy nguyên bộ `dotnet test` hiện tại: `FriendIntegrationTests` đăng ký tài khoản, đăng nhập và đọc dev-mail; `RetentionSqlAcceptanceTests` ghi qua DefaultConnection và có seed Admin. Test assembly tắt parallelization không tạo isolation. Tên “test”, “dry-run” hoặc fixture teardown không phải giấy phép dùng DB MeChat hiện có.

Trước G3, phải có: explicit permission cho **đúng host/instance/catalog**, nhận diện target hai lần độc lập, test identity A/B/C do operator provision an toàn, fixture manifest, backup/restore rehearsal và cleanup plan. Không tự cấp Admin; không lấy token từ private uploads/dev-mail. Với auth test, operator dùng phương thức credentials an toàn hiện có; không gửi password/token vào chat, commit hoặc collection.

### Fixtures và bằng chứng

- A là recipient/receiver chính, A2 là phiên thứ hai cùng tài khoản; B là sender/actor khác, C là outsider. Không cần quyền Admin cho lifecycle BCR. Fixture AdminWarning chỉ do operator được cấp quyền chuẩn bị trong DB test nếu cần, không seed Admin tự động.
- Notifications: active unread/read, trash unread/read, hidden message reference, reference null, AdminWarning đúng recipient và reference không khớp. Ghi ID tổng hợp trong manifest bảo vệ; không xuất text/source thật. Friends: Pending unseen/seen, Accepted/Rejected/Cancelled/Blocked, hai hướng và actor khác.
- Dữ liệu nhiều trang: hơn 50, đúng 200 và hơn 200 rows, nhiều CreatedAt bằng nhau ở precision 7; Guid được chọn để .NET order khác SQL order. Mọi insert trực tiếp phục vụ fixture chỉ sau G3 trên target test đã duyệt.
- Postman variables chỉ gồm tên: `baseUrl`, `accessTokenA`, `accessTokenA2`, `accessTokenB`, `accessTokenC`, `notificationId`, `requestId`, `before`, `beforeId`. Token lưu secret/local, không shared/export. Refresh/CSRF/cookie theo contract đang có, không sửa để tiện test.
- Báo cáo lưu case ID, source SHA/build, UTC run time, target alias đã kiểm chứng, fixture alias, expected/actual status và assertions đã redacted. Không lưu Authorization/Cookie headers, raw HAR, DB dump, log SQL parameters hoặc message content. Dùng dữ liệu tổng hợp nếu chụp UI.
- Cleanup ưu tiên hủy nguyên DB/VM disposable theo manifest và quyền riêng sau test. Nếu dùng DB test chung, operator duyệt cleanup chỉ ID fixtures; không dùng delete-all/reset/retention purge. Restore fixture snapshot giữa nhóm destructive tests chỉ trên target cô lập.

## 2. Postman / HTTP acceptance matrix

Chạy trên **API thật + SQL Server thật** sau G2/G3 và BE được triển khai ở test. SignalR cần browser/SignalR client riêng; HTTP Postman đơn lẻ không chứng minh fanout. Hai flags bật cho trường hợp cần tính năng; kiểm riêng flags off. T01/T02/T03/T04/T08 chưa chốt thì expected result tương ứng cũng chưa được khóa.

### BCR-001

| ID | Hành động / dữ liệu | Expected result / assertions |
| --- | --- | --- |
| P-N01 | A DELETE single active unread | 204 empty; absent active; present Trash; ReadAt vẫn null; visible unread count giảm đúng 1 |
| P-N02 | A DELETE single active read | 204; ReadAt/CreatedAt/reference giữ nguyên; unread count không đổi |
| P-N03 | DELETE lại cùng owned visible Trash ID | 204; DeletedAt không bị đổi; không cần event mới khi no-op |
| P-N04 | A POST restore trash unread | 204; active trở lại nếu vẫn visible; DeletedAt null; ReadAt null; count tăng lại |
| P-N05 | Restore trash read rồi restore lặp | 204 mỗi lần; ReadAt giữ nguyên; replay không đổi timestamp khác |
| P-N06 | B/C delete hoặc restore ID của A | 404 NOTIFICATION_UNAVAILABLE cùng shape như unknown; state A không đổi |
| P-N07 | Unknown GUID, Guid.Empty, malformed route | Unknown/empty 404 generic; malformed route framework404; không trả recipient/type/ref |
| P-N08 | A single action trên hidden message notification, gồm replay | Theo T01:404 dù owner; không lộ source; resource access không được khôi phục |
| P-N09 | A DELETE /read với đủ bốn states, owner khác và hidden read | Chỉ A active-read được soft-delete, gồm hidden nếu T01 được chốt; active-unread, existing Trash và B không đổi |
| P-N10 | Bulk khi không có active-read; gửi lại | 204 empty; không counts/ID list; không side effect trên unread |
| P-N11 | GET active/count và /api/unread sau delete | Trash không góp active items/notificationCount; DTO count cũ vẫn count, không đổi thành unreadCount |
| P-N12 | PATCH read lên Trash; POST read-all | Single Trash404; read-all chỉ active unread của A; hidden active vẫn giữ semantics read-all cũ; Trash ReadAt không đổi |
| P-N13 | GET Trash mặc định, limit 1/50/0/51, non-int | Default30/clamp1–50 như T02; non-int400; items chỉ owner+trashed+visible |
| P-N14 | Nhiều trang cùng CreatedAt, roundtrip full cursor | DESC CreatedAt/SQL Id; không duplicate/skip trong dataset tĩnh; timestamps không round qua JS Date |
| P-N15 | Thiếu nửa cursor pair, invalid GUID/time, Guid.Empty | New Trash400 INVALID_NOTIFICATION_CURSOR; không chạy mutation; active legacy before-only vẫn dùng được |
| P-N16 | 200 hidden rows rồi visible page; tất cả hidden | T02: có thể empty items+hasMore true, cursor tiến; next page đọc tiếp; kết thúc hasMore false/cursors null; không loop |
| P-N17 | Restore/delete/new arrival giữa các lượt GET | Không giả snapshot; state membership có thể đổi; không trả active item trong Trash response tại thời điểm query; client dedupe IDs khi refetch |
| P-N18 | AdminWarning text projection và source reference mismatch | Chỉ recipient warning đúng filter có text; không đọc trực tiếp audit Details theo reference; không thêm exception/storage data |
| P-N19 | Xóa/restore notification tham chiếu message/invite/report | Entity nguồn, attachment/evidence/audit không bị xóa/restore/cấp quyền lại |
| P-N20 | Revoke source permission trước GET/action | Reauthorize theo VisibleAsync hiện có; không cache quyền cũ để bypass; other types không tự thêm business rule |
| P-N21 | Single/bulk/restore khi flag off | New APIs404 BCR_FEATURE_DISABLED; old active filters DeletedAt luôn còn hiệu lực |
| P-N22 | GET list/count/Trash lặp, route-open | Không thay ReadAt/DeletedAt; GET read-only |

### BCR-002

| ID | Hành động / dữ liệu | Expected result / assertions |
| --- | --- | --- |
| P-F01 | B gửi request mới đến A | Existing send201, request Pending/SeenAt null; A unreadCount+1, B count không tăng do outgoing |
| P-F02 | A GET requests và unread-count lặp | GET không mark; Incoming marker thật; Outgoing.SeenAt luôn null theo T04; count chỉ receiver+Pending+null |
| P-F03 | A mark-seen subset IDs thực sự visible | 204; chỉ subset SeenAt=opUtc; Pending status/list/pending count không giảm; unseenCount giảm đúng |
| P-F04 | A mark lại ID đã seen | 204; SeenAt đầu tiên giữ nguyên; no-op không phát count-change event |
| P-F05 | Mixed own/alien/outgoing/unknown/non-Pending IDs | 204; chỉ own Pending unseen đổi; không per-ID result/affected count/oracle |
| P-F06 | Missing/null/empty list, invalid/empty GUID, wrong property shape | T03:400 INVALID_REQUEST_IDS/model errors; batch atomic reject, không partial write |
| P-F07 | 100 raw IDs hợp lệ, duplicates trong limit; 101 raw entries | ≤100 xử lý/dedupe; 101400 kể cả duplicate; không vượt SQL parameter/body limits |
| P-F08 | Body >8KiB và wrong Content-Type | T03/T08:413/415 khi policy được chốt; error không echo body; host/gateway behavior được ghi thực tế |
| P-F09 | A accept/reject một unseen incoming | Existing204; Status/RespondedAt như cũ; unseen count tự giảm; không viết SeenAt như cách accept giả |
| P-F10 | B cancel unseen outgoing đến A | Existing204; A count giảm; event đến receiver A, không gửi payload request sang C |
| P-F11 | Reverse send auto-accept | Existing200 Accepted; friendship/chat semantics giữ nguyên; request rời Pending, đúng receiver count/event |
| P-F12 | A/B block, kể cả replay | Pending chuyển Blocked như source; affected receiver count về đúng; giữ PermissionsChanged; không chỉ hook FriendshipChanged |
| P-F13 | Unfriend/unblock | Không tự tạo Pending hoặc SeenAt mới; count không tăng giả, không hồi sinh request |
| P-F14 | Mark-seen flag off, GET count/list | Endpoint mới404; list DTO/additive behavior theo build mới; request mới vẫn SeenAt null, không chạy lại backfill |
| P-F15 | Frontend hidden tab/offscreen/route-open/account switch | Chỉ visible recipient rows được queue; không mark nền; abort/clear khi đổi account, không replay batch A dưới token B |

### Security, compatibility và realtime chung

| ID | Kiểm tra | Expected result |
| --- | --- | --- |
| P-S01 | Missing/expired/invalid JWT, revoked sid, suspended/deactivated/unverified actor | Giữ auth/session validation hiện tại; challenge phù hợp, không update/emit event; không claim custom401 body sẵn có |
| P-S02 | Appeal-scope token vào BCR routes | Bị từ chối bởi existing scope validation; không mở quyền để tiện test |
| P-S03 | New API request giả recipientUserId/actor, JSON/query thừa | Actor chỉ từ claims; không thao tác dữ liệu actor khác; input không có ownership selector |
| P-S04 | Rate limit429 và server500 có kiểm soát trong test fixture | Safe errors, không stack/SQL/token; UI không retry mutation vô hạn; unknown commit outcome xử lý GET reconciliation |
| P-S05 | Old client + new BE; new client + flags off | Existing routes/status/DTO fields/events hoạt động; feature mới không giả success; không sửa /api/unread.requestCount thành friend count |
| P-S06 | A, A2, B, C subscribe, trigger lifecycle/seen | Event mới chỉ đúng account; A/A2 cùng eventId; batch không IDs list; không notification/message text |
| P-S07 | Drop event, disconnect/reconnect, visibility/focus/online | HTTP refetch phục hồi count/feed/trash; không dựa cộng/trừ event; không yêu cầu delivery exactly-once |
| P-S08 | Publisher failure/cancel sau commit | DB mutation không rollback giả; event mất được HTTP repair; lỗi không log payload nhạy cảm |
| P-S09 | Rollback transaction/no-op | Không phát new event cho transaction rollback; no-op không bắt buộc event; retry callback không nhân đôi trước commit |
| P-S10 | Regression 18 old events và 4 hub invocations | Không rename/payload breaking change; ChatHub join/leave/typing không bị BCR sửa |

P-S04 fault injection chỉ trong harness cô lập đã được phép, không chủ động gây lỗi production. Test revoked account cần operator/test fixture lifecycle phù hợp; không dùng endpoint Admin bằng quyền tự cấp.

## 3. Unit và component plan

Mỗi nhóm có assertions thật về policy/service outcome, không chỉ mock function-name mapping. Pure unit không dùng SQL/Dev Mail/DefaultConnection; chỉ chạy sau G2 khi test source được phép tạo. Test unit pass không chứng minh ExecuteUpdate translation/transaction correctness trên SQL Server.

| ID | Unit/component scope | Assertions trọng yếu |
| --- | --- | --- |
| U01 | Notification lifecycle service + repository stub | Ownership/visibility trước idempotency theo T01; unknown/alien indistinguishable; ReadAt/CreatedAt được giữ |
| U02 | Conditional mutation result handling | Affected0 re-read phân biệt active no-op với Trash/invisible; không stale tracked SaveChanges ghi đè DeletedAt |
| U03 | Bulk read-only service | Gọi đúng atomic repository operation một opUtc; không SELECT-all rồi delete; no-op204; no per-ID response |
| U04 | Active/Trash DTO projection | Separate DTO; non-null DeletedAt cho Trash; scoped AdminWarning lookup; null-safe reference/text |
| U05 | Pagination service boundaries | 0/1/limit/limit+1 visible; raw199/200; hidden-only; returned-last vs raw-last cursor; no infinite next token |
| U06 | Friends mark-seen validation/service | Raw1–100; dedupe; reject whole invalid batch; monotonic first SeenAt; actor receiver only |
| U07 | Friend Pending transitions | Create/reverse/respond/cancel/block schedule recipient invalidation; existing Changed/Permissions events preserved |
| U08 | Atomic helper callbacks | Clear on retry; emit only after commit; zero affected no new count event; publisher failure not undo state |
| U09 | Options/errors/controller contracts | Flags independent; local safe codes and204 empty; literal routes not GUID collision; old endpoints unchanged |
| U10 | Frontend visible-ID queue | Intersection/foreground/dwell policy approved trước test; chunk≤100; unmount/account/logout clear, no background route-open mark; retry revalidates visibility |
| U11 | Frontend notifications/trash/count | Confirm “Xóa thông báo đã đọc”; restore keeps read meaning; no fake counter; errors/loading/empty; local query isolation |
| U12 | Frontend event reconciliation | Three new names invalidation, dedupe eventId, no count arithmetic; unknown/old payloads accepted, reconnect/focus fallback |

Component/browser sau G2: keyboard/focus/dialog return, Vietnamese copy/errors, Light/Dark, mobile/desktop320/390/768/1366/1440, text wrapping, composer không bị toast/trash overlay che. UI evidence dùng fixtures tổng hợp; không mượn screenshot cũ để PASS Trash mới.

## 4. SQL Server integration, migration và concurrency plan

Thực thi EF Core SQL Server10.0.12 theo source và SQL engine/compatibility của target đã duyệt. SQLite/InMemory chỉ hỗ trợ unit, không thay bằng chứng SQL. Mỗi concurrent operation dùng DbContext/connection riêng, coordination barrier chỉ trong test harness; không chia sẻ context song song.

| ID | Test thật trên SQL cô lập | Expected evidence |
| --- | --- | --- |
| I01 | Migration từ đúng hai historical migrations | Hai nullable datetime2(7), no default; existing schema/constraints/FKs/unique pair giữ nguyên; migration history thêm đúng1entry |
| I02 | Backfill dataset Pending/non-Pending trước cutover | Một UTC instant cho old Pending null; non-Pending null; existing marker không overwrite trong rehearsal có marker; Notifications đều active |
| I03 | Chặn ingress và drain old transaction rồi migrate | Không còn writer cũ; pending tạo sau resume luôn null, kể cả app clock skew/backdated CreatedAt; flags không trigger backfill |
| I04 | Migration failure injection trong disposable clone | Transaction/EF history nhất quán theo generated script thực tế; rollback hoặc recovery được chứng minh, không giả mọi DDL đều atomic |
| I05 | Generated SQL page + precision/tie order | Predicates owner/state/keyset server-side; exact SQL ordering; >200 identical CreatedAt không skip/dup; giữ đủ fractional timestamp precision |
| I06 | Hidden200/400/mixed page scan | Cursor monotonic theo SQL; bounded work; empty-success semantics T02; permission-hidden items không lộ trong payload |
| I07 | Bulk delete concurrent new unread/read-all/mark-read | Không row ReadAt null bị DeletedAt set; mark-read không ghi lên Trash; row vừa read có thể đủ điều kiện theo T05; no lost state |
| I08 | Delete vs delete, restore vs restore, delete vs restore | No-op monotonic timestamp; chỉ first effective write của cùng state; opposing transitions serialize tại row; không hứa replay chống opposing action |
| I09 | Mark-seen vs seen/accept/reject/cancel/block | SeenAt first-write-only nếu còn Pending tại update; status/RespondedAt không lost update; count khớp SQL predicate sau convergence |
| I10 | Existing send reverse/concurrent normalized-pair race | Unique Pending pair giữ nguyên; retry scope như source; existing statuses/HTTP behavior không đổi; callbacks sau commit |
| I11 | Permission revoke giữa check và state write | Ghi rõ request-time consistency T05; không claim multi-table atomic authorization nếu không có lock design; GET sau revoke không trả hidden source |
| I12 | Rollback/cancel/deadlock/retry | No event trước commit; retries bounded theo approved policy; query/transaction không stale overwrite; postcommit cancellation được HTTP repair |
| I13 | Index alternatives với workload test được duyệt | Execution plans/reads/duration/log cost trước-sau, không Status max key; không drop existing indexes chỉ vì candidate có tên mới |
| I14 | Backward compatibility/rollback rehearsal | Old FE trên compatible BE; flags off vẫn ẩn Trash; no old BE serving after delete; giữ SeenAt data khi rollback feature |

Migration `Up` cần được review cùng generated SQL trước G3, nhất là default/nullability/backfill placement/transaction suppression/timeout. Không chạy Down tự động trong tests nhắm DB hiện có. Reapplying same migration không được chạy backfill lần hai; tự chép UPDATE vào startup/hotfix là không được phép.

Performance acceptance chưa có SLA định lượng: trước G4 phải chốt notification volume, unread volume, số Pending, concurrency, p95/p99 latency, max locking/window, temp/log/disk budget. Source hiện unread scan materializes rows và kiểm visibility từng row; candidate index không giải quyết tất cả N+1/permission-query cost. Không coi dataset nhỏ PASS là production scalability.

## 5. Deployment và backfill runbook đề xuất

Mỗi bước dưới chỉ là hướng dẫn tương lai, **chưa thực hiện**. Không có production target approval trong task này.

### A. Trước G3/G4

1. PO chốt T01–T08, ký G1 và cấp G2 rõ scope. Implement ở lượt sau; unit/client regression và source diff review phải đạt trước SQL deployment.
2. Operator ghi exact host/instance/catalog, process topology và tất cả writer/job; kiểm tra read-only metadata sau quyền phù hợp: SQL version, compatibility, migration history, actual column/index definitions, row counts, open transactions, database recovery model, free space/log usage. Hiện mọi dữ liệu live này NEEDS_VERIFICATION.
3. Đối chiếu actual schema với migration source; drift thì dừng, không “sửa nhanh” DB. Kiểm script additive và single-UTC backfill, nguyên tắc down/rollback, estimated locks và duration trên clone cô lập.
4. Duyệt maintenance window, notice và owner on-call; RPO, RTO, maximum write pause/lock wait, rollback threshold phải có số cụ thể. Package không tự đặt số không có capacity evidence.
5. Manifest xác nhận `FileRetention:RunScheduled`, `FileRetention:EnableDeletion`, `RetentionMaintenance:RunScheduled`, `RetentionMaintenance:EnablePurge` off trong test rehearsal; production operator kiểm theo window được duyệt, không thay business retention hoặc chạy purge. Cả hai BCR feature flags mới mặc định off.

### B. Backup và restore rehearsal

- Operator DBA chọn full backup phù hợp recovery model/log chain, kiểm backup encryption/access control/checksum và đủ dung lượng. Nếu dùng full để không đổi differential baseline thì xem xét COPY_ONLY theo quy trình DBA; không tự đổi recovery model để thuận test.
- Bảo vệ backup ngoài workspace/Git, encrypted/off-host theo chính sách; không gửi bản sao DB vào chat hoặc tài liệu công khai. Ghi reference ID, thời điểm UTC, target metadata và restore point; không copy secret/PII vào báo cáo.
- Kiểm VERIFY/checksum và **restore thử trên SQL host cô lập**, kiểm integrity/schema và app smoke. VERIFY đơn lẻ không chứng minh restore time hay recovery thành công. Kiểm log chain/tail-log khả dụng nếu dự kiến point-in-time restore.
- Ghi thời gian restore đo thực tế, RPO/RTO được PO/operator chấp nhận, người có quyền restore và nơi lưu runbook kín. Chưa có bằng chứng backup/restore ở lượt này; G4 không thể dựa vào lời hứa backup.

### C. Cutover được khuyến nghị: maintenance có write quiescence

1. Chặn request ghi mới ở ingress; drain HTTP/hub work đang chạy, DB transactions và mọi background/ngoài-app writer trên tất cả nodes. Không chỉ tắt UI/flag. Stop old nodes khỏi read traffic trước khi compatible build hoạt động.
2. Xác nhận backup/restore point cuối cùng và không còn writer cũ có thể commit Pending sau backfill. Ghi thời điểm UTC và operator sign-off.
3. Apply đúng migration/script đã review theo G3/G4; trong migration transaction thêm nullable columns, capture một DB UTC instant và backfill **Status Pending + SeenAt null**. Không lọc CreatedAt/timezone app; không backfill mới mỗi startup. Chỉ số mới không tự thêm nếu T08 chưa có plan evidence.
4. Kiểm history/columns và invariants bằng số tổng hợp trên target được phép: no old Pending unseen sau quiescent backfill; new notification DeletedAt null; original row counts/unique pair/FK giữ nguyên. Không export rows.
5. Deploy toàn bộ compatible BE, flags off; mọi active query/read path đã hiểu DeletedAt; app không auto-migrate. Kiểm schema/code readiness trước resume; thiếu columns thì không đưa node vào traffic, không catch lỗi rồi fake empty lists.
6. Resume traffic khi toàn bộ writer là build mới. Pending mới luôn SeenAt null kể cả BCR-002 flag off. Không rolling old/new BE khi bật soft-delete.
7. G3: chạy ma trận trên target test được phép, cleanup theo manifest. G4/G5: chỉ chạy live acceptance write đã được cấp riêng bằng data/test identity được cho phép; không chạy nguyên regression SQL destructively trên DB thật.
8. Enable BCR-001/BCR-002 từng feature sau evidence/release approval; FE enable sau BE/schema readiness. Canary theo actor/account được phép nếu infra hỗ trợ, không giả định có rollout SDK/capability API hiện hành.
9. Monitor status/error code rates, query latency/locks/deadlocks, count mismatch qua aggregate metrics, event delivery warnings và HTTP convergence. Không log token/content/ID batches. Nếu threshold vượt mức đã chốt, flags off, giữ compatible BE và schema.

Backfill làm badge old Pending không tăng, nhưng có tác động ghi số lượng lớn/log/locks. Duration/log-growth/index build chưa thể kết luận từ source. Không hứa zero downtime; nếu không được maintenance window thì phải quay lại thiết kế cutover generation/writer coordination trước G2, không thay bằng `CreatedAt < deployTime`.

## 6. Rollback matrix

| Tình huống | Hành động đề xuất | Bảo toàn dữ liệu / điều cấm |
| --- | --- | --- |
| Trước schema apply | Dừng release/giữ flags off | Không có schema để Down; không reset dữ liệu |
| Schema additive đã apply, chưa backfill/traffic mới | Operator kiểm trạng thái script/history; ưu tiên giữ columns, fix-forward hoặc compatible build theo rehearsal | Không tự chạy Down nếu chưa biết transaction outcome |
| Backfill đã commit, feature chưa bật | Giữ SeenAt và columns; compatible BE flags off | Không clear SeenAt toàn bộ vì không phân biệt backfill và seen thật sau resume nếu không có evidence/marker |
| Có soft-delete traffic | Tắt new endpoints/UI bằng flags, giữ BE luôn lọc DeletedAt; hotfix compatible nếu cần | **Không quay BE cũ** không biết DeletedAt; không set DeletedAt=null để “rollback” khiến Trash hồi sinh |
| Có mark-seen traffic | Tắt count/mark endpoints/UI theo flag; giữ markers, list privacy projection | Không drop SeenAt/overwrite timestamps, không tự chuyển Pending statuses |
| SignalR delivery outage | HTTP focus/online/reconnect/foreground refetch, theo dõi convergence | Không undo DB vì event mất; không gửi payload private qua transport khác |
| Migration lỗi/timeout | Dừng writers, kiểm transaction/history bằng operator; phục hồi theo rehearsed failure path | Không assume migration atomic chỉ từ C# source; không chạy lại mù backfill trên resumed data |
| Corruption hoặc không thể fix-forward | PO/DBA quyết restore point và loss window; isolate target, restore theo rehearsal, đối chiếu toàn hệ thống trước resume | Full DB restore có thể mất **mọi** write sau restore point, không chỉ BCR; phải cấp quyền riêng, không hứa lossless |

Rollback scripts/Down nếu được sinh sau G2 chỉ phục vụ review và disposable rehearsal. Hot rollback ưu tiên compatible app + flags; drop columns làm mất Trash/seen state và không nằm trong quyền hiện tại. Không tác động message/evidence/source hoặc lịch sử Git.

## 7. Evidence checklist và điều kiện trình duyệt tiếp theo

| Gate / deliverable | Hiện tại | Cần để chuyển bước |
| --- | --- | --- |
| G0 D1–D6 | BUSINESS_APPROVED | Không hỏi lại cùng nghiệp vụ |
| G1 T01–T08 + contracts | TECHNICAL_APPROVED | PO đã duyệt2026-11-08, không hỏi lại |
| G2 source/migration creation | SOURCE_IMPLEMENTATION_AUTHORIZED; COMPLETE_SAFE_BOUNDARY_ONLY | Source/migration/tests và safe results trong G2 report; không cấp DB permission |
| G3 disposable SQL tests | NOT_AUTHORIZED | Named target/isolation/credentials/fixtures/backup/cleanup + quyền ghi cụ thể |
| G4 real database migration | NOT_AUTHORIZED | Accepted rehearsal, backup restore evidence, numeric RPO/RTO/window, exact target approval |
| G5 release enablement | NOT_AUTHORIZED | Live acceptance + old-client/SignalR/security/rollback evidence và PO release decision |

Evidence G2 đã liên kết từng P/U/I group trong [73-case ledger](bcr-001-002-case-evidence.md), kết quả và timestamps trong [G2 report](bcr-001-002-g2-report.md). Fail/skip/blocked giữ nguyên; không nhận SQL PASS từ adapter/Health200 hoặc HTTP simulation. SQL/Postman toàn bộ chưa chạy; baseline Frontend cũ vẫn chỉ là lịch sử.

**Next Exact Action:** Product Owner review G2 source/results và [G3 Approval Package](bcr-001-002-g3-approval-package.md), định danh exact isolated target/fixtures/backup/cleanup rồi quyết định G3 riêng. Agent dừng trước apply/ghi SQL/Postman integration; không xin duyệt lại T01–T08/G1/G2 hoặc tự đề nghị G4.
