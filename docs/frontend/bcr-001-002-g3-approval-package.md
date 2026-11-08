# BCR-001/BCR-002 — G3 Test Environment Approval Package

Ngày chuẩn bị: **2026-11-08**. **BUSINESS_APPROVED / TECHNICAL_APPROVED / SOURCE_IMPLEMENTATION_AUTHORIZED**. T01–T08 và G1/G2 do Product Owner duyệt. **G3/G4/G5 NOT_AUTHORIZED**. Tài liệu này trình phạm vi test tiếp theo, không cấp quyền hoặc chứa lệnh apply DB.

G2 source/results xem [implementation report](bcr-001-002-g2-report.md); thiết kế/runbook xem [package kỹ thuật](bcr-001-002-implementation-approval-package.md), [test/rollout plan](bcr-001-002-test-and-rollout-plan.md).

## 1. Phạm vi G3 đề nghị PO xem xét

Chỉ một **SQL Server disposable target được định danh chính xác**, hoàn toàn không dùng dữ liệu đang có của MeChat. PO/operator tự provision target và phương thức xác thực an toàn. Chưa có host/catalog/credentials/backup cụ thể nên **BLOCKED**. Không suy từ G2 rằng đã được tạo/reset/clone/seed/apply/test SQL.

Sau khi PO phê duyệt riêng, phạm vi cần được ghi rõ: ai provision DB; ai apply đúng migration/script; cho phép tạo synthetic User-role fixtures; chạy project SQL riêng và Postman folders theo thứ tự/fixtures; isolation/backup và cleanup do ai thực hiện. Không seed Admin, không clone dữ liệu thật hoặc private uploads, không mở retention/purge. Mọi thao tác chưa được ghi trong approval vẫn bị khóa.

Không đề nghị G4 hoặc deployment lên DB đang sử dụng ở checkpoint này. Trước hết phải có G3 evidence thật.

## 2. Manifest bắt buộc — hiện CHƯA ĐIỀN/CHƯA DUYỆT

| Trường | Operator/PO phải cung cấp qua kênh phù hợp |
| --- | --- |
| Approval reference | Mã G3 riêng, scope và người phê duyệt, ngày giờ |
| SQL host/instance | Exact host/VM identity + instance/port, không chỉ alias chưa kiểm chứng |
| Catalog | Tên riêng bắt đầu `MeChat_BcrTest_`; không `MeChat`, không fallback DefaultConnection |
| Engine | SQL version/compatibility/collation và EF SQL Server10.0.12 parity |
| Provisioning/migration | Ai thực hiện, quyền cụ thể, initial history/schema, maintenance/quiescence, có cho apply BCR trên target này hay không |
| Backup/recovery | Protected backup reference/restore rehearsal, free space/log budget, rollback owner/window |
| Fixtures | Synthetic A/B/C + A2 sessions; các trạng thái/IDs cần cho mỗi Postman folder; không dữ liệu người dùng thật |
| HTTP test host | Exact base URL/host identity và chứng cứ DbContext được override sang đúng test catalog, workers/retention tắt |
| Auth | Cách cấp sessions JWT/refresh/CSRF thực qua secret/local environment; không gửi token/password vào chat |
| Cleanup | Operator-approved teardown của DB disposable hoặc cleanup chỉ fixture IDs; không test teardown hard-delete notifications tự động |
| Limits/acceptance | Thời gian/locking budget, query latency/load, abort criteria và người giữ quyền dừng |

## 3. Fail-closed test infrastructure có source

- `backend/MeChat.Bcr.SqlTests/SqlTestTarget.cs`: chỉ parse/validate strings, không kết nối. Phải có approval reference, allowlisted host, exact isolated catalog, encrypted Windows Authentication. Reject missing connection/unsafe target, catalog MeChat, attach/failover/user-instance; không fallback.
- `ApprovedSqlFixture`: chỉ dùng context test riêng sau guard; không Migrate/EnsureCreated/reset DB. Khi được G3 cho chạy, kiểm migration history đã có BCR trước synthetic fixtures. Không tạo Admin; không tự cleanup record đã xóa mềm.
- `BcrTestDbOverride`: helper **test-only** thay DbContext/options registrations sau guard, không thay DatabaseTarget production. Chưa khởi động HTTP host bằng helper này. Operator/harness tương lai phải chứng minh registration cuối, đúng host/catalog và mọi worker cần thiết đều tắt trước request ghi.
- `MeChat.Bcr.UnitTests` tách project SQL, chạy không DbContext/SQL. Guard tests chỉ kiểm string validation; không phải isolation runtime certification.

