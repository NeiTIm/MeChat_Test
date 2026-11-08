"""Generate the 52-ticket review from unchanged OOXML and explicit audit decisions."""
import hashlib
import importlib.util
import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('workbook_reader', ROOT / 'scripts/read-improvement-workbook.py')
reader = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reader)
sheets = reader.analyze()
source = ROOT / 'docs/frontend/MeChat_Danh_sach_cai_thien_va_kiem_thu_v1.xlsx'

# Each entry is an audit decision, not a generated assertion that a business API passed.
# status, feature, classification, finding / current evidence
decisions = {
    'MC-001': ('FIXED_NEEDS_VERIFICATION', 'auth', 'CONFIRMED_BUG', 'Ảnh 01 + CSS login-submit dùng space-between làm chữ lệch trái. Đã căn giữa chữ, giữ icon bên phải; chờ tổng hợp matrix đo DOM/focus.'),
    'MC-002': ('BLOCKED_BACKEND_APPROVAL', 'unread', 'IMPROVEMENT_REQUEST', 'Navigation chưa có consumer count. Đã thêm Private/Group từ toàn bộ server unread projection và Notifications từ count API. Không tự giảm khi click. Bạn bè không có unread/read contract: BCR-002.'),
    'MC-003': ('FIXED_NEEDS_VERIFICATION', 'layout', 'IMPROVEMENT_REQUEST', 'Ảnh 03 cho thấy link mặc định, thiếu hệ thống hover/focus. Đã thống nhất token/underline/focus cho text links; chờ review toàn bộ matrix.'),
    'MC-004': ('FIXED_NEEDS_VERIFICATION', 'groups', 'IMPROVEMENT_REQUEST', 'Ảnh 04: link mời chỉ nằm sâu trong quản lý. Thêm CTA sau tạo nhóm và trong Details, tới mục invite-links, không tạo link ngầm. Tạo nhóm thật chưa chạy.'),
    'MC-005': ('FIXED_NEEDS_VERIFICATION', 'groups', 'CONFIRMED_BUG', 'Tái hiện browser: reset workspace cache làm mất URL một lần. GroupChanged thực hiện cùng reset sau tạo link. Lift state lên route, kiểm tra token có thật, copy feedback và expiry; red 1 FAIL -> green 1 PASS. Transport/join/expiry thật chưa chạy.'),
    'MC-006': ('BLOCKED_BACKEND_APPROVAL', 'notifications', 'IMPROVEMENT_REQUEST', 'NotificationsController chỉ có list/count/read/read-all/settings. Không có delete. BCR-001 đã có phương án bảo mật/retention; không giả lập xóa.'),
    'MC-007': ('FIXED_NEEDS_VERIFICATION', 'notifications', 'IMPROVEMENT_REQUEST', 'Ảnh 07 là tham khảo UI. Đã thêm preview dùng chung feed/count/actions, 5 thông báo gần nhất, loading/empty/error/Escape/deep link. DTO chỉ có type/text/reference/time, không bịa tên người gửi hoặc nội dung message.'),
    'MC-008': ('FIXED_NEEDS_VERIFICATION', 'presence', 'IMPROVEMENT_REQUEST', 'Có subtitle API nhưng thiếu chấm trạng thái. Thêm xanh/online, xám/offline, nhãn chữ; null không thành offline. Client checks true/false/null; cần hai user/privacy thật.'),
    'MC-009': ('FIXED_NEEDS_VERIFICATION', 'feedback', 'IMPROVEMENT_REQUEST', 'Ảnh 09 là inline success. Thêm toast success 2s/close/pause, giữ inline/error, preference local theo account. Unit 3 PASS; browser/theme/a11y đang tổng hợp.'),
    'MC-010': ('FIXED_NEEDS_VERIFICATION', 'layout', 'IMPROVEMENT_REQUEST', 'Ảnh 10 dùng zoom 75%; CSS wide message padding lấy toàn viewport thay vì thread, gây dư khoảng trống. Đổi padding cố định theo breakpoint. Chưa lấy viewport hoặc root font làm bằng chứng browser zoom.'),
    'MC-011': ('FIXED_NEEDS_VERIFICATION', 'files', 'CONFIRMED_BUG', 'PNG/JPEG/WebP đúng MIME đã decode được trong baseline simulation; không chứng minh server storage hỏng. Red: ảnh hỏng/MIME JSON không có fallback, thiếu zoom. Đã sửa decode/MIME/retry/thumbnail/zoom. 17 client PASS; live upload/refresh/authorization chưa chạy.'),
    'MC-012': ('FIXED_NEEDS_VERIFICATION', 'files', 'CONFIRMED_BUG', 'Ảnh 12 là broken image trong modal mobile. Cùng thiếu decode fallback như MC-011; chưa chứng minh CSS/auth/storage là nguyên nhân của tệp gốc. Matrix 10 viewport/theme PASS với ảnh tổng hợp; cần file thật và mobile vật lý.'),
    'QA-001': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Không có credentials kiểm thử được cấp phép. Real auth test SKIPPED; 4 anonymous boundaries PASS không chứng minh login/SQL.'),
    'QA-002': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Source single-flight/Web Locks/CSRF hiện có, session unit PASS. Cookie rotation/replay/multi-tab thực tế chưa chạy.'),
    'QA-003': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Source revoke/cache cleanup hiện có. Cần hai browser sessions được phép; không tự lấy token từ dev-mail hoặc private uploads.'),
    'QA-004': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Validation/i18n và mapping lỗi có client tests; public DTO boundary thật PASS. Verified/unverified/lockout/reset-expired trên account thật chưa chạy.'),
    'QA-005': ('BLOCKED_TEST_ENVIRONMENT', 'friends', 'QA_RISK', 'Requests/accept/reject/cancel source và simulation hiện có; cần hai test identities và cleanup được cấp phép.'),
    'QA-006': ('BLOCKED_TEST_ENVIRONMENT', 'friends', 'QA_RISK', 'Có block/unblock/permission invalidation source, chưa có bằng chứng hai tài khoản mất quyền chat đồng thời.'),
    'QA-007': ('BLOCKED_TEST_ENVIRONMENT', 'users', 'QA_RISK', 'Profile/avatar consumers và cache invalidation hiện có. Cần kiểm tra account khác/search/sidebar sau cập nhật thật; FND-001 bổ sung avatar decode fallback.'),
    'QA-008': ('BLOCKED_TEST_ENVIRONMENT', 'messages', 'QA_RISK', 'Hide/list/unread consumers hiện có. ConversationDto không trả latest message preview; không tự tạo nội dung. Hide rồi người kia gửi thật chưa chạy.'),
    'QA-009': ('BLOCKED_TEST_ENVIRONMENT', 'realtime', 'QA_RISK', '18 handlers chỉ có source/unit/simulation; anonymous hub 401 PASS không phải authenticated negotiate/WS101.'),
    'QA-010': ('BLOCKED_TEST_ENVIRONMENT', 'messages', 'QA_RISK', 'Composer giữ clientMessageId theo retry signature; merge theo ID. Chưa đếm persistence/sequence trong SQL thật sau retry/SignalR.'),
    'QA-011': ('BLOCKED_TEST_ENVIRONMENT', 'messages', 'QA_RISK', 'Read cursor theo visible foreground, delivery ACK API, receipt source hiện có. Chưa so sánh hai browser và server cursor thật.'),
    'QA-012': ('INVESTIGATING', 'messages', 'QA_RISK', 'Có scroll anchor và near-bottom tests cũ; cần bổ sung lịch sử 100+ và chiều cao ảnh/resize. Không mặc định đã bị nhảy scroll.'),
    'QA-013': ('BLOCKED_TEST_ENVIRONMENT', 'messages', 'QA_RISK', 'Edit 15 phút/recall/reply/reaction/forward consumers và cache sanitization tests có sẵn. Cần Backend permission/lifecycle thật.'),
    'QA-014': ('CONFIRMED', 'messages', 'IMPROVEMENT_REQUEST', 'Source search mở detail modal, chưa có hành động nhảy tới sequence/ngữ cảnh của tin cũ. Chưa đánh dấu chức năng nhảy đã hoàn thành.'),
    'QA-015': ('BLOCKED_TEST_ENVIRONMENT', 'realtime', 'QA_RISK', 'Reconnect indicator/backoff/reconcile/dedupe hiện có. Cần hai accounts, ngắt mạng 30s và đối chiếu missed events thực.'),
    'QA-016': ('BLOCKED_TEST_ENVIRONMENT', 'presence', 'QA_RISK', 'Typing TTL unit có sẵn và indicator null-safe đã bổ sung. Cần privacy/đa tab/group thực; không suy từ viewer connection.'),
    'QA-017': ('BLOCKED_TEST_ENVIRONMENT', 'groups', 'QA_RISK', 'Owner/Admin/Member UI source và 13 baseline group checks PASS. Chưa có thực nghiệm Backend từ chối trái quyền; không cấp Admin.'),
    'QA-018': ('BLOCKED_TEST_ENVIRONMENT', 'groups', 'QA_RISK', 'Token creation-only, URL fragment, resolve/confirm join consumers có sẵn; sửa mất URL MC-005. Revoked/expired/blocked/join trùng thật chưa chạy.'),
    'QA-019': ('BLOCKED_TEST_ENVIRONMENT', 'groups', 'QA_RISK', 'Invitation consumers hiện có; cần hai/ba accounts và GroupInvitationChanged thật, không mock thành live.'),
    'QA-020': ('BLOCKED_TEST_ENVIRONMENT', 'files', 'QA_RISK', 'Authorized Blob transport hiện có và decode fallback được sửa. Chưa có upload/storage/download positive/negative với authorized files thật.'),
    'QA-021': ('INVESTIGATING', 'files', 'QA_RISK', 'useBlobUrl unit chứng minh revoke khi permission data biến mất. Cần thêm cancel/retry/progress và browser lifecycle; chưa chứng nhận memory profiling.'),
    'QA-022': ('BLOCKED_TEST_ENVIRONMENT', 'files', 'QA_RISK', 'Cảnh báo TXT rút gọn, escaped text/unsupported fallback đã tồn tại, có regression. MIME/encoding/download thực chưa chạy.'),
    'QA-023': ('BLOCKED_TEST_ENVIRONMENT', 'notifications', 'QA_RISK', 'Page/tray/nav dùng chung count/query/actions, mutate xong mới invalidate. Cần NotificationCreated/Read hai tab với server thật.'),
    'QA-024': ('BLOCKED_TEST_ENVIRONMENT', 'notifications', 'QA_RISK', 'Message/GroupMessage/MessageRequest resolve qua authorized message API trước navigate; unknown type không bịa route. Cần actual deleted/forbidden resources.'),
    'QA-025': ('FIXED_NEEDS_VERIFICATION', 'feedback', 'IMPROVEMENT_REQUEST', 'Toast preference local theo userId, default on, không lưu secrets; theme browser-wide hiện có. Unit reload/identity PASS; chờ browser/privacy-mode checks.'),
    'QA-026': ('BLOCKED_TEST_ENVIRONMENT', 'admin', 'QA_RISK', 'DB-role guard source và simulation PASS; anonymous admin401 thật PASS. Chưa có Admin200/User403 thật, không tự cấp quyền.'),
    'QA-027': ('BLOCKED_TEST_ENVIRONMENT', 'moderation', 'QA_RISK', 'Appeal transport/scope/cache riêng và timeout cleanup hiện có. Chưa có disposable suspended identity được cấp phép; không tự đình chỉ account.'),
    'QA-028': ('BLOCKED_TEST_ENVIRONMENT', 'admin', 'QA_RISK', 'UI chỉ preview/explicit dry-run/hold, có confirmation. Không purge execution. Admin actual permissions/audit/hold chưa kiểm thử.'),
    'QA-029': ('INVESTIGATING', 'security', 'QA_RISK', 'Workbook gốc có private screenshots: giữ local và gitignore riêng; bản ảnh tạm đã xem và gỡ. Cần scan source/Git/artifacts chỉ xuất tên/counters, không xuất giá trị secrets.'),
    'QA-030': ('BLOCKED_TEST_ENVIRONMENT', 'layout', 'QA_RISK', 'Axe/keyboard/focus/contrast có browser coverage; chưa có NVDA/native assistive-tech acceptance. Không coi axe là WCAG certification.'),
    'QA-031': ('BLOCKED_TEST_ENVIRONMENT', 'layout', 'QA_RISK', 'Enter/composition/shrunken viewport simulation có sẵn; chưa có Android/iOS keyboard Telex và safe area thật.'),
    'QA-032': ('FIXED_NEEDS_VERIFICATION', 'layout', 'QA_RISK', 'Baseline 5 widths × 2 themes có PASS; header thêm notification tray và thumbnail cần full regression sau sửa.'),
    'QA-033': ('FIXED_NEEDS_VERIFICATION', 'layout', 'QA_RISK', 'vi-VN/Inter/Intl hiện có; static audit sau sửa 679 keys PASS. Vẫn cần audit text động/visual; không dịch user content/contracts.'),
    'QA-034': ('INVESTIGATING', 'layout', 'QA_RISK', 'Bundle split hiện có, thumbnail chỉ fetch gần viewport. Chưa có 500-message CPU/memory profile hoặc Lighthouse/load thật.'),
    'QA-035': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Safe error mapping/unit + client failures hiện có. Cần actual endpoints trả 403/404/409/429, timeout/draft; không hiển thị raw exception.'),
    'QA-036': ('VERIFIED', 'layout', 'ALREADY_RESOLVED_SOURCE', 'Đã kiểm source/index và README: không service worker/offline private caching, không claim installable PWA. README nêu rõ responsive preparation; triển khai PWA thật là scope riêng.'),
    'QA-037': ('BLOCKED_TEST_ENVIRONMENT', 'docs', 'QA_RISK', 'README có BE/FE commands/env/anonymous vs business limits, đã sửa câu design checkpoint cũ. Clean clone/two-account demo/end-to-end chưa nghiệm thu.'),
    'QA-038': ('NOT_REPRODUCIBLE', 'security', 'ALREADY_RESOLVED_SOURCE', 'Git hiện đã có babbcfd feat(frontend): implement MeChat UI and API integration, 167 frontend files tracked trước đợt này. Không còn đúng giả định toàn bộ Frontend chưa có checkpoint. Chưa stage/commit/push đợt sửa mới.'),
    'QA-039': ('BLOCKED_TEST_ENVIRONMENT', 'auth', 'QA_RISK', 'Identity generation/401 actor guard/query clear/hub dispose hiện có, unit PASS. Cần A→B khi upload/API chậm thật; không coi unit là Backend acceptance.'),
    'QA-040': ('FIXED_NEEDS_VERIFICATION', 'feedback', 'QA_RISK', 'Toast chỉ áp dụng success. Error tại form/modal không tự dismiss; preference off không xóa feedback. Unit 3 PASS + browser wait >2s đang kiểm.'),
}

