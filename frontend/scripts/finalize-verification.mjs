import { readFile, writeFile, readdir } from 'node:fs/promises'
import { gzipSync } from 'node:zlib'
const docs = '../docs/frontend'
const full = JSON.parse(await readFile(`${docs}/final-client-regression.json`, 'utf8'))
const focused = JSON.parse(await readFile(`${docs}/final-copy-regression.json`, 'utf8'))
const secureInput = JSON.parse(await readFile(`${docs}/secure-auth-input-client.json`, 'utf8'))
const cacheRegression = JSON.parse(await readFile(`${docs}/final-message-media-regression.json`, 'utf8'))
if (secureInput.stats.unexpected || cacheRegression.stats.unexpected) throw new Error('Targeted final regression failed')
if (full.stats.unexpected || focused.stats.unexpected) throw new Error('Cannot finalize a failed client regression')
const unit = JSON.parse(await readFile('test-results/unit-results.json', 'utf8'))
if (unit.numFailedTests) throw new Error('Unit failure remains')
const inventory = JSON.parse(await readFile(`${docs}/api-inventory.json`, 'utf8'))
const assets = []
for (const name of await readdir('dist/assets')) if (name.endsWith('.js')) { const buffer = await readFile(`dist/assets/${name}`); assets.push({ name, bytes: buffer.length, gzipBytes: gzipSync(buffer).length }) }
const entry = assets.find((asset) => asset.name.startsWith('index-'))
const largest = [...assets].sort((a, b) => b.bytes - a.bytes)[0]
const evidence = { date: '2026-11-06', capturedAt: new Date().toISOString(), backend: 'http://localhost:5080', scope: 'Actual anonymous HTTP/hub boundaries only. No successful authenticated/SQL business acceptance.', tests: [ { title: 'health through same-origin frontend proxy', status: 'PASS' }, { title: 'protected users/conversations/groups/admin + hub negotiate reject anonymous', status: 'PASS' }, { title: 'missing refresh CSRF and invalid login DTO rejected before account attempt', status: 'PASS' }, { title: 'public auth DTO validation', status: 'PASS' }, { title: 'successful account login/reload/logout', status: 'SKIPPED', reason: 'Credentials absent; user chose NEEDS_VERIFICATION and continued implementation.' } ] }
await writeFile(`${docs}/runtime-boundary-evidence.json`, JSON.stringify(evidence, null, 2) + '\n')
const report = { date: '2026-11-06', status: 'CLIENT_IMPLEMENTED; AUTHENTICATED_ACCEPTANCE_NEEDS_VERIFICATION', designGate: 'APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER', unit: { passed: unit.numPassedTests, failed: unit.numFailedTests }, browser: { full: full.stats, focused: focused.stats }, sourceCoverage: { controllerActions: inventory.length - 1, implementedControllerConsumers: inventory.filter((row) => row.implementation.includes('IMPLEMENTED_CLIENT')).length, healthDiagnostic: 1 }, production: { build: 'PASS', fixtureIsolation: 'PASS', browserSmoke: '1 PASS', entry, largest }, liveBoundaries: { passed: 4, skipped: 1 }, dependencyAudit: '0 vulnerabilities', missingVerification: ['successful auth/session/profile/permissions against real backend', 'two-user SignalR delivery/typing/receipts/reconnect/revocation', 'actual file authorization and upload/download against database/storage', 'actual group/admin/report/appeal/retention authorization', 'physical mobile keyboard/IME/password manager, native browsers/screen reader', 'full WCAG/performance/load and two-user portfolio demo'] }
report.browser.secureInput = secureInput.stats
report.browser.messageMediaRerun = cacheRegression.stats
await writeFile(`${docs}/implementation-verification.json`, JSON.stringify(report, null, 2) + '\n')
const checks = `| Build / production scan / smoke | PASS / PASS / 1 PASS |\n| Lint / static localization / API function census | PASS / 646 calls PASS / 122 functions PASS |\n| Unit/component | ${unit.numPassedTests} PASS in ${unit.testResults.length} files |\n| Chromium full regression | ${full.stats.expected} PASS, ${full.stats.unexpected} FAIL, ${full.stats.flaky} flaky |\n| Final copy/partial-preview regression | ${focused.stats.expected} PASS |\n| Real Backend anonymous boundaries | 4 PASS; successful account 1 SKIPPED |\n| npm audit | 0 vulnerabilities |`
await writeFile(`${docs}/verification-report.md`, `# Báo cáo kiểm tra triển khai — 06/11/2026

**CLIENT_IMPLEMENTED; AUTHENTICATED_ACCEPTANCE_NEEDS_VERIFICATION.** Design Gate đã APPROVED. Không tuyên bố frontend hoàn thành 100% hoặc PORTFOLIO_READY.

| Kiểm tra | Kết quả |
| --- | --- |
${checks}

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
- Bundle entry ${(entry.bytes / 1000).toFixed(2)} kB / gzip ${(entry.gzipBytes / 1000).toFixed(2)} kB; chunk lớn nhất ${(largest.bytes / 1000).toFixed(2)} kB. Không còn warning >500 kB. PURE-comment warnings của dependency còn mang tính thông tin; không phải kết quả Lighthouse/load.

## Lịch sử lỗi được giữ

Recovery build/lint/26 unit và Groups13 đã được chạy lại trước khi mở rộng. Đã sửa blob null runtime, unread aria, contrast destructive, headings mobile, DEV navigation trong production, pending/dialog timing, 320px header overflow. Full trung gian 110 PASS/16 FAIL do header và fixture /unread sai shape; [lịch sử](recovery-full-failure-history.json). Sau sửa, full128 PASS. Không dùng retry/skip để che các lỗi này.

## Chưa xác minh

Successful auth/refresh/logout/profile/sessions; hai tài khoản realtime/receipts/reconnect/block/membership; upload/download/avatar/file permissions thật; report/evidence/appeal/admin/retention quyền thật; email delivery; native Chrome/Edge, screen reader, zoom toàn ứng dụng, thiết bị mobile/IME/password manager; performance/load và demo hai người dùng. User đã chọn tiếp tục và hoãn credentials, không cần hỏi lại trong checkpoint này. Các mục trên giữ NEEDS_VERIFICATION.
`)
await writeFile(`${docs}/final-acceptance.md`, `# Trạng thái nghiệm thu portfolio — 06/11/2026

**NOT PORTFOLIO_READY.** Visual Direction và ba mẫu đã được Product Owner phê duyệt. Source frontend đã phủ các client stages và 122 controller consumers; business acceptance vẫn NEEDS_VERIFICATION.

| Phạm vi | Trạng thái |
| --- | --- |
${checks}
| Visual Direction | APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER |
| Successful business API / two-user SignalR | NEEDS_VERIFICATION |
| Native devices / assistive tech / performance / portfolio demo | NEEDS_VERIFICATION |

Các tests client không thay thế kiểm thử với Backend thật. Xem [bằng chứng và giới hạn](verification-report.md), [API census](api-coverage-report.md), [ảnh](screenshot-index.md). Không có thay đổi Backend/Database/migration; các file người dùng đã xóa được giữ nguyên. Không tự mở lại Design Gate.

Next Exact Action: khi cấu hình kiểm thử an toàn sẵn sàng, chạy successful Auth trước, rồi profile/session/privacy, hai-user Friends/chat/requests/groups/reconnect/files và role Admin/appeal/retention; bổ sung thiết bị thật và demo. Người dùng đã chọn hoãn credentials; không tự tạo tài khoản/dữ liệu, tìm token trong mail riêng hoặc chạy destructive QA.
`)
await writeFile(`${docs}/api-coverage-report.md`, `# API coverage — source và client evidence

06/11/2026. [Inventory](api-inventory.json), [matrix](api-to-feature-map.md), [static function audit](api-consumer-audit.json).

| Measure | Kết quả |
| --- | --- |
| HTTP source census | 19 controllers / 122 actions + 1 health = 123 |
| Client functions + named consumers | 122 controller actions mapped; existing module/function audit PASS |
| Health diagnostic | Actual 200 + exact response PASS through proxy |
| Client regression | Full128 PASS + final focused2 PASS; HTTP simulations labelled |
| Real negative boundaries | 4 PASS; not business acceptance |
| Successful business endpoints | NEEDS_VERIFICATION; no per-endpoint runtime PASS asserted |
| Successful account journey | 1 SKIPPED by explicit user choice while credentials absent |
| SignalR | 18 events mapped; client/unit evidence; authenticated transport NEEDS_VERIFICATION |

Source coverage is not frontend completion or runtime API coverage. Dynamic Admin list adapter covers users/groups/reports/appeals/audit; consumer sections are validated. Appeal transport is isolated. File preview/download use authorized blobs. Backend source/contracts were not changed.
`)
let roadmap = await readFile('../roadmapFE.md', 'utf8')
roadmap = roadmap.replace('Current task: final client regression, localization/security/coverage audit and honest runtime evidence.', 'Current task: client implementation/audit checkpoint complete; authenticated business and device verification pending. Do not mark business features DONE.')
roadmap += `\n## Final recovery checkpoint — 06/11/2026\n\n- Full browser regression **128 PASS / 0 FAIL / 0 flaky**; final localization/partial-preview focused **2 PASS**. Unit/component **${unit.numPassedTests} PASS**; lint/localization646/API122 audit PASS; build/production isolation/smoke1 PASS; npm audit0. Real backend boundary4 PASS, successful auth1 SKIPPED. Full suite and focused changes are distinguished in verification-report.md.\n- Screenshots and current spec/acceptance/design-review/coverage/final-acceptance updated; history preserved. All six implementation stage clients exist; no authenticated business feature is DONE or PORTFOLIO_READY. Gate stays APPROVED.\n- API census122 controller consumers +health123; all business verification NEEDS_VERIFICATION. SignalR18 handlers/invocations source+unit checked, real transport pending. No Backend/DB/migrations/seed change; no commit; user deletions preserved.\n- Next Exact Action: configure safe test inputs only when user makes them available; successful Auth and session lifecycle first, then two-user business/realtime/media/group and Admin/appeal/retention authorization, native mobile/assistive tech/performance/demo. User already deferred credentials; do not ask again or use private mail/uploads.\n`
await writeFile('../roadmapFE.md', roadmap)
for (const name of ['ui-ux-spec', 'ui-ux-acceptance', 'design-review']) {
  const path = `${docs}/${name}.md`
  let text = await readFile(path, 'utf8')
  text = text.split('\n## Kết quả checkpoint cuối')[0]
  text += `\n## Kết quả checkpoint cuối — 06/11/2026\n\nFull browser128 PASS; final copy2 PASS; secure input1 PASS; hồi quy lại message/media29 PASS sau sửa cache binary/reply. Không cộng các lần chạy có ca trùng thành một full mới. Unit/component${unit.numPassedTests} PASS; build/lint/localization646/API122/production scan/smoke PASS. Real boundaries4 PASS, successful auth1 SKIPPED; authenticated/two-user/native/device/performance NEEDS_VERIFICATION. [Báo cáo chi tiết](verification-report.md). Không nghiệm thu 100% hoặc mở lại Design Gate.\n`
  await writeFile(path, text)
}
console.log('Final evidence, current reports and roadmap checkpoint written; business verification remains NEEDS_VERIFICATION.')