Các environment variable **names only**, hiện không có giá trị được cấp trong package:

```text
MECHAT_BCR_G3_APPROVAL_REFERENCE
MECHAT_BCR_SQL_ALLOWED_HOST
MECHAT_BCR_SQL_DATABASE
MECHAT_BCR_SQL_CONNECTION
```

Environment interlock không tự tạo quyền. Không đặt các giá trị này trong G2 để thử kết nối. Không dùng normal app/design-time factory với connection string test tùy ý: production guard vẫn khóa target cũ. Phương án SQL VM cùng tên catalog MeChat trong review lịch sử không dùng được với fixture hiện tại; helper này chủ ý chỉ nhận catalog riêng theo T07 cuối, không nới guard production.

## 4. Artifacts để PO/operator review trước G3

| Artifact | Tình trạng |
| --- | --- |
| Migration `20261108144829_NotificationTrashFriendSeen.cs`, Designer + Snapshot | GENERATED, NOT_APPLIED; hai nullable datetime2(7), no defaults/indexes, one-time Pending backfill |
| [SQL review-only](sql/bcr-001-002-review-only.sql) | GENERATED OFFLINE, NOT_EXECUTED; chỉ target G3 được duyệt mới được cân nhắc apply |
| `backend/MeChat.Bcr.SqlTests/` | BUILD checked; **NOT_RUN / BLOCKED_G3**; repository predicates, SQL Guid tie-order, state races và real FriendService transaction races |
| [Postman collection](postman/MeChat-BCR-001-002.postman_collection.json) | 47 acceptance folders, 104 requests; **NOT_RUN / BLOCKED_G3**, không secrets |
| [73-case evidence ledger](bcr-001-002-case-evidence.md) | Phân biệt safe checks với integration/manual assertions còn thiếu |

Postman cần local secret environment với `baseUrl`, `approvedBaseUrl`, `g3ApprovalReference`, `testDatabaseName`, account tokens và fixture IDs. Collection variables để trống. Pre-request interlock skip request nếu thiếu approval/target; không log/auto-export tokens. Mỗi folder dùng fresh fixtures; không chạy tuần tự toàn collection với cùng mutable IDs rồi nhận PASS giả. Không auto-delete fixtures qua production APIs.

HTTP status assertions không đủ cho concurrency/ownership/visibility: phải kiểm DB state đúng target, metadata fanout tới A/A2 và không tới C, rollback/no-event, reconnect HTTP repair. Postman không tự chứng minh SignalR. Các manual prerequisites được giữ trong từng folder; không gọi chúng PASS chỉ vì HTTP assertion xanh.

## 5. Trình tự sau khi được G3 riêng

1. Operator xác minh target/approval/backup và provision theo manifest. Đối chiếu source/schema/history, không dùng DB MeChat hiện có.
2. Review Up/Down/script, hai columns và single UTC backfill. Nếu được phép apply, thao tác thuộc người/quyền trong G3, dưới maintenance write quiescence. Không clock/CreatedAt cutoff. Rehearsal backfill/schema failure/restore cần target riêng và quyền tương ứng.
3. Kiểm post-cutover old Pending seen, non-Pending giữ nguyên, new Pending null, flags off không rerun. Không áp dụng schema dựa vào Health200.
4. Chạy SQL project đúng target theo quyền G3. Theo dõi rows/logs/deadlocks, synthetic fixtures only; giữ trạng thái FAIL/BLOCKED/NOT_RUN đúng bằng chứng.
5. Khi HTTP test host đã được override/kiểm chứng và credentials được cấp an toàn, chạy Postman từng folder và true JWT/CSRF/session/SignalR/two-account checks. Không dùng test authentication của pure HTTP tests làm evidence JWT.
6. Operator cleanup theo manifest đã duyệt, không tác động dữ liệu khác. Tổng hợp evidence, limitations và đề nghị PO review kết quả G3; không tự chuyển release gate.

**Next Exact Action:** Product Owner review G2 source/evidence, rồi quyết định G3 với exact target và manifest trên. Agent không mở SQL connection/apply migration hoặc chạy integration/Postman ghi dữ liệu khi chưa có approval này.