features = {
    'auth': ('frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx', 'API-001…014,114', 'session.test.js; auth-client.spec.js; auth.live.spec.js', 'Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads.'),
    'users': ('frontend/src/features/users; frontend/src/components/Avatar.jsx', 'API-032,114…122', 'auth-client.spec.js; media-notifications-client.spec.js', 'Hai test accounts, file avatar tổng hợp.'),
    'friends': ('frontend/src/features/friends; frontend/src/app/providers/RealtimeProvider.jsx', 'API-033…042,121…122', 'friends-client.spec.js; realtime.test.jsx', 'Hai identities, quan hệ test có quyền tạo và cleanup.'),
    'messages': ('frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat', 'API-015…028,069…082; group history/send API-067/068', 'messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js', 'Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup.'),
    'unread': ('frontend/src/components/layout/NavigationLinks.jsx; frontend/src/features/messages/api.js', 'GET /api/unread; API-108; BCR-002', 'improvement-ui.spec.js; realtime.test.jsx', 'Unread projection thật; Friends contract chờ Product Owner.'),
    'groups': ('frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx', 'API-043…068', 'groups-client.spec.js; improvement-ui.spec.js', 'Hai/ba test users và group disposable; không đổi business permissions.'),
    'notifications': ('frontend/src/features/notifications; frontend/src/components/layout/NavigationLinks.jsx', 'API-107…112; BCR-001', 'media-notifications-client.spec.js; improvement-ui.spec.js', 'Server count/read truth, hai users cho realtime; Delete chờ duyệt.'),
    'presence': ('frontend/src/features/conversations/WorkspacePage.jsx; frontend/src/app/providers/RealtimeProvider.jsx', 'API-113; StartTyping/StopTyping/PresenceChanged', 'improvement-ui.spec.js; realtime.test.jsx', 'Hai user và privacy settings; nullable online phải được giữ nguyên.'),
    'realtime': ('frontend/src/app/providers/RealtimeProvider.jsx; frontend/src/components/chat/ChatWorkspace.jsx', '18 events trong signalr-contract.md; 4 hub methods', 'realtime.test.jsx; boundary.live.spec.js', 'Authenticated transport và hai browser; anonymous401 không đủ.'),
    'files': ('frontend/src/features/files; frontend/src/components/Avatar.jsx', 'API-029…032', 'improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js', 'Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật.'),
    'feedback': ('frontend/src/stores/ui-feedback-store.js; frontend/src/components/ToastViewport.jsx; frontend/src/components/forms/ValidatedForm.jsx; frontend/src/features/users/InterfacePreferences.jsx', 'Không thêm API; gọi consumer hiện có rồi mới success', 'ui-feedback.test.jsx; improvement-ui.spec.js', 'Local preference theo account/browser; không hứa sync đa thiết bị.'),
    'admin': ('frontend/src/features/admin; frontend/src/app/guards.jsx', 'API-083…101', 'moderation-admin-client.spec.js; boundary.live.spec.js', 'Admin/User được cấp sẵn; không cấp role, không chạy purge hoặc job xóa.'),
    'moderation': ('frontend/src/features/moderation; frontend/src/features/admin', 'API-102…106,086…096', 'moderation-admin-client.spec.js', 'Disposable suspended identity được phê duyệt; token appeal cô lập.'),
    'layout': ('frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n', 'Consumer hiện có, không thay đổi contract', 'design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js', 'Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng.'),
    'security': ('.gitignore; frontend/tests/e2e; frontend/src/lib/http.js; scripts/read-improvement-workbook.py', 'Không gọi API business hoặc xuất credentials', 'git status/ls-files; privacy scan; production isolation', 'Chỉ scan output category/path/count, không xuất nội dung nhạy cảm.'),
    'docs': ('README.md; roadmapFE.md; docs/frontend', '123 source mappings; live acceptance chưa PASS', 'README/source review; clean clone và demo thật chưa chạy', 'Môi trường hai account, .NET/SQL cấu hình sẵn; không migration/seed.'),
}

final = '--final' in sys.argv
if final:
    # Do not promote statuses based on a plan or an old checkpoint.
    browser = json.loads((ROOT / 'docs/frontend/improvement-final-regression.json').read_text(encoding='utf-8'))
    unit = json.loads((ROOT / 'frontend/test-results/improvement-unit-final.json').read_text(encoding='utf-8'))
    assert browser['stats']['expected'] == 176 and browser['stats']['unexpected'] == 0 and browser['stats']['flaky'] == 0
    assert unit['numPassedTests'] == 40 and unit['numFailedTests'] == 0
    updates = {
        'MC-001': ('VERIFIED', 'Đã căn giữa nhãn, giữ icon phải; đo form/button/text ở 10 tổ hợp viewport/theme PASS. Keyboard/focus, pending/disabled regression PASS. Chỉ nghiệm thu ticket UI căn chỉnh, không nghiệm thu login Backend.'),
        'MC-003': ('VERIFIED', 'Đã thống nhất primary/underline/focus tokens cho text links, Auth/account links; kiểm tra hover/focus và 5 widths × 2 themes, axe trong các feature suites PASS. Không thay Visual Direction hoặc semantics điều hướng.'),
        'MC-005': ('FIXED_NEEDS_VERIFICATION', 'Red cache-reset tái hiện mất URL token một lần. Đã lift state lên route, chỉ chia sẻ link còn trong danh sách có quyền/không hết hạn, kiểm tra response token, copy feedback và metadata expiry. Regression reload/malformed-token PASS; actual create/copy/join/revoke/expiry với Backend chưa chạy.'),
        'MC-009': ('FIXED_NEEDS_VERIFICATION', 'Toast success 2s/close/pause, inline/error giữ nguyên; local account preference. Unit và browser off/reload/failure/geometry PASS, 10 ảnh toast. Profile/actions thật và native screen reader vẫn chưa nghiệm thu.'),
        'MC-010': ('FIXED_NEEDS_VERIFICATION', 'Sửa wide padding phụ thuộc toàn viewport. 56 true Chromium chrome.tabs.setZoom combinations 80/90/100/110/125/150/200%, 1366/1440, private/group, Light/Dark PASS. Native installed Chrome/Edge vẫn NEEDS_VERIFICATION; không lấy viewport/root-font làm browser zoom.'),
        'MC-011': ('FIXED_NEEDS_VERIFICATION', 'Sửa thiếu decode/MIME fallback và zoom; authorized lazy thumbnails, modal, retry/download, request401-refresh simulation và object URL cleanup ba vòng. Media suite19 PASS. Không chứng minh storage/JWT permission hoặc tệp gốc của Product Owner đã hoạt động; cần upload/preview/download thật.'),
        'MC-012': ('FIXED_NEEDS_VERIFICATION', 'Ảnh gốc12 hỏng trong modal. Decode/MIME/network fallback đã sửa và 10 image viewport/theme cases PASS; chưa xác định được nguyên nhân auth/storage của file gốc, chưa có physical mobile acceptance.'),
        'QA-012': ('VERIFIED', 'Không tái hiện nhảy vị trí nội dung với500 tin tổng hợp/17 trang: bubble anchor shift0px; incoming/prepend/near-bottom tests PASS. Test probe ban đầu đo wrapper có date-divider nên báo43px; đã sửa probe theo nội dung đọc, không coi đó là product bug.'),
        'QA-014': ('FIXED_NEEDS_VERIFICATION', 'Đã thêm nhảy tới ngữ cảnh tin cũ bằng beforeSequence=sequence+1, giữ pagination, focus/highlight5s, trở về mới nhất và unavailable fallback. Browser regression PASS. Actual old/hidden/deleted messages còn cần Backend acceptance.'),
        'QA-017': ('BLOCKED_TEST_ENVIRONMENT', 'Bổ sung guard khi myRole null: không compose/pin/manage/invite; regression PASS. Group suite17 PASS gồm role Member/Owner và former membership. Backend quyền Owner/Admin/Member thật chưa nghiệm thu, không tự cấp role.'),
        'QA-021': ('FIXED_NEEDS_VERIFICATION', 'Unit progress/cancel/late-completion/retry/unmount PASS. Browser mở/đóng ảnh ba vòng rồi unmount: object URLs cân bằng0. Chưa chứng nhận heap/memory lâu dài với nhiều ảnh độ phân giải cao; thumbnail vẫn lấy Blob đầy đủ từ endpoint hiện có.'),
        'QA-025': ('VERIFIED', 'Local toast preference theo userId/browser: default on, reload và account isolation unit PASS; browser context private/reload/off retains inline PASS. Không lưu token, không quảng cáo sync hoặc persistence sau khi đóng private session; theme browser-wide giữ nguyên.'),
        'QA-029': ('VERIFIED', 'Value-free Git/source scan không phát hiện JWT-shaped value/private key/sensitive tracked paths/production console. Workbook riêng tư được ignore, bản trích ảnh tạm gỡ; screenshot mới đều synthetic fixtures. Scope scan và giới hạn được ghi trong improvement-privacy-audit.json, không chứng nhận mọi dữ liệu ngoài repository.'),
        'QA-032': ('VERIFIED', 'Full Chromium regression PASS với 5 widths × 2 themes, selected/list/details/modal/long text/composer, no horizontal overflow; preview/toast không che composer/header. Đây là acceptance hình học UI browser; native keyboard nằm ở QA-031.'),
        'QA-034': ('FIXED_NEEDS_VERIFICATION', 'Giảm idle typing rerender và memo các hàng tin/feature renderers. 500 incoming messages/17 cursor pages PASS; có input samples4xCPU + heapUsed metrics từ DevTools, không phải production Web Vitals/Lighthouse/load. Authenticated production profiling vẫn cần môi trường test.'),
        'QA-040': ('VERIFIED', 'Toast chỉ success; lỗi form/modal vẫn tồn tại sau2s và preference off không xóa feedback. Browser500 giả lập giữ alert sau2200ms, không lộ raw server text; unit close/pause/identity PASS. Không suy ra API500 thật đã nghiệm thu.'),
    }
    for ticket, (status, finding) in updates.items():
        _, feature, kind, _ = decisions[ticket]
        decisions[ticket] = (status, feature, kind, finding)

additional = [
    {'id': 'FND-001', 'priority': 'P1', 'related': 'QA-007/021', 'status': 'VERIFIED' if final else 'FIXED_NEEDS_VERIFICATION', 'finding': 'Avatar không có decode-error fallback. Đã chuyển về initials, revoke URL khi unmount; blob-url.test.jsx2 PASS. Không chứng minh actual avatar storage.'},
    {'id': 'FND-002', 'priority': 'P1', 'related': 'MC-007/QA-030', 'status': 'VERIFIED' if final else 'FIXED_NEEDS_VERIFICATION', 'finding': 'Popover mới dùng animation chưa có keyframes làm Radix giữ closed content. Đã sửa surface-enter + closed animation none; loading/empty/error Escape/focus và matrix19 PASS.'},
    {'id': 'FND-003', 'priority': 'P2', 'related': 'QA-034', 'status': 'VERIFIED' if final else 'FIXED_NEEDS_VERIFICATION', 'finding': 'Typing TTL tạo array/state/context mới mỗi giây ngay khi idle. Giữ reference khi không đổi, memo context và message rows/renderers. Idle consumer unit và 500-message profile PASS; production performance riêng chưa nghiệm thu.'},
    {'id': 'FND-004', 'priority': 'P1', 'related': 'QA-017', 'status': 'FIXED_NEEDS_VERIFICATION', 'finding': 'Frontend chỉ kiểm status Active, thiếu myRole cho historical group viewer. Đã khóa compose/invite/manage và pin khi không còn role hoạt động. Browser17 group checks PASS; actual Backend role acceptance còn bị chặn môi trường.'},
]

rows = []
for row in sheets[1]['rows']:
    cols = {re.sub(r'\d+$', '', cell): value for cell, value in row.items()}
    ticket = cols.get('A', '')
    if ticket not in decisions:
        continue
    status, feature, kind, finding = decisions[ticket]
    files, apis, tests, dependencies = features[feature]
    rows.append({'id': ticket, 'sourceSheet': sheets[1]['name'], 'sourceRow': int(re.search(r'\d+', next(iter(row))).group()),
                 'module': cols['C'], 'priority': cols['D'], 'status': status, 'classification': kind,
                 'original': {k: cols.get(col, '') for col, k in [('B', 'source'), ('E', 'status'), ('F', 'title'), ('G', 'risk'), ('H', 'expected'), ('I', 'acceptance'), ('J', 'scope'), ('K', 'evidence'), ('L', 'notes')]},
                 'rootCauseOrFinding': finding, 'affectedFiles': files, 'backendApis': apis,
                 'proposedSolution': 'Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. ' + ('Xem backend-change-requests.md trước mọi sửa Backend.' if status == 'BLOCKED_BACKEND_APPROVAL' else 'Thiếu live evidence thì giữ chưa nghiệm thu.'),
                 'dependencies': dependencies, 'risk': cols['D'] + ': ' + cols.get('G', ''),
                 'testingMethod': tests + '. ' + cols['I'], 'evidence': finding,
                 'liveBusinessAcceptance': 'NEEDS_VERIFICATION' if feature not in ('layout', 'feedback', 'security', 'docs') else 'Không suy từ kiểm tra UI sang API business.'})
assert len(rows) == 52
counts = Counter(r['status'] for r in rows)
meta = {'reviewDate': '2026-11-07', 'workbookSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'originalWorkbookChanged': False, 'sheetsRead': [s['name'] for s in sheets], 'embeddedImagesInspected': 12,
        'statusCounts': dict(counts), 'additionalFindings': additional, 'totalOriginalTickets': 52, 'totalIncludingAdditionalFindings': 56, 'tickets': rows}
(ROOT / 'docs/frontend/improvement-tickets.json').write_text(json.dumps(meta, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
lines = ['# Tiến độ cải thiện và kiểm thử — 07/11/2026', '',
         '**52 ticket gốc: 12 MC + 40 QA.** Không đổi phản ánh/acceptance gốc, không tự coi QA là lỗi đã xảy ra. Visual Baseline vẫn APPROVED. Không nghiệm thu Backend business hoặc tuyên bố hoàn thành 100%.', '',
         '## Thống kê', '', '| Trạng thái | Số ticket |', '| --- | ---: |']
lines += [f'| {status} | {count} |' for status, count in sorted(counts.items())]
lines += ['', f"Confirmed Bugs: {sum(r['classification']=='CONFIRMED_BUG' for r in rows)}; Improvement Requests: {sum(r['classification']=='IMPROVEMENT_REQUEST' for r in rows)}. Các nhóm này khác với trạng thái nghiệm thu.", '',
          '## Workbook và ảnh gốc', '',
          'Đã đọc cả 6 sheet bằng OOXML ZIP/XML: TỔNG QUAN, DANH SÁCH CẢI TIẾN, ẢNH MINH CHỨNG, CHECKLIST DEMO, NỘI DUNG GỐC, HƯỚNG DẪN. Đã xem trực tiếp 12 PNG nhúng và đối chiếu A6:E17 của sheet ẢNH MINH CHỨNG: dòng 6 → MC-001, …, dòng 17 → MC-012. Không suy nội dung ảnh từ caption.', '',
          'Ảnh 01: nhãn nút lệch trái. 02: rail chưa badge. 03: link mặc định. 04: Details chỉ có đường dẫn quản lý. 05: danh sách có metadata link nhưng không có URL vừa tạo. 06: thông báo chỉ có read. 07: UI tray tham khảo. 08: subtitle online thiếu dot. 09: inline success. 10: browser zoom 75% và khoảng trống chat rộng. 11: file cards, chưa chứng minh request bị từ chối. 12: ảnh lỗi ngay trong modal mobile. Không chép tên, email, nội dung chat hoặc token từ ảnh vào báo cáo.', '',
          'Giữ nguyên workbook gốc (6 sheets, 12 images, 7 formulas). Công cụ artifact-tool không có trong runtime được chỉ định; không có engine round-trip đã xác minh bảo toàn ảnh/formulas. Vì vậy không ghi đè hoặc xuất bản workbook thay thế. Dùng báo cáo này và improvement-tickets.json làm ledger đầy đủ. Bản trích ảnh tạm đã được gỡ; workbook riêng tư được gitignore, chỉ giữ trên máy.', '',
          f"SHA-256 workbook: `{meta['workbookSha256']}`.", '',
          '## Baseline và bằng chứng', '',
          '- Git baseline: babbcfd đã commit 167 Frontend files; giữ các xóa có trước REFACTOR_STATUS.md, STATIC_AUDIT.json. Không stage/commit/push.',
          '- Baseline: lint/build PASS; 33 unit/component PASS; 132 Chromium regression PASS (improvement-baseline-2026-11-07.json). Groups gốc 13/13 PASS.',
          '- Backend 5080 qua proxy: 4 anonymous/invalid-DTO boundary PASS; auth thật 1 SKIPPED vì thiếu credentials. Không write business data, không mở private uploads/dev-mail.',
          '- Red mới: 3 ca thiếu image zoom, 2 ca thiếu decode/MIME fallback, 1 ca mất one-time invite URL. HTTP403/404 fallback vốn đã PASS. Không gọi các failure giả lập là server bug.',
          '- Sau media fix: 17/17 PASS (3 MIME + corrupt/MIME/403/404 + 5 widths × 2 themes). MC-005 cache reload targeted PASS. Unit sau thêm toast: 36/36 PASS. Các vòng full/UI sau cùng phải được tổng hợp trước khi nghiệm thu.', '',
          '## Quyền truy cập kiểm thử và giới hạn', '',
          'Không có credentials được cấp phép; người dùng đã chọn tiếp tục và giữ đăng nhập thật chưa xác minh. Dùng biến môi trường MECHAT_E2E_IDENTIFIER/MECHAT_E2E_PASSWORD hoặc ignored frontend/.env.e2e.local trên máy, do operator nhập ngoài chat. Không in giá trị, không hardcode, không chụp/trace phiên thật. Xem live-integration-acceptance.md trước test có ghi dữ liệu.', '',
          'BCR-001 xóa notifications và BCR-002 unread Friends đang chờ Product Owner. Không đổi Mark Read thành Delete, không dùng item count làm unread. Native zoom/keyboard/NVDA/device và actual storage/SignalR cần evidence riêng.', '',
          '## Ticket ledger', '']
for r in rows:
    o = r['original']
    lines += [f"### {r['id']} — {o['title']}", '',
              f"- Module / Priority / Status: {r['module']} / **{r['priority']}** / **{r['status']}**.",
              f"- Loại: {r['classification']}. Source: DANH SÁCH CẢI TIẾN dòng {r['sourceRow']}.",
              f"- Phản ánh/rủi ro gốc: {o['risk']}", f"- Acceptance gốc: {o['acceptance']}",
              f"- Root cause / kết quả đối chiếu: {r['rootCauseOrFinding']}",
              f"- Files: {r['affectedFiles']}.", f"- Backend APIs: {r['backendApis']}.",
              f"- Giải pháp: {r['proposedSolution']}", f"- Dependencies / risk: {r['dependencies']} / {r['risk']}",
              f"- Test / evidence: {r['testingMethod']}. {r['evidence']}", '']
lines += ['## Phát hiện mới ngoài workbook', '',
          'Giữ 52 ID gốc; thêm4 phát hiện, tổng phạm vi theo dõi56. Không dùng lại ID của workbook.', '',
          *[f"- **{item['id']} / {item['priority']} / {item['status']}** ({item['related']}): {item['finding']}" for item in additional], '',
          '## Next Exact Action', '',
          ('Client regression và source đã lưu; chờ BCR approval/test identities. Thứ tự live acceptance và các điều kiện chưa xác minh ở checkpoint cuối bên dưới.' if final else 'Hoàn tất regression UI mới (Escape/focus/count labels/axe), kiểm tra true browser zoom nếu runtime hỗ trợ; bổ sung cancel/retry/Blob và scroll/performance; giải quyết QA-014 search context. Chạy lại full lint/unit/browser/build/localization/API/production và cập nhật kết quả tại cuối tài liệu. Live write acceptance tiếp tục BLOCKED_TEST_ENVIRONMENT; Backend requests chờ duyệt riêng.'), '']
if final:
    totals = Counter(counts)
    totals.update(item['status'] for item in additional)
    lines += ['## Checkpoint cuối và kết quả nghiệm thu', '',
              f"52 ticket gốc: {dict(counts)}. Cộng4 FND: {dict(totals)}. Remaining (chưa VERIFIED/NOT_REPRODUCIBLE): {sum(v for k,v in totals.items() if k not in ('VERIFIED','NOT_REPRODUCIBLE'))}.", '',
              '- **40 unit/component PASS** trong11 files; **176 Chromium browser PASS,0 FAIL,0 flaky**; [full cases](improvement-final-regression.json). Các focused reruns có trùng ca, không cộng vào176.',
              '- **56 actual Chromium browser zoom combinations PASS** qua chrome.tabs.setZoom, độc lập5viewport×2theme. Native Chrome/Edge/thiết bị vẫn NEEDS_VERIFICATION.',
              '- Build/lint PASS; localization 687 calls PASS; source API 122 function mappings PASS; production isolation PASS, production smoke 1 PASS; npm audit 0 vulnerabilities; real anonymous boundaries 4 PASS; successful auth 1 SKIPPED.',
              '- Screenshots57 PNG mới từ browser thật với synthetic data, gồm login/media/notification tray/toast/zoom/group CTA; [chỉ mục](improvement-screenshots.md). Đã xem lại ảnh đại diện Light/Dark/mobile/zoom; không có ảnh tài khoản thật.',
              '- Final machine evidence: improvement-verification.json; privacy scope: improvement-privacy-audit.json; performance sampling: improvement-client-performance.json. Performance số đo gồm overhead automation/dev, không chứng nhận Web Vitals hoặc tải SQL.', '',
              'Các mục VERIFIED là ticket có acceptance độc lập ở Frontend/docs và evidence tương ứng. Không có successful Backend business endpoint mới được nâng PASS. MC-011/012 vẫn FIXED_NEEDS_VERIFICATION cho original storage/device issue. Không cấp role, sửa Backend/DB, migration/seed, xóa dữ liệu, stage/commit/push.', '',
              'Lịch sử failure: thiếu zoom3 FAIL; decode/MIME2 FAIL; one-time URL reset1 FAIL; popover closed animation và accessible-count label đã bị phát hiện rồi sửa. Một unit đầu cần tách server navigation khỏi preview để không yêu cầu QueryProvider ở pure component. Performance probe đầu đo wrapper/date-divider43px, sau sửa probe bubble0px; lượt khác timeout120s khi load390 messages trước tối ưu idle/render. Giữ lịch sử, không dùng skip/retry che lỗi.', '',
              'Next Exact Action hiện hành: Product Owner xem BCR-001/BCR-002. Khi có test identities được cấp phép qua kênh cấu hình an toàn, chạy Auth/session lifecycle trước, rồi two-user friends/messages/groups/unread/presence/reconnect/files; Admin/appeal/retention theo quyền được cấp. Chạy native Chrome/Edge/NVDA/Android/iOS và authenticated production performance theo live-integration-acceptance.md. Không còn task source dang dở; phần chưa xác minh không được coi DONE.', '']
    verification = json.loads((ROOT / 'docs/frontend/improvement-verification.json').read_text(encoding='utf-8'))
    lines += ['## Files đã sửa hoặc tạo', '', 'Danh sách lấy từ Git diff + untracked không bị ignore. Hai file người dùng đã xóa có trước được giữ nguyên, không tính vào thay đổi của đợt này. Không có Backend/Database source.', '']
    lines += [f'- [{path}](../../{path})' for path in verification['changedFiles']]
    lines += ['', 'Focused cuối: matrix hover/focus/capture10 PASS và ảnh CTA nhóm390px1 PASS; có ca trùng full176, không cộng thành full mới. Workbook gốc không được ghi lại bởi script này.', '']
(ROOT / 'docs/frontend/improvement-progress.md').write_text('\n'.join(lines), encoding='utf-8')
print(json.dumps({'tickets': len(rows), 'statusCounts': dict(counts), 'workbookUnchanged': True}, ensure_ascii=False))
