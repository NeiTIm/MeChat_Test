# Tiến độ cải thiện và kiểm thử — 07/11/2026

**52 ticket gốc: 12 MC + 40 QA.** Không đổi phản ánh/acceptance gốc, không tự coi QA là lỗi đã xảy ra. Visual Baseline vẫn APPROVED. Không nghiệm thu Backend business hoặc tuyên bố hoàn thành 100%.

## Thống kê

| Trạng thái | Số ticket |
| --- | ---: |
| BLOCKED_BACKEND_APPROVAL | 2 |
| BLOCKED_TEST_ENVIRONMENT | 29 |
| FIXED_NEEDS_VERIFICATION | 12 |
| NOT_REPRODUCIBLE | 1 |
| VERIFIED | 8 |

Confirmed Bugs: 4; Improvement Requests: 10. Các nhóm này khác với trạng thái nghiệm thu.

## Workbook và ảnh gốc

Đã đọc cả 6 sheet bằng OOXML ZIP/XML: TỔNG QUAN, DANH SÁCH CẢI TIẾN, ẢNH MINH CHỨNG, CHECKLIST DEMO, NỘI DUNG GỐC, HƯỚNG DẪN. Đã xem trực tiếp 12 PNG nhúng và đối chiếu A6:E17 của sheet ẢNH MINH CHỨNG: dòng 6 → MC-001, …, dòng 17 → MC-012. Không suy nội dung ảnh từ caption.

Ảnh 01: nhãn nút lệch trái. 02: rail chưa badge. 03: link mặc định. 04: Details chỉ có đường dẫn quản lý. 05: danh sách có metadata link nhưng không có URL vừa tạo. 06: thông báo chỉ có read. 07: UI tray tham khảo. 08: subtitle online thiếu dot. 09: inline success. 10: browser zoom 75% và khoảng trống chat rộng. 11: file cards, chưa chứng minh request bị từ chối. 12: ảnh lỗi ngay trong modal mobile. Không chép tên, email, nội dung chat hoặc token từ ảnh vào báo cáo.

Giữ nguyên workbook gốc (6 sheets, 12 images, 7 formulas). Công cụ artifact-tool không có trong runtime được chỉ định; không có engine round-trip đã xác minh bảo toàn ảnh/formulas. Vì vậy không ghi đè hoặc xuất bản workbook thay thế. Dùng báo cáo này và improvement-tickets.json làm ledger đầy đủ. Bản trích ảnh tạm đã được gỡ; workbook riêng tư được gitignore, chỉ giữ trên máy.

SHA-256 workbook: `400769ed9bb285bb0de9d106c99f57bb4ee3ef3c5f0c02ac74bb83a1ce985c5f`.

## Baseline và bằng chứng

- Git baseline: babbcfd đã commit 167 Frontend files; giữ các xóa có trước REFACTOR_STATUS.md, STATIC_AUDIT.json. Không stage/commit/push.
- Baseline: lint/build PASS; 33 unit/component PASS; 132 Chromium regression PASS (improvement-baseline-2026-11-07.json). Groups gốc 13/13 PASS.
- Backend 5080 qua proxy: 4 anonymous/invalid-DTO boundary PASS; auth thật 1 SKIPPED vì thiếu credentials. Không write business data, không mở private uploads/dev-mail.
- Red mới: 3 ca thiếu image zoom, 2 ca thiếu decode/MIME fallback, 1 ca mất one-time invite URL. HTTP403/404 fallback vốn đã PASS. Không gọi các failure giả lập là server bug.
- Sau media fix: 17/17 PASS (3 MIME + corrupt/MIME/403/404 + 5 widths × 2 themes). MC-005 cache reload targeted PASS. Unit sau thêm toast: 36/36 PASS. Các vòng full/UI sau cùng phải được tổng hợp trước khi nghiệm thu.

## Quyền truy cập kiểm thử và giới hạn

Không có credentials được cấp phép; người dùng đã chọn tiếp tục và giữ đăng nhập thật chưa xác minh. Dùng biến môi trường MECHAT_E2E_IDENTIFIER/MECHAT_E2E_PASSWORD hoặc ignored frontend/.env.e2e.local trên máy, do operator nhập ngoài chat. Không in giá trị, không hardcode, không chụp/trace phiên thật. Xem live-integration-acceptance.md trước test có ghi dữ liệu.

BCR-001 xóa notifications và BCR-002 unread Friends đang chờ Product Owner. Không đổi Mark Read thành Delete, không dùng item count làm unread. Native zoom/keyboard/NVDA/device và actual storage/SignalR cần evidence riêng.

## Ticket ledger

### MC-001 — Căn giữa nút Đăng nhập

- Module / Priority / Status: Auth / Giao diện / **P2** / **VERIFIED**.
- Loại: CONFIRMED_BUG. Source: DANH SÁCH CẢI TIẾN dòng 6.
- Phản ánh/rủi ro gốc: Nút Đăng nhập chưa được căn giữa theo form hoặc layout.
- Acceptance gốc: Kiểm tra tại 320/390/768/1366/1440px, Light/Dark; đo alignment và kiểm tra focus.
- Root cause / kết quả đối chiếu: Đã căn giữa nhãn, giữ icon phải; đo form/button/text ở 10 tổ hợp viewport/theme PASS. Keyboard/focus, pending/disabled regression PASS. Chỉ nghiệm thu ticket UI căn chỉnh, không nghiệm thu login Backend.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P2: Nút Đăng nhập chưa được căn giữa theo form hoặc layout.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Kiểm tra tại 320/390/768/1366/1440px, Light/Dark; đo alignment và kiểm tra focus.. Đã căn giữa nhãn, giữ icon phải; đo form/button/text ở 10 tổ hợp viewport/theme PASS. Keyboard/focus, pending/disabled regression PASS. Chỉ nghiệm thu ticket UI căn chỉnh, không nghiệm thu login Backend.

### MC-002 — Badge chưa đọc trên thanh điều hướng

- Module / Priority / Status: Điều hướng / Unread / **P1** / **BLOCKED_BACKEND_APPROVAL**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 7.
- Phản ánh/rủi ro gốc: Chưa có số chưa đọc ở Tin nhắn, Nhóm, Bạn bè, Thông báo; người dùng muốn badge mất khi mở mục.
- Acceptance gốc: 2 tài khoản tạo unread; đối chiếu API counts; mở đúng mục nhưng không tự đánh dấu đã đọc khi Backend chưa xác nhận; reload không sai.
- Root cause / kết quả đối chiếu: Navigation chưa có consumer count. Đã thêm Private/Group từ toàn bộ server unread projection và Notifications từ count API. Không tự giảm khi click. Bạn bè không có unread/read contract: BCR-002.
- Files: frontend/src/components/layout/NavigationLinks.jsx; frontend/src/features/messages/api.js.
- Backend APIs: GET /api/unread; API-108; BCR-002.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Xem backend-change-requests.md trước mọi sửa Backend.
- Dependencies / risk: Unread projection thật; Friends contract chờ Product Owner. / P1: Chưa có số chưa đọc ở Tin nhắn, Nhóm, Bạn bè, Thông báo; người dùng muốn badge mất khi mở mục.
- Test / evidence: improvement-ui.spec.js; realtime.test.jsx. 2 tài khoản tạo unread; đối chiếu API counts; mở đúng mục nhưng không tự đánh dấu đã đọc khi Backend chưa xác nhận; reload không sai.. Navigation chưa có consumer count. Đã thêm Private/Group từ toàn bộ server unread projection và Notifications từ count API. Không tự giảm khi click. Bạn bè không có unread/read contract: BCR-002.

### MC-003 — Text link chưa thống nhất, khó nhận biết

- Module / Priority / Status: Design System / **P2** / **VERIFIED**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 8.
- Phản ánh/rủi ro gốc: Các link trong Group và nhiều trang đang mang phong cách mặc định/chưa nổi bật.
- Acceptance gốc: Kiểm tra toàn bộ Auth/Friends/Chat/Groups/Admin ở hai theme; keyboard focus và tương phản AA.
- Root cause / kết quả đối chiếu: Đã thống nhất primary/underline/focus tokens cho text links, Auth/account links; kiểm tra hover/focus và 5 widths × 2 themes, axe trong các feature suites PASS. Không thay Visual Direction hoặc semantics điều hướng.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P2: Các link trong Group và nhiều trang đang mang phong cách mặc định/chưa nổi bật.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Kiểm tra toàn bộ Auth/Friends/Chat/Groups/Admin ở hai theme; keyboard focus và tương phản AA.. Đã thống nhất primary/underline/focus tokens cho text links, Auth/account links; kiểm tra hover/focus và 5 widths × 2 themes, axe trong các feature suites PASS. Không thay Visual Direction hoặc semantics điều hướng.

### MC-004 — Sau tạo nhóm khó tìm nơi lấy link mời

- Module / Priority / Status: Groups / Invite / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 9.
- Phản ánh/rủi ro gốc: Tạo nhóm thành công nhưng không thấy rõ chức năng lấy liên kết mời.
- Acceptance gốc: Tạo nhóm mới; mở panel; thao tác tạo link qua API; hiển thị kết quả, copy và lỗi/quyền phù hợp.
- Root cause / kết quả đối chiếu: Ảnh 04: link mời chỉ nằm sâu trong quản lý. Thêm CTA sau tạo nhóm và trong Details, tới mục invite-links, không tạo link ngầm. Tạo nhóm thật chưa chạy.
- Files: frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx.
- Backend APIs: API-043…068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai/ba test users và group disposable; không đổi business permissions. / P1: Tạo nhóm thành công nhưng không thấy rõ chức năng lấy liên kết mời.
- Test / evidence: groups-client.spec.js; improvement-ui.spec.js. Tạo nhóm mới; mở panel; thao tác tạo link qua API; hiển thị kết quả, copy và lỗi/quyền phù hợp.. Ảnh 04: link mời chỉ nằm sâu trong quản lý. Thêm CTA sau tạo nhóm và trong Details, tới mục invite-links, không tạo link ngầm. Tạo nhóm thật chưa chạy.

### MC-005 — Link lời mời vừa tạo chưa được hiển thị rõ

- Module / Priority / Status: Groups / Invite / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: CONFIRMED_BUG. Source: DANH SÁCH CẢI TIẾN dòng 10.
- Phản ánh/rủi ro gốc: Sau bấm tạo liên kết, khó thấy URL/token và thao tác chia sẻ.
- Acceptance gốc: Tạo → xem → copy → mở link → thử link hết hạn/thu hồi; không lộ token trong logs.
- Root cause / kết quả đối chiếu: Red cache-reset tái hiện mất URL token một lần. Đã lift state lên route, chỉ chia sẻ link còn trong danh sách có quyền/không hết hạn, kiểm tra response token, copy feedback và metadata expiry. Regression reload/malformed-token PASS; actual create/copy/join/revoke/expiry với Backend chưa chạy.
- Files: frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx.
- Backend APIs: API-043…068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai/ba test users và group disposable; không đổi business permissions. / P1: Sau bấm tạo liên kết, khó thấy URL/token và thao tác chia sẻ.
- Test / evidence: groups-client.spec.js; improvement-ui.spec.js. Tạo → xem → copy → mở link → thử link hết hạn/thu hồi; không lộ token trong logs.. Red cache-reset tái hiện mất URL token một lần. Đã lift state lên route, chỉ chia sẻ link còn trong danh sách có quyền/không hết hạn, kiểm tra response token, copy feedback và metadata expiry. Regression reload/malformed-token PASS; actual create/copy/join/revoke/expiry với Backend chưa chạy.

### MC-006 — Mong muốn có nút xóa thông báo

- Module / Priority / Status: Notifications / **P1** / **BLOCKED_BACKEND_APPROVAL**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 11.
- Phản ánh/rủi ro gốc: Danh sách thông báo hiện chưa có hành động xóa; người dùng muốn xóa từng cái/tất cả.
- Acceptance gốc: Kiểm kê Notifications Controller; phân biệt Mark read với Delete; nếu thiếu API tạo GAP, không xóa chỉ ở FE để giả thành công.
- Root cause / kết quả đối chiếu: NotificationsController chỉ có list/count/read/read-all/settings. Không có delete. BCR-001 đã có phương án bảo mật/retention; không giả lập xóa.
- Files: frontend/src/features/notifications; frontend/src/components/layout/NavigationLinks.jsx.
- Backend APIs: API-107…112; BCR-001.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Xem backend-change-requests.md trước mọi sửa Backend.
- Dependencies / risk: Server count/read truth, hai users cho realtime; Delete chờ duyệt. / P1: Danh sách thông báo hiện chưa có hành động xóa; người dùng muốn xóa từng cái/tất cả.
- Test / evidence: media-notifications-client.spec.js; improvement-ui.spec.js. Kiểm kê Notifications Controller; phân biệt Mark read với Delete; nếu thiếu API tạo GAP, không xóa chỉ ở FE để giả thành công.. NotificationsController chỉ có list/count/read/read-all/settings. Không có delete. BCR-001 đã có phương án bảo mật/retention; không giả lập xóa.

### MC-007 — Xem nhanh thông báo khi đang nhắn tin

- Module / Priority / Status: Thông báo trong Chat / **P2** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 12.
- Phản ánh/rủi ro gốc: Muốn một vùng nổi/xem nhanh, tóm lược tin nhắn, lời mời và thông báo ngay trong màn hình Chat.
- Acceptance gốc: Trong lúc chat nhận notifications; xem preview không che composer; keyboard/escape, độ dài text, privacy khi block.
- Root cause / kết quả đối chiếu: Ảnh 07 là tham khảo UI. Đã thêm preview dùng chung feed/count/actions, 5 thông báo gần nhất, loading/empty/error/Escape/deep link. DTO chỉ có type/text/reference/time, không bịa tên người gửi hoặc nội dung message.
- Files: frontend/src/features/notifications; frontend/src/components/layout/NavigationLinks.jsx.
- Backend APIs: API-107…112; BCR-001.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Server count/read truth, hai users cho realtime; Delete chờ duyệt. / P2: Muốn một vùng nổi/xem nhanh, tóm lược tin nhắn, lời mời và thông báo ngay trong màn hình Chat.
- Test / evidence: media-notifications-client.spec.js; improvement-ui.spec.js. Trong lúc chat nhận notifications; xem preview không che composer; keyboard/escape, độ dài text, privacy khi block.. Ảnh 07 là tham khảo UI. Đã thêm preview dùng chung feed/count/actions, 5 thông báo gần nhất, loading/empty/error/Escape/deep link. DTO chỉ có type/text/reference/time, không bịa tên người gửi hoặc nội dung message.

### MC-008 — Online màu xanh, offline màu xám

- Module / Priority / Status: Presence / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 13.
- Phản ánh/rủi ro gốc: Trạng thái chưa được thể hiện bằng chấm màu rõ ràng.
- Acceptance gốc: 2 user online/offline; tắt ShowOnline; nhiều tab; kiểm tra timeout/reconnect và aria-label.
- Root cause / kết quả đối chiếu: Có subtitle API nhưng thiếu chấm trạng thái. Thêm xanh/online, xám/offline, nhãn chữ; null không thành offline. Client checks true/false/null; cần hai user/privacy thật.
- Files: frontend/src/features/conversations/WorkspacePage.jsx; frontend/src/app/providers/RealtimeProvider.jsx.
- Backend APIs: API-113; StartTyping/StopTyping/PresenceChanged.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai user và privacy settings; nullable online phải được giữ nguyên. / P1: Trạng thái chưa được thể hiện bằng chấm màu rõ ràng.
- Test / evidence: improvement-ui.spec.js; realtime.test.jsx. 2 user online/offline; tắt ShowOnline; nhiều tab; kiểm tra timeout/reconnect và aria-label.. Có subtitle API nhưng thiếu chấm trạng thái. Thêm xanh/online, xám/offline, nhãn chữ; null không thành offline. Client checks true/false/null; cần hai user/privacy thật.

### MC-009 — Toast đẹp, tự ẩn sau 2 giây và có thể bật/tắt

- Module / Priority / Status: Toast / Settings / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 14.
- Phản ánh/rủi ro gốc: Các thao tác chỉnh sửa chỉ hiện thông báo thường; mong toast 2s, nút đóng và tùy chọn trong Settings.
- Acceptance gốc: Đổi profile, chat actions, thất bại mạng; test 2 theme/mobile/focus; lưu sở thích UI an toàn; khi tắt có feedback thay thế.
- Root cause / kết quả đối chiếu: Toast success 2s/close/pause, inline/error giữ nguyên; local account preference. Unit và browser off/reload/failure/geometry PASS, 10 ảnh toast. Profile/actions thật và native screen reader vẫn chưa nghiệm thu.
- Files: frontend/src/stores/ui-feedback-store.js; frontend/src/components/ToastViewport.jsx; frontend/src/components/forms/ValidatedForm.jsx; frontend/src/features/users/InterfacePreferences.jsx.
- Backend APIs: Không thêm API; gọi consumer hiện có rồi mới success.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Local preference theo account/browser; không hứa sync đa thiết bị. / P1: Các thao tác chỉnh sửa chỉ hiện thông báo thường; mong toast 2s, nút đóng và tùy chọn trong Settings.
- Test / evidence: ui-feedback.test.jsx; improvement-ui.spec.js. Đổi profile, chat actions, thất bại mạng; test 2 theme/mobile/focus; lưu sở thích UI an toàn; khi tắt có feedback thay thế.. Toast success 2s/close/pause, inline/error giữ nguyên; local account preference. Unit và browser off/reload/failure/geometry PASS, 10 ảnh toast. Profile/actions thật và native screen reader vẫn chưa nghiệm thu.

### MC-010 — Nội dung chat co giãn không hợp lý khi thu nhỏ

- Module / Priority / Status: Responsive / Zoom / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 15.
- Phản ánh/rủi ro gốc: Khi zoom nhỏ hơn khoảng 110% giao diện tin nhắn bị lệch/co giãn; ở >=110% trông ổn theo phản ánh.
- Acceptance gốc: Kiểm tra Chrome/Edge zoom 80/90/100/110/125/150/200%, desktop 1366/1440; long text, ảnh, sidebar open/close.
- Root cause / kết quả đối chiếu: Sửa wide padding phụ thuộc toàn viewport. 56 true Chromium chrome.tabs.setZoom combinations 80/90/100/110/125/150/200%, 1366/1440, private/group, Light/Dark PASS. Native installed Chrome/Edge vẫn NEEDS_VERIFICATION; không lấy viewport/root-font làm browser zoom.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P1: Khi zoom nhỏ hơn khoảng 110% giao diện tin nhắn bị lệch/co giãn; ở >=110% trông ổn theo phản ánh.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Kiểm tra Chrome/Edge zoom 80/90/100/110/125/150/200%, desktop 1366/1440; long text, ảnh, sidebar open/close.. Sửa wide padding phụ thuộc toàn viewport. 56 true Chromium chrome.tabs.setZoom combinations 80/90/100/110/125/150/200%, 1366/1440, private/group, Light/Dark PASS. Native installed Chrome/Edge vẫn NEEDS_VERIFICATION; không lấy viewport/root-font làm browser zoom.

### MC-011 — Không xem trước được hình ảnh đính kèm

- Module / Priority / Status: Media / Preview / **P0** / **FIXED_NEEDS_VERIFICATION**.
- Loại: CONFIRMED_BUG. Source: DANH SÁCH CẢI TIẾN dòng 16.
- Phản ánh/rủi ro gốc: Bấm ảnh/file trong chat nhưng preview không mở hoặc không tải được.
- Acceptance gốc: Upload JPG/PNG/WebP; mở ảnh trước/sau refresh và sau hết hạn access token; quan sát Network status, Blob URL cleanup.
- Root cause / kết quả đối chiếu: Sửa thiếu decode/MIME fallback và zoom; authorized lazy thumbnails, modal, retry/download, request401-refresh simulation và object URL cleanup ba vòng. Media suite19 PASS. Không chứng minh storage/JWT permission hoặc tệp gốc của Product Owner đã hoạt động; cần upload/preview/download thật.
- Files: frontend/src/features/files; frontend/src/components/Avatar.jsx.
- Backend APIs: API-029…032.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật. / P0: Bấm ảnh/file trong chat nhưng preview không mở hoặc không tải được.
- Test / evidence: improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js. Upload JPG/PNG/WebP; mở ảnh trước/sau refresh và sau hết hạn access token; quan sát Network status, Blob URL cleanup.. Sửa thiếu decode/MIME fallback và zoom; authorized lazy thumbnails, modal, retry/download, request401-refresh simulation và object URL cleanup ba vòng. Media suite19 PASS. Không chứng minh storage/JWT permission hoặc tệp gốc của Product Owner đã hoạt động; cần upload/preview/download thật.

### MC-012 — Ảnh không hiển thị ở chế độ mô phỏng mobile

- Module / Priority / Status: Media / Mobile / **P0** / **FIXED_NEEDS_VERIFICATION**.
- Loại: CONFIRMED_BUG. Source: DANH SÁCH CẢI TIẾN dòng 17.
- Phản ánh/rủi ro gốc: DevTools mobile hiển thị attachment nhưng không tải ảnh; chưa kiểm thử điện thoại thật.
- Acceptance gốc: Test DevTools 390/320px, mobile Chrome thật; network/auth/blob/object-fit/overflow; so với desktop cùng file.
- Root cause / kết quả đối chiếu: Ảnh gốc12 hỏng trong modal. Decode/MIME/network fallback đã sửa và 10 image viewport/theme cases PASS; chưa xác định được nguyên nhân auth/storage của file gốc, chưa có physical mobile acceptance.
- Files: frontend/src/features/files; frontend/src/components/Avatar.jsx.
- Backend APIs: API-029…032.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật. / P0: DevTools mobile hiển thị attachment nhưng không tải ảnh; chưa kiểm thử điện thoại thật.
- Test / evidence: improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js. Test DevTools 390/320px, mobile Chrome thật; network/auth/blob/object-fit/overflow; so với desktop cùng file.. Ảnh gốc12 hỏng trong modal. Decode/MIME/network fallback đã sửa và 10 image viewport/theme cases PASS; chưa xác định được nguyên nhân auth/storage của file gốc, chưa có physical mobile acceptance.

### QA-001 — Đăng nhập thật chưa nghiệm thu

- Module / Priority / Status: Auth / API / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 18.
- Phản ánh/rủi ro gốc: Client tests dùng HTTP simulation, chưa chứng minh login với SQL thật.
- Acceptance gốc: Login User/Admin từ FE; xem cookies/CSRF/Network, GET /users/me; logout thành công.
- Root cause / kết quả đối chiếu: Không có credentials kiểm thử được cấp phép. Real auth test SKIPPED; 4 anonymous boundaries PASS không chứng minh login/SQL.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P0: Client tests dùng HTTP simulation, chưa chứng minh login với SQL thật.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Login User/Admin từ FE; xem cookies/CSRF/Network, GET /users/me; logout thành công.. Không có credentials kiểm thử được cấp phép. Real auth test SKIPPED; 4 anonymous boundaries PASS không chứng minh login/SQL.

### QA-002 — Refresh token và CSRF rotation

- Module / Priority / Status: Auth / Session / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 19.
- Phản ánh/rủi ro gốc: Mất phiên/401 sau reload hoặc nhiều request đồng thời có thể làm logout giả.
- Acceptance gốc: Dùng nhiều tab; access expired; refresh/rotation; test 401/403/429; browser reload.
- Root cause / kết quả đối chiếu: Source single-flight/Web Locks/CSRF hiện có, session unit PASS. Cookie rotation/replay/multi-tab thực tế chưa chạy.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P0: Mất phiên/401 sau reload hoặc nhiều request đồng thời có thể làm logout giả.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Dùng nhiều tab; access expired; refresh/rotation; test 401/403/429; browser reload.. Source single-flight/Web Locks/CSRF hiện có, session unit PASS. Cookie rotation/replay/multi-tab thực tế chưa chạy.

### QA-003 — Đăng xuất và thu hồi các phiên

- Module / Priority / Status: Auth / Session / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 20.
- Phản ánh/rủi ro gốc: SessionRevoked và logout-all mới có client code, thiếu kiểm tra hai trình duyệt.
- Acceptance gốc: Login 2 trình duyệt; revoke 1 session; thử API và SignalR tab bị revoke.
- Root cause / kết quả đối chiếu: Source revoke/cache cleanup hiện có. Cần hai browser sessions được phép; không tự lấy token từ dev-mail hoặc private uploads.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P1: SessionRevoked và logout-all mới có client code, thiếu kiểm tra hai trình duyệt.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Login 2 trình duyệt; revoke 1 session; thử API và SignalR tab bị revoke.. Source revoke/cache cleanup hiện có. Cần hai browser sessions được phép; không tự lấy token từ dev-mail hoặc private uploads.

### QA-004 — Form và thông báo lỗi Authentication

- Module / Priority / Status: Auth / Validation / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 21.
- Phản ánh/rủi ro gốc: Thông báo lỗi 400/401/409/429, mật khẩu tối thiểu và verify chưa được kiểm thực đầy đủ.
- Acceptance gốc: Test email sai, password, chưa xác minh, rate limit, resubmit, reset expired token.
- Root cause / kết quả đối chiếu: Validation/i18n và mapping lỗi có client tests; public DTO boundary thật PASS. Verified/unverified/lockout/reset-expired trên account thật chưa chạy.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P1: Thông báo lỗi 400/401/409/429, mật khẩu tối thiểu và verify chưa được kiểm thực đầy đủ.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Test email sai, password, chưa xác minh, rate limit, resubmit, reset expired token.. Validation/i18n và mapping lỗi có client tests; public DTO boundary thật PASS. Verified/unverified/lockout/reset-expired trên account thật chưa chạy.

### QA-005 — Gửi/duyệt lời mời giữa hai tài khoản

- Module / Priority / Status: Friends / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 22.
- Phản ánh/rủi ro gốc: Không có bằng chứng create/accept friend thành công bằng backend thật.
- Acceptance gốc: 2 tài khoản 2 browser; gửi/cancel/reject/accept; reload kiểm tra backend truth.
- Root cause / kết quả đối chiếu: Requests/accept/reject/cancel source và simulation hiện có; cần hai test identities và cleanup được cấp phép.
- Files: frontend/src/features/friends; frontend/src/app/providers/RealtimeProvider.jsx.
- Backend APIs: API-033…042,121…122.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai identities, quan hệ test có quyền tạo và cleanup. / P0: Không có bằng chứng create/accept friend thành công bằng backend thật.
- Test / evidence: friends-client.spec.js; realtime.test.jsx. 2 tài khoản 2 browser; gửi/cancel/reject/accept; reload kiểm tra backend truth.. Requests/accept/reject/cancel source và simulation hiện có; cần hai test identities và cleanup được cấp phép.

### QA-006 — Chặn bạn bè và quyền gửi tin

- Module / Priority / Status: Friends / Blocking / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 23.
- Phản ánh/rủi ro gốc: Blocked state có thể chưa đồng bộ đồng thời trên cả 2 tài khoản.
- Acceptance gốc: Block/unblock khi đang mở chat; kiểm tra API errors, realtime permission changes.
- Root cause / kết quả đối chiếu: Có block/unblock/permission invalidation source, chưa có bằng chứng hai tài khoản mất quyền chat đồng thời.
- Files: frontend/src/features/friends; frontend/src/app/providers/RealtimeProvider.jsx.
- Backend APIs: API-033…042,121…122.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai identities, quan hệ test có quyền tạo và cleanup. / P1: Blocked state có thể chưa đồng bộ đồng thời trên cả 2 tài khoản.
- Test / evidence: friends-client.spec.js; realtime.test.jsx. Block/unblock khi đang mở chat; kiểm tra API errors, realtime permission changes.. Có block/unblock/permission invalidation source, chưa có bằng chứng hai tài khoản mất quyền chat đồng thời.

### QA-007 — Profile/avatar nhất quán giữa các màn hình

- Module / Priority / Status: Profile / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 24.
- Phản ánh/rủi ro gốc: Tên/avatar đổi có thể không làm mới sidebar, chat, requests.
- Acceptance gốc: Đổi tên/avatar; kiểm tra list/search/chat/group và tab thứ 2; refresh.
- Root cause / kết quả đối chiếu: Profile/avatar consumers và cache invalidation hiện có. Cần kiểm tra account khác/search/sidebar sau cập nhật thật; FND-001 bổ sung avatar decode fallback.
- Files: frontend/src/features/users; frontend/src/components/Avatar.jsx.
- Backend APIs: API-032,114…122.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai test accounts, file avatar tổng hợp. / P1: Tên/avatar đổi có thể không làm mới sidebar, chat, requests.
- Test / evidence: auth-client.spec.js; media-notifications-client.spec.js. Đổi tên/avatar; kiểm tra list/search/chat/group và tab thứ 2; refresh.. Profile/avatar consumers và cache invalidation hiện có. Cần kiểm tra account khác/search/sidebar sau cập nhật thật; FND-001 bổ sung avatar decode fallback.

### QA-008 — Conversation list, unread, preview sau hide

- Module / Priority / Status: Conversations / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 25.
- Phản ánh/rủi ro gốc: Ẩn cuộc trò chuyện/nhắn mới có thể sai preview hoặc số chưa đọc.
- Acceptance gốc: A hide B, B gửi tin mới; reload; compare GET conversation + unread.
- Root cause / kết quả đối chiếu: Hide/list/unread consumers hiện có. ConversationDto không trả latest message preview; không tự tạo nội dung. Hide rồi người kia gửi thật chưa chạy.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P1: Ẩn cuộc trò chuyện/nhắn mới có thể sai preview hoặc số chưa đọc.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. A hide B, B gửi tin mới; reload; compare GET conversation + unread.. Hide/list/unread consumers hiện có. ConversationDto không trả latest message preview; không tự tạo nội dung. Hide rồi người kia gửi thật chưa chạy.

### QA-009 — Hai người nhắn tin SignalR thật

- Module / Priority / Status: Messages / Realtime / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 26.
- Phản ánh/rủi ro gốc: 18 event handlers mới được test giả lập, chưa có authenticated WebSocket.
- Acceptance gốc: 2 account 2 browsers; kiểm tra /hubs/chat negotiate, WS 101, MessageCreated, DB persistence.
- Root cause / kết quả đối chiếu: 18 handlers chỉ có source/unit/simulation; anonymous hub 401 PASS không phải authenticated negotiate/WS101.
- Files: frontend/src/app/providers/RealtimeProvider.jsx; frontend/src/components/chat/ChatWorkspace.jsx.
- Backend APIs: 18 events trong signalr-contract.md; 4 hub methods.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Authenticated transport và hai browser; anonymous401 không đủ. / P0: 18 event handlers mới được test giả lập, chưa có authenticated WebSocket.
- Test / evidence: realtime.test.jsx; boundary.live.spec.js. 2 account 2 browsers; kiểm tra /hubs/chat negotiate, WS 101, MessageCreated, DB persistence.. 18 handlers chỉ có source/unit/simulation; anonymous hub 401 PASS không phải authenticated negotiate/WS101.

### QA-010 — Chống tin nhắn bị nhân đôi

- Module / Priority / Status: Messages / Idempotency / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 27.
- Phản ánh/rủi ro gốc: Optimistic UI + HTTP response + SignalR dễ tạo message trùng.
- Acceptance gốc: Simulate delay/offline/retry/double-click; đếm messageId/sequence sau reconnect.
- Root cause / kết quả đối chiếu: Composer giữ clientMessageId theo retry signature; merge theo ID. Chưa đếm persistence/sequence trong SQL thật sau retry/SignalR.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P0: Optimistic UI + HTTP response + SignalR dễ tạo message trùng.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. Simulate delay/offline/retry/double-click; đếm messageId/sequence sau reconnect.. Composer giữ clientMessageId theo retry signature; merge theo ID. Chưa đếm persistence/sequence trong SQL thật sau retry/SignalR.

### QA-011 — Delivered/Seen/Unread từ Backend

- Module / Priority / Status: Messages / Receipts / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 28.
- Phản ánh/rủi ro gốc: Chưa kiểm chứng receipt và read cursor thực tế.
- Acceptance gốc: Mở/đóng thread B, hai tab, offline, reconnect; kiểm tra API receipts/read-cursor.
- Root cause / kết quả đối chiếu: Read cursor theo visible foreground, delivery ACK API, receipt source hiện có. Chưa so sánh hai browser và server cursor thật.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P1: Chưa kiểm chứng receipt và read cursor thực tế.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. Mở/đóng thread B, hai tab, offline, reconnect; kiểm tra API receipts/read-cursor.. Read cursor theo visible foreground, delivery ACK API, receipt source hiện có. Chưa so sánh hai browser và server cursor thật.

### QA-012 — Cuộn và tải lịch sử không nhảy vị trí

- Module / Priority / Status: Messages / Scroll / **P1** / **VERIFIED**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 29.
- Phản ánh/rủi ro gốc: Tin nhắn mới hoặc prepend cũ có nguy cơ kéo scroll khi đang đọc cũ.
- Acceptance gốc: 100+ messages; scroll lên, load cursor, nhận tin mới; zoom/mobile/ảnh cao.
- Root cause / kết quả đối chiếu: Không tái hiện nhảy vị trí nội dung với500 tin tổng hợp/17 trang: bubble anchor shift0px; incoming/prepend/near-bottom tests PASS. Test probe ban đầu đo wrapper có date-divider nên báo43px; đã sửa probe theo nội dung đọc, không coi đó là product bug.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P1: Tin nhắn mới hoặc prepend cũ có nguy cơ kéo scroll khi đang đọc cũ.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. 100+ messages; scroll lên, load cursor, nhận tin mới; zoom/mobile/ảnh cao.. Không tái hiện nhảy vị trí nội dung với500 tin tổng hợp/17 trang: bubble anchor shift0px; incoming/prepend/near-bottom tests PASS. Test probe ban đầu đo wrapper có date-divider nên báo43px; đã sửa probe theo nội dung đọc, không coi đó là product bug.

### QA-013 — Edit 15 phút, recall, reply, reaction

- Module / Priority / Status: Messages / Actions / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 30.
- Phản ánh/rủi ro gốc: UI phải tôn trọng cửa sổ edit và reply khi message đã recall/delete.
- Acceptance gốc: Thử edit trong/ngoài 15 phút; recall, history, reply unavailable, reaction, forward.
- Root cause / kết quả đối chiếu: Edit 15 phút/recall/reply/reaction/forward consumers và cache sanitization tests có sẵn. Cần Backend permission/lifecycle thật.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P1: UI phải tôn trọng cửa sổ edit và reply khi message đã recall/delete.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. Thử edit trong/ngoài 15 phút; recall, history, reply unavailable, reaction, forward.. Edit 15 phút/recall/reply/reaction/forward consumers và cache sanitization tests có sẵn. Cần Backend permission/lifecycle thật.

### QA-014 — Tìm kiếm tin nhắn và nhảy tới ngữ cảnh

- Module / Priority / Status: Messages / Search / **P2** / **FIXED_NEEDS_VERIFICATION**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 31.
- Phản ánh/rủi ro gốc: Kết quả search có thể không điều hướng đúng conversation/sequence.
- Acceptance gốc: Tìm tin cũ đã phân trang; click; ẩn/xóa/recall message; kiểm tra xử lý không còn tồn tại.
- Root cause / kết quả đối chiếu: Đã thêm nhảy tới ngữ cảnh tin cũ bằng beforeSequence=sequence+1, giữ pagination, focus/highlight5s, trở về mới nhất và unavailable fallback. Browser regression PASS. Actual old/hidden/deleted messages còn cần Backend acceptance.
- Files: frontend/src/features/messages; frontend/src/features/conversations; frontend/src/components/chat.
- Backend APIs: API-015…028,069…082; group history/send API-067/068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai users, lịch sử tổng hợp; live tests cần tài khoản và cleanup. / P2: Kết quả search có thể không điều hướng đúng conversation/sequence.
- Test / evidence: messages-client.spec.js; hybrid-interactions.spec.js; message-cache.test.js. Tìm tin cũ đã phân trang; click; ẩn/xóa/recall message; kiểm tra xử lý không còn tồn tại.. Đã thêm nhảy tới ngữ cảnh tin cũ bằng beforeSequence=sequence+1, giữ pagination, focus/highlight5s, trở về mới nhất và unavailable fallback. Browser regression PASS. Actual old/hidden/deleted messages còn cần Backend acceptance.

### QA-015 — Reconnect, offline và missed events

- Module / Priority / Status: Realtime / Recovery / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 32.
- Phản ánh/rủi ro gốc: Sau mất kết nối, query có thể cũ hoặc không hồi phục presence/unread.
- Acceptance gốc: Tắt mạng 30s; gửi tin bên kia; bật mạng, so sánh server state và unread.
- Root cause / kết quả đối chiếu: Reconnect indicator/backoff/reconcile/dedupe hiện có. Cần hai accounts, ngắt mạng 30s và đối chiếu missed events thực.
- Files: frontend/src/app/providers/RealtimeProvider.jsx; frontend/src/components/chat/ChatWorkspace.jsx.
- Backend APIs: 18 events trong signalr-contract.md; 4 hub methods.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Authenticated transport và hai browser; anonymous401 không đủ. / P0: Sau mất kết nối, query có thể cũ hoặc không hồi phục presence/unread.
- Test / evidence: realtime.test.jsx; boundary.live.spec.js. Tắt mạng 30s; gửi tin bên kia; bật mạng, so sánh server state và unread.. Reconnect indicator/backoff/reconcile/dedupe hiện có. Cần hai accounts, ngắt mạng 30s và đối chiếu missed events thực.

### QA-016 — Typing TTL và quyền riêng tư

- Module / Priority / Status: Typing / Presence / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 33.
- Phản ánh/rủi ro gốc: Typing có thể kẹt sau ngắt mạng; online hidden hiển thị sai.
- Acceptance gốc: Start/stop typing; đóng tab; offline; privacy disabled; group nhiều user.
- Root cause / kết quả đối chiếu: Typing TTL unit có sẵn và indicator null-safe đã bổ sung. Cần privacy/đa tab/group thực; không suy từ viewer connection.
- Files: frontend/src/features/conversations/WorkspacePage.jsx; frontend/src/app/providers/RealtimeProvider.jsx.
- Backend APIs: API-113; StartTyping/StopTyping/PresenceChanged.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai user và privacy settings; nullable online phải được giữ nguyên. / P1: Typing có thể kẹt sau ngắt mạng; online hidden hiển thị sai.
- Test / evidence: improvement-ui.spec.js; realtime.test.jsx. Start/stop typing; đóng tab; offline; privacy disabled; group nhiều user.. Typing TTL unit có sẵn và indicator null-safe đã bổ sung. Cần privacy/đa tab/group thực; không suy từ viewer connection.

### QA-017 — Quyền Owner/Admin/Member ngoài thực tế

- Module / Priority / Status: Groups / Roles / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 34.
- Phản ánh/rủi ro gốc: UI có source nhưng role actions chưa test với tài khoản thật.
- Acceptance gốc: Thử promote/demote, remove, transfer-owner, leave/dissolve; role nhận realtime.
- Root cause / kết quả đối chiếu: Bổ sung guard khi myRole null: không compose/pin/manage/invite; regression PASS. Group suite17 PASS gồm role Member/Owner và former membership. Backend quyền Owner/Admin/Member thật chưa nghiệm thu, không tự cấp role.
- Files: frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx.
- Backend APIs: API-043…068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai/ba test users và group disposable; không đổi business permissions. / P0: UI có source nhưng role actions chưa test với tài khoản thật.
- Test / evidence: groups-client.spec.js; improvement-ui.spec.js. Thử promote/demote, remove, transfer-owner, leave/dissolve; role nhận realtime.. Bổ sung guard khi myRole null: không compose/pin/manage/invite; regression PASS. Group suite17 PASS gồm role Member/Owner và former membership. Backend quyền Owner/Admin/Member thật chưa nghiệm thu, không tự cấp role.

### QA-018 — Link hết hạn, thu hồi, tham gia nhóm

- Module / Priority / Status: Groups / Invite / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 35.
- Phản ánh/rủi ro gốc: Các luồng link resolve/join có thể thiếu error states.
- Acceptance gốc: Copy link sang browser mới; hết hạn, thu hồi, join trùng/blocked; URL encode token.
- Root cause / kết quả đối chiếu: Token creation-only, URL fragment, resolve/confirm join consumers có sẵn; sửa mất URL MC-005. Revoked/expired/blocked/join trùng thật chưa chạy.
- Files: frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx.
- Backend APIs: API-043…068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai/ba test users và group disposable; không đổi business permissions. / P1: Các luồng link resolve/join có thể thiếu error states.
- Test / evidence: groups-client.spec.js; improvement-ui.spec.js. Copy link sang browser mới; hết hạn, thu hồi, join trùng/blocked; URL encode token.. Token creation-only, URL fragment, resolve/confirm join consumers có sẵn; sửa mất URL MC-005. Revoked/expired/blocked/join trùng thật chưa chạy.

### QA-019 — Invitation accept/reject và trạng thái nhóm

- Module / Priority / Status: Groups / Invitation / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 36.
- Phản ánh/rủi ro gốc: Invitation có thể không đồng bộ giữa group list và notification.
- Acceptance gốc: A invite B; B accept/reject; refresh cả hai; kiểm tra quota thành viên.
- Root cause / kết quả đối chiếu: Invitation consumers hiện có; cần hai/ba accounts và GroupInvitationChanged thật, không mock thành live.
- Files: frontend/src/features/groups/GroupPages.jsx; frontend/src/features/conversations/WorkspacePage.jsx.
- Backend APIs: API-043…068.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Hai/ba test users và group disposable; không đổi business permissions. / P1: Invitation có thể không đồng bộ giữa group list và notification.
- Test / evidence: groups-client.spec.js; improvement-ui.spec.js. A invite B; B accept/reject; refresh cả hai; kiểm tra quota thành viên.. Invitation consumers hiện có; cần hai/ba accounts và GroupInvitationChanged thật, không mock thành live.

### QA-020 — Upload, preview, download thật

- Module / Priority / Status: Files / Storage / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 37.
- Phản ánh/rủi ro gốc: Source đã có nhưng chưa xác minh storage auth trên server thật.
- Acceptance gốc: Upload JPG/PNG/WebP/PDF/TXT/ZIP; kiểm tra size, MIME, auth, ID, 403, refresh.
- Root cause / kết quả đối chiếu: Authorized Blob transport hiện có và decode fallback được sửa. Chưa có upload/storage/download positive/negative với authorized files thật.
- Files: frontend/src/features/files; frontend/src/components/Avatar.jsx.
- Backend APIs: API-029…032.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật. / P0: Source đã có nhưng chưa xác minh storage auth trên server thật.
- Test / evidence: improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js. Upload JPG/PNG/WebP/PDF/TXT/ZIP; kiểm tra size, MIME, auth, ID, 403, refresh.. Authorized Blob transport hiện có và decode fallback được sửa. Chưa có upload/storage/download positive/negative với authorized files thật.

### QA-021 — Blob URL, file progress, cancel, retry

- Module / Priority / Status: Files / Memory / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 38.
- Phản ánh/rủi ro gốc: Blob không revoke có thể rò rỉ memory; progress/error không nhất quán.
- Acceptance gốc: Mở/đóng preview nhiều lần; upload file lớn/cancel; memory devtools, stale URLs.
- Root cause / kết quả đối chiếu: Unit progress/cancel/late-completion/retry/unmount PASS. Browser mở/đóng ảnh ba vòng rồi unmount: object URLs cân bằng0. Chưa chứng nhận heap/memory lâu dài với nhiều ảnh độ phân giải cao; thumbnail vẫn lấy Blob đầy đủ từ endpoint hiện có.
- Files: frontend/src/features/files; frontend/src/components/Avatar.jsx.
- Backend APIs: API-029…032.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật. / P1: Blob không revoke có thể rò rỉ memory; progress/error không nhất quán.
- Test / evidence: improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js. Mở/đóng preview nhiều lần; upload file lớn/cancel; memory devtools, stale URLs.. Unit progress/cancel/late-completion/retry/unmount PASS. Browser mở/đóng ảnh ba vòng rồi unmount: object URLs cân bằng0. Chưa chứng nhận heap/memory lâu dài với nhiều ảnh độ phân giải cao; thumbnail vẫn lấy Blob đầy đủ từ endpoint hiện có.

### QA-022 — Preview TXT và file không hỗ trợ

- Module / Priority / Status: Files / Preview / **P2** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 39.
- Phản ánh/rủi ro gốc: Preview TXT có thể chỉ hiển thị một phần và dễ bị hiểu nhầm là đầy đủ.
- Acceptance gốc: Dùng TXT dài, PDF, ZIP; kiểm tra file name Unicode, HTML injection, encoding.
- Root cause / kết quả đối chiếu: Cảnh báo TXT rút gọn, escaped text/unsupported fallback đã tồn tại, có regression. MIME/encoding/download thực chưa chạy.
- Files: frontend/src/features/files; frontend/src/components/Avatar.jsx.
- Backend APIs: API-029…032.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Ảnh JPG/PNG/WebP không nhạy cảm; test ownership/permission/storage thật. / P2: Preview TXT có thể chỉ hiển thị một phần và dễ bị hiểu nhầm là đầy đủ.
- Test / evidence: improvement-media.spec.js; media-notifications-client.spec.js; blob-url.test.jsx; files.test.js. Dùng TXT dài, PDF, ZIP; kiểm tra file name Unicode, HTML injection, encoding.. Cảnh báo TXT rút gọn, escaped text/unsupported fallback đã tồn tại, có regression. MIME/encoding/download thực chưa chạy.

### QA-023 — Thông báo realtime, đọc, đọc tất cả

- Module / Priority / Status: Notifications / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 40.
- Phản ánh/rủi ro gốc: Notification count/mark-read có thể lệch giữa popover, trang và badge.
- Acceptance gốc: B nhận 2 thông báo; đọc 1/read all; reload, WebSocket, request failure rollback.
- Root cause / kết quả đối chiếu: Page/tray/nav dùng chung count/query/actions, mutate xong mới invalidate. Cần NotificationCreated/Read hai tab với server thật.
- Files: frontend/src/features/notifications; frontend/src/components/layout/NavigationLinks.jsx.
- Backend APIs: API-107…112; BCR-001.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Server count/read truth, hai users cho realtime; Delete chờ duyệt. / P1: Notification count/mark-read có thể lệch giữa popover, trang và badge.
- Test / evidence: media-notifications-client.spec.js; improvement-ui.spec.js. B nhận 2 thông báo; đọc 1/read all; reload, WebSocket, request failure rollback.. Page/tray/nav dùng chung count/query/actions, mutate xong mới invalidate. Cần NotificationCreated/Read hai tab với server thật.

### QA-024 — Điều hướng đúng từ notification

- Module / Priority / Status: Notifications / Navigation / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 41.
- Phản ánh/rủi ro gốc: ReferenceId/Type không phải lúc nào cũng có route hợp lệ.
- Acceptance gốc: Test mỗi notification type, xóa/block/disabled resource, 403/404 và mobile.
- Root cause / kết quả đối chiếu: Message/GroupMessage/MessageRequest resolve qua authorized message API trước navigate; unknown type không bịa route. Cần actual deleted/forbidden resources.
- Files: frontend/src/features/notifications; frontend/src/components/layout/NavigationLinks.jsx.
- Backend APIs: API-107…112; BCR-001.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Server count/read truth, hai users cho realtime; Delete chờ duyệt. / P1: ReferenceId/Type không phải lúc nào cũng có route hợp lệ.
- Test / evidence: media-notifications-client.spec.js; improvement-ui.spec.js. Test mỗi notification type, xóa/block/disabled resource, 403/404 và mobile.. Message/GroupMessage/MessageRequest resolve qua authorized message API trước navigate; unknown type không bịa route. Cần actual deleted/forbidden resources.

### QA-025 — Lưu lựa chọn UI qua reload/tài khoản

- Module / Priority / Status: Settings / User preference / **P2** / **VERIFIED**.
- Loại: IMPROVEMENT_REQUEST. Source: DANH SÁCH CẢI TIẾN dòng 42.
- Phản ánh/rủi ro gốc: Toast theme/tùy chọn popover có thể bị reset hoặc ảnh hưởng account khác.
- Acceptance gốc: Đổi theme/toast, F5, logout, login account khác, private mode.
- Root cause / kết quả đối chiếu: Local toast preference theo userId/browser: default on, reload và account isolation unit PASS; browser context private/reload/off retains inline PASS. Không lưu token, không quảng cáo sync hoặc persistence sau khi đóng private session; theme browser-wide giữ nguyên.
- Files: frontend/src/stores/ui-feedback-store.js; frontend/src/components/ToastViewport.jsx; frontend/src/components/forms/ValidatedForm.jsx; frontend/src/features/users/InterfacePreferences.jsx.
- Backend APIs: Không thêm API; gọi consumer hiện có rồi mới success.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Local preference theo account/browser; không hứa sync đa thiết bị. / P2: Toast theme/tùy chọn popover có thể bị reset hoặc ảnh hưởng account khác.
- Test / evidence: ui-feedback.test.jsx; improvement-ui.spec.js. Đổi theme/toast, F5, logout, login account khác, private mode.. Local toast preference theo userId/browser: default on, reload và account isolation unit PASS; browser context private/reload/off retains inline PASS. Không lưu token, không quảng cáo sync hoặc persistence sau khi đóng private session; theme browser-wide giữ nguyên.

### QA-026 — Phân quyền Admin backend thực

- Module / Priority / Status: Admin / Security / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 43.
- Phản ánh/rủi ro gốc: Route guard không thay thế được backend authorization.
- Acceptance gốc: 2 account thật; truy cập direct URL, gửi GET/POST admin endpoint, force reload.
- Root cause / kết quả đối chiếu: DB-role guard source và simulation PASS; anonymous admin401 thật PASS. Chưa có Admin200/User403 thật, không tự cấp quyền.
- Files: frontend/src/features/admin; frontend/src/app/guards.jsx.
- Backend APIs: API-083…101.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Admin/User được cấp sẵn; không cấp role, không chạy purge hoặc job xóa. / P0: Route guard không thay thế được backend authorization.
- Test / evidence: moderation-admin-client.spec.js; boundary.live.spec.js. 2 account thật; truy cập direct URL, gửi GET/POST admin endpoint, force reload.. DB-role guard source và simulation PASS; anonymous admin401 thật PASS. Chưa có Admin200/User403 thật, không tự cấp quyền.

### QA-027 — Report, Appeal và token cô lập

- Module / Priority / Status: Moderation / Appeals / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 44.
- Phản ánh/rủi ro gốc: Token appeal riêng có thể bị trộn với auth thường.
- Acceptance gốc: Submit report; appeal suspended test identity, 401/403, change identity và logout.
- Root cause / kết quả đối chiếu: Appeal transport/scope/cache riêng và timeout cleanup hiện có. Chưa có disposable suspended identity được cấp phép; không tự đình chỉ account.
- Files: frontend/src/features/moderation; frontend/src/features/admin.
- Backend APIs: API-102…106,086…096.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Disposable suspended identity được phê duyệt; token appeal cô lập. / P1: Token appeal riêng có thể bị trộn với auth thường.
- Test / evidence: moderation-admin-client.spec.js. Submit report; appeal suspended test identity, 401/403, change identity và logout.. Appeal transport/scope/cache riêng và timeout cleanup hiện có. Chưa có disposable suspended identity được cấp phép; không tự đình chỉ account.

### QA-028 — Retention/audit không gây thao tác nguy hiểm

- Module / Priority / Status: Admin / Retention / **P0** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 45.
- Phản ánh/rủi ro gốc: Admin UI có preview/plan/hold; nếu thiếu xác nhận có thể gây nhầm tác động thật.
- Acceptance gốc: Admin preview, dry-run, hold an toàn; không chạy job xóa, không sửa config Backend.
- Root cause / kết quả đối chiếu: UI chỉ preview/explicit dry-run/hold, có confirmation. Không purge execution. Admin actual permissions/audit/hold chưa kiểm thử.
- Files: frontend/src/features/admin; frontend/src/app/guards.jsx.
- Backend APIs: API-083…101.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Admin/User được cấp sẵn; không cấp role, không chạy purge hoặc job xóa. / P0: Admin UI có preview/plan/hold; nếu thiếu xác nhận có thể gây nhầm tác động thật.
- Test / evidence: moderation-admin-client.spec.js; boundary.live.spec.js. Admin preview, dry-run, hold an toàn; không chạy job xóa, không sửa config Backend.. UI chỉ preview/explicit dry-run/hold, có confirmation. Không purge execution. Admin actual permissions/audit/hold chưa kiểm thử.

### QA-029 — Không lộ secrets hoặc user data trong Git/console

- Module / Priority / Status: Security / Privacy / **P0** / **VERIFIED**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 46.
- Phản ánh/rủi ro gốc: Preview/logs/screenshot có thể chứa JWT, mật khẩu, URL token, email hoặc tin nhắn thật.
- Acceptance gốc: Scan tracked/untracked; verify .gitignore, dev-mail, playwright auth, .env; không commit 289 ảnh dữ liệu thật.
- Root cause / kết quả đối chiếu: Value-free Git/source scan không phát hiện JWT-shaped value/private key/sensitive tracked paths/production console. Workbook riêng tư được ignore, bản trích ảnh tạm gỡ; screenshot mới đều synthetic fixtures. Scope scan và giới hạn được ghi trong improvement-privacy-audit.json, không chứng nhận mọi dữ liệu ngoài repository.
- Files: .gitignore; frontend/tests/e2e; frontend/src/lib/http.js; scripts/read-improvement-workbook.py.
- Backend APIs: Không gọi API business hoặc xuất credentials.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chỉ scan output category/path/count, không xuất nội dung nhạy cảm. / P0: Preview/logs/screenshot có thể chứa JWT, mật khẩu, URL token, email hoặc tin nhắn thật.
- Test / evidence: git status/ls-files; privacy scan; production isolation. Scan tracked/untracked; verify .gitignore, dev-mail, playwright auth, .env; không commit 289 ảnh dữ liệu thật.. Value-free Git/source scan không phát hiện JWT-shaped value/private key/sensitive tracked paths/production console. Workbook riêng tư được ignore, bản trích ảnh tạm gỡ; screenshot mới đều synthetic fixtures. Scope scan và giới hạn được ghi trong improvement-privacy-audit.json, không chứng nhận mọi dữ liệu ngoài repository.

### QA-030 — Keyboard, focus, contrast, screen readers

- Module / Priority / Status: Accessibility / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 47.
- Phản ánh/rủi ro gốc: Axe/browser tests chưa thay thế kiểm tra bàn phím và screen reader thật.
- Acceptance gốc: Tab/Shift+Tab/Esc, zoom 200%, reduced motion, NVDA, kiểm tra focus labels/contrast.
- Root cause / kết quả đối chiếu: Axe/keyboard/focus/contrast có browser coverage; chưa có NVDA/native assistive-tech acceptance. Không coi axe là WCAG certification.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P1: Axe/browser tests chưa thay thế kiểm tra bàn phím và screen reader thật.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Tab/Shift+Tab/Esc, zoom 200%, reduced motion, NVDA, kiểm tra focus labels/contrast.. Axe/keyboard/focus/contrast có browser coverage; chưa có NVDA/native assistive-tech acceptance. Không coi axe là WCAG certification.

### QA-031 — Bàn phím ảo và bộ gõ tiếng Việt

- Module / Priority / Status: Mobile / IME / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 48.
- Phản ánh/rủi ro gốc: DevTools không mô phỏng đầy đủ safe area/keyboard/IME.
- Acceptance gốc: Test Android Chrome/iOS Safari nếu có thiết bị; gõ Telex, emoji, multiline, scroll.
- Root cause / kết quả đối chiếu: Enter/composition/shrunken viewport simulation có sẵn; chưa có Android/iOS keyboard Telex và safe area thật.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P1: DevTools không mô phỏng đầy đủ safe area/keyboard/IME.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Test Android Chrome/iOS Safari nếu có thiết bị; gõ Telex, emoji, multiline, scroll.. Enter/composition/shrunken viewport simulation có sẵn; chưa có Android/iOS keyboard Telex và safe area thật.

### QA-032 — Giao diện khi sidebar/panel thay đổi

- Module / Priority / Status: Responsive / Layout / **P1** / **VERIFIED**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 49.
- Phản ánh/rủi ro gốc: Vùng chat cần đúng ưu tiên khi mở details/group members và đổi viewport.
- Acceptance gốc: Resize liên tục; mở modal + sidebar + details; long names, large zoom, theme.
- Root cause / kết quả đối chiếu: Full Chromium regression PASS với 5 widths × 2 themes, selected/list/details/modal/long text/composer, no horizontal overflow; preview/toast không che composer/header. Đây là acceptance hình học UI browser; native keyboard nằm ở QA-031.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P1: Vùng chat cần đúng ưu tiên khi mở details/group members và đổi viewport.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Resize liên tục; mở modal + sidebar + details; long names, large zoom, theme.. Full Chromium regression PASS với 5 widths × 2 themes, selected/list/details/modal/long text/composer, no horizontal overflow; preview/toast không che composer/header. Đây là acceptance hình học UI browser; native keyboard nằm ở QA-031.

### QA-033 — Còn chuỗi tiếng Anh, lỗi dấu hoặc overflow

- Module / Priority / Status: i18n / vi-VN / **P1** / **FIXED_NEEDS_VERIFICATION**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 50.
- Phản ánh/rủi ro gốc: 646 translation calls PASS không chứng minh toàn bộ text hiển thị đã Việt hóa.
- Acceptance gốc: Audit text strings, 320px, screens empty/error/toast/admin/datepicker; aria-label.
- Root cause / kết quả đối chiếu: vi-VN/Inter/Intl hiện có; static audit sau sửa 679 keys PASS. Vẫn cần audit text động/visual; không dịch user content/contracts.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P1: 646 translation calls PASS không chứng minh toàn bộ text hiển thị đã Việt hóa.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Audit text strings, 320px, screens empty/error/toast/admin/datepicker; aria-label.. vi-VN/Inter/Intl hiện có; static audit sau sửa 679 keys PASS. Vẫn cần audit text động/visual; không dịch user content/contracts.

### QA-034 — Tốc độ render, bundle, tin nhắn dài

- Module / Priority / Status: Performance / **P2** / **FIXED_NEEDS_VERIFICATION**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 51.
- Phản ánh/rủi ro gốc: Bundle đã split nhưng chưa có Lighthouse hay load profiling thực.
- Acceptance gốc: Lighthouse/Performance browser, CPU throttle, 500 messages, memory, images lazy load.
- Root cause / kết quả đối chiếu: Giảm idle typing rerender và memo các hàng tin/feature renderers. 500 incoming messages/17 cursor pages PASS; có input samples4xCPU + heapUsed metrics từ DevTools, không phải production Web Vitals/Lighthouse/load. Authenticated production profiling vẫn cần môi trường test.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P2: Bundle đã split nhưng chưa có Lighthouse hay load profiling thực.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Lighthouse/Performance browser, CPU throttle, 500 messages, memory, images lazy load.. Giảm idle typing rerender và memo các hàng tin/feature renderers. 500 incoming messages/17 cursor pages PASS; có input samples4xCPU + heapUsed metrics từ DevTools, không phải production Web Vitals/Lighthouse/load. Authenticated production profiling vẫn cần môi trường test.

### QA-035 — Mã lỗi 400/401/403/404/409/429/500 và timeout

- Module / Priority / Status: API Error Handling / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 52.
- Phản ánh/rủi ro gốc: Không phải mọi 404 đều có cùng ý nghĩa; có thể toast gây hiểu lầm.
- Acceptance gốc: Simulate từng mã lỗi, timeout, offline, rate limit; kiểm tra ErrorBoundary.
- Root cause / kết quả đối chiếu: Safe error mapping/unit + client failures hiện có. Cần actual endpoints trả 403/404/409/429, timeout/draft; không hiển thị raw exception.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P1: Không phải mọi 404 đều có cùng ý nghĩa; có thể toast gây hiểu lầm.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Simulate từng mã lỗi, timeout, offline, rate limit; kiểm tra ErrorBoundary.. Safe error mapping/unit + client failures hiện có. Cần actual endpoints trả 403/404/409/429, timeout/draft; không hiển thị raw exception.

### QA-036 — PWA-ready khác với PWA đã hoạt động

- Module / Priority / Status: PWA / Deployment / **P2** / **VERIFIED**.
- Loại: ALREADY_RESOLVED_SOURCE. Source: DANH SÁCH CẢI TIẾN dòng 53.
- Phản ánh/rủi ro gốc: Đã chốt “PWA-ready”, không có bằng chứng manifest/service-worker/offline.
- Acceptance gốc: Kiểm tra metadata/icons/manifest khi cần; không gắn nhãn installable nếu chưa chạy audit.
- Root cause / kết quả đối chiếu: Đã kiểm source/index và README: không service worker/offline private caching, không claim installable PWA. README nêu rõ responsive preparation; triển khai PWA thật là scope riêng.
- Files: frontend/src/styles/index.css; frontend/src/components/chat; frontend/src/components/layout; frontend/src/i18n.
- Backend APIs: Consumer hiện có, không thay đổi contract.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chromium; native Chrome/Edge zoom, NVDA và mobile vật lý khi áp dụng. / P2: Đã chốt “PWA-ready”, không có bằng chứng manifest/service-worker/offline.
- Test / evidence: design.spec.js; hybrid-interactions.spec.js; improvement-ui.spec.js; improvement-media.spec.js. Kiểm tra metadata/icons/manifest khi cần; không gắn nhãn installable nếu chưa chạy audit.. Đã kiểm source/index và README: không service worker/offline private caching, không claim installable PWA. README nêu rõ responsive preparation; triển khai PWA thật là scope riêng.

### QA-037 — README/demo steps và ảnh minh họa an toàn

- Module / Priority / Status: Portfolio / Documentation / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 54.
- Phản ánh/rủi ro gốc: Chưa có bằng chứng demo tích hợp cuối; tài liệu có thể quảng cáo quá mức.
- Acceptance gốc: Clone sạch, npm ci, dotnet build, run, thử 2 account, rà soát ảnh README.
- Root cause / kết quả đối chiếu: README có BE/FE commands/env/anonymous vs business limits, đã sửa câu design checkpoint cũ. Clean clone/two-account demo/end-to-end chưa nghiệm thu.
- Files: README.md; roadmapFE.md; docs/frontend.
- Backend APIs: 123 source mappings; live acceptance chưa PASS.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Môi trường hai account, .NET/SQL cấu hình sẵn; không migration/seed. / P1: Chưa có bằng chứng demo tích hợp cuối; tài liệu có thể quảng cáo quá mức.
- Test / evidence: README/source review; clean clone và demo thật chưa chạy. Clone sạch, npm ci, dotnet build, run, thử 2 account, rà soát ảnh README.. README có BE/FE commands/env/anonymous vs business limits, đã sửa câu design checkpoint cũ. Clean clone/two-account demo/end-to-end chưa nghiệm thu.

### QA-038 — Chưa commit checkpoint Frontend

- Module / Priority / Status: Git / Checkpoint / **P1** / **NOT_REPRODUCIBLE**.
- Loại: ALREADY_RESOLVED_SOURCE. Source: DANH SÁCH CẢI TIẾN dòng 55.
- Phản ánh/rủi ro gốc: Codex tạo nhiều file, nếu chưa commit dễ mất mốc trước sửa lỗi.
- Acceptance gốc: git status/ls-files/diff --cached; ignore upload & reports; commit trên feature branch.
- Root cause / kết quả đối chiếu: Git hiện đã có babbcfd feat(frontend): implement MeChat UI and API integration, 167 frontend files tracked trước đợt này. Không còn đúng giả định toàn bộ Frontend chưa có checkpoint. Chưa stage/commit/push đợt sửa mới.
- Files: .gitignore; frontend/tests/e2e; frontend/src/lib/http.js; scripts/read-improvement-workbook.py.
- Backend APIs: Không gọi API business hoặc xuất credentials.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Chỉ scan output category/path/count, không xuất nội dung nhạy cảm. / P1: Codex tạo nhiều file, nếu chưa commit dễ mất mốc trước sửa lỗi.
- Test / evidence: git status/ls-files; privacy scan; production isolation. git status/ls-files/diff --cached; ignore upload & reports; commit trên feature branch.. Git hiện đã có babbcfd feat(frontend): implement MeChat UI and API integration, 167 frontend files tracked trước đợt này. Không còn đúng giả định toàn bộ Frontend chưa có checkpoint. Chưa stage/commit/push đợt sửa mới.

### QA-039 — Chuyển tài khoản hoặc logout khi request đang chạy

- Module / Priority / Status: Cross-tab / Identity / **P1** / **BLOCKED_TEST_ENVIRONMENT**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 56.
- Phản ánh/rủi ro gốc: Stale query/Mutation có thể tràn dữ liệu tài khoản trước sang tài khoản sau.
- Acceptance gốc: Login A→logout→B khi slow network/long upload; so sánh userId, notification, profile.
- Root cause / kết quả đối chiếu: Identity generation/401 actor guard/query clear/hub dispose hiện có, unit PASS. Cần A→B khi upload/API chậm thật; không coi unit là Backend acceptance.
- Files: frontend/src/lib/http.js; frontend/src/features/auth; frontend/src/features/users/AccountPage.jsx.
- Backend APIs: API-001…014,114.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Test account được cấp phép, mail token do operator tự thao tác khi cần; không lấy từ private uploads. / P1: Stale query/Mutation có thể tràn dữ liệu tài khoản trước sang tài khoản sau.
- Test / evidence: session.test.js; auth-client.spec.js; auth.live.spec.js. Login A→logout→B khi slow network/long upload; so sánh userId, notification, profile.. Identity generation/401 actor guard/query clear/hub dispose hiện có, unit PASS. Cần A→B khi upload/API chậm thật; không coi unit là Backend acceptance.

### QA-040 — Thông báo quan trọng không bị mất do auto-dismiss

- Module / Priority / Status: Error / Feedback / **P1** / **VERIFIED**.
- Loại: QA_RISK. Source: DANH SÁCH CẢI TIẾN dòng 57.
- Phản ánh/rủi ro gốc: Toast 2 giây đối với lỗi hoặc cảnh báo có thể không đọc kịp.
- Acceptance gốc: Giả lập upload fail/session expired/delete confirm; screen reader aria-live.
- Root cause / kết quả đối chiếu: Toast chỉ success; lỗi form/modal vẫn tồn tại sau2s và preference off không xóa feedback. Browser500 giả lập giữ alert sau2200ms, không lộ raw server text; unit close/pause/identity PASS. Không suy ra API500 thật đã nghiệm thu.
- Files: frontend/src/stores/ui-feedback-store.js; frontend/src/components/ToastViewport.jsx; frontend/src/components/forms/ValidatedForm.jsx; frontend/src/features/users/InterfacePreferences.jsx.
- Backend APIs: Không thêm API; gọi consumer hiện có rồi mới success.
- Giải pháp: Giữ fix tối thiểu đã mô tả và chạy lại acceptance; không đổi contract. Thiếu live evidence thì giữ chưa nghiệm thu.
- Dependencies / risk: Local preference theo account/browser; không hứa sync đa thiết bị. / P1: Toast 2 giây đối với lỗi hoặc cảnh báo có thể không đọc kịp.
- Test / evidence: ui-feedback.test.jsx; improvement-ui.spec.js. Giả lập upload fail/session expired/delete confirm; screen reader aria-live.. Toast chỉ success; lỗi form/modal vẫn tồn tại sau2s và preference off không xóa feedback. Browser500 giả lập giữ alert sau2200ms, không lộ raw server text; unit close/pause/identity PASS. Không suy ra API500 thật đã nghiệm thu.

## Phát hiện mới ngoài workbook

Giữ 52 ID gốc; thêm4 phát hiện, tổng phạm vi theo dõi56. Không dùng lại ID của workbook.

- **FND-001 / P1 / VERIFIED** (QA-007/021): Avatar không có decode-error fallback. Đã chuyển về initials, revoke URL khi unmount; blob-url.test.jsx2 PASS. Không chứng minh actual avatar storage.
- **FND-002 / P1 / VERIFIED** (MC-007/QA-030): Popover mới dùng animation chưa có keyframes làm Radix giữ closed content. Đã sửa surface-enter + closed animation none; loading/empty/error Escape/focus và matrix19 PASS.
- **FND-003 / P2 / VERIFIED** (QA-034): Typing TTL tạo array/state/context mới mỗi giây ngay khi idle. Giữ reference khi không đổi, memo context và message rows/renderers. Idle consumer unit và 500-message profile PASS; production performance riêng chưa nghiệm thu.
- **FND-004 / P1 / FIXED_NEEDS_VERIFICATION** (QA-017): Frontend chỉ kiểm status Active, thiếu myRole cho historical group viewer. Đã khóa compose/invite/manage và pin khi không còn role hoạt động. Browser17 group checks PASS; actual Backend role acceptance còn bị chặn môi trường.

## Next Exact Action

Client regression và source đã lưu; chờ BCR approval/test identities. Thứ tự live acceptance và các điều kiện chưa xác minh ở checkpoint cuối bên dưới.

## Checkpoint cuối và kết quả nghiệm thu

52 ticket gốc: {'VERIFIED': 8, 'BLOCKED_BACKEND_APPROVAL': 2, 'FIXED_NEEDS_VERIFICATION': 12, 'BLOCKED_TEST_ENVIRONMENT': 29, 'NOT_REPRODUCIBLE': 1}. Cộng4 FND: {'VERIFIED': 11, 'BLOCKED_BACKEND_APPROVAL': 2, 'FIXED_NEEDS_VERIFICATION': 13, 'BLOCKED_TEST_ENVIRONMENT': 29, 'NOT_REPRODUCIBLE': 1}. Remaining (chưa VERIFIED/NOT_REPRODUCIBLE): 44.

- **40 unit/component PASS** trong11 files; **176 Chromium browser PASS,0 FAIL,0 flaky**; [full cases](improvement-final-regression.json). Các focused reruns có trùng ca, không cộng vào176.
- **56 actual Chromium browser zoom combinations PASS** qua chrome.tabs.setZoom, độc lập5viewport×2theme. Native Chrome/Edge/thiết bị vẫn NEEDS_VERIFICATION.
- Build/lint PASS; localization 687 calls PASS; source API 122 function mappings PASS; production isolation PASS, production smoke 1 PASS; npm audit 0 vulnerabilities; real anonymous boundaries 4 PASS; successful auth 1 SKIPPED.
- Screenshots57 PNG mới từ browser thật với synthetic data, gồm login/media/notification tray/toast/zoom/group CTA; [chỉ mục](improvement-screenshots.md). Đã xem lại ảnh đại diện Light/Dark/mobile/zoom; không có ảnh tài khoản thật.
- Final machine evidence: improvement-verification.json; privacy scope: improvement-privacy-audit.json; performance sampling: improvement-client-performance.json. Performance số đo gồm overhead automation/dev, không chứng nhận Web Vitals hoặc tải SQL.

Các mục VERIFIED là ticket có acceptance độc lập ở Frontend/docs và evidence tương ứng. Không có successful Backend business endpoint mới được nâng PASS. MC-011/012 vẫn FIXED_NEEDS_VERIFICATION cho original storage/device issue. Không cấp role, sửa Backend/DB, migration/seed, xóa dữ liệu, stage/commit/push.

Lịch sử failure: thiếu zoom3 FAIL; decode/MIME2 FAIL; one-time URL reset1 FAIL; popover closed animation và accessible-count label đã bị phát hiện rồi sửa. Một unit đầu cần tách server navigation khỏi preview để không yêu cầu QueryProvider ở pure component. Performance probe đầu đo wrapper/date-divider43px, sau sửa probe bubble0px; lượt khác timeout120s khi load390 messages trước tối ưu idle/render. Giữ lịch sử, không dùng skip/retry che lỗi.

Next Exact Action hiện hành: Product Owner xem BCR-001/BCR-002. Khi có test identities được cấp phép qua kênh cấu hình an toàn, chạy Auth/session lifecycle trước, rồi two-user friends/messages/groups/unread/presence/reconnect/files; Admin/appeal/retention theo quyền được cấp. Chạy native Chrome/Edge/NVDA/Android/iOS và authenticated production performance theo live-integration-acceptance.md. Không còn task source dang dở; phần chưa xác minh không được coi DONE.

## Files đã sửa hoặc tạo

Danh sách lấy từ Git diff + untracked không bị ignore. Hai file người dùng đã xóa có trước được giữ nguyên, không tính vào thay đổi của đợt này. Không có Backend/Database source.

- [.gitignore](../../.gitignore)
- [README.md](../../README.md)
- [docs/frontend/api-consumer-audit.json](../../docs/frontend/api-consumer-audit.json)
- [docs/frontend/backend-change-requests.md](../../docs/frontend/backend-change-requests.md)
- [docs/frontend/backend-gaps.md](../../docs/frontend/backend-gaps.md)
- [docs/frontend/design-review.md](../../docs/frontend/design-review.md)
- [docs/frontend/improvement-baseline-2026-11-07.json](../../docs/frontend/improvement-baseline-2026-11-07.json)
- [docs/frontend/improvement-browser-zoom.json](../../docs/frontend/improvement-browser-zoom.json)
- [docs/frontend/improvement-client-performance.json](../../docs/frontend/improvement-client-performance.json)
- [docs/frontend/improvement-final-regression.json](../../docs/frontend/improvement-final-regression.json)
- [docs/frontend/improvement-intermediate-174.json](../../docs/frontend/improvement-intermediate-174.json)
- [docs/frontend/improvement-privacy-audit.json](../../docs/frontend/improvement-privacy-audit.json)
- [docs/frontend/improvement-progress.md](../../docs/frontend/improvement-progress.md)
- [docs/frontend/improvement-screenshots.md](../../docs/frontend/improvement-screenshots.md)
- [docs/frontend/improvement-tickets.json](../../docs/frontend/improvement-tickets.json)
- [docs/frontend/improvement-verification.json](../../docs/frontend/improvement-verification.json)
- [docs/frontend/live-integration-acceptance.md](../../docs/frontend/live-integration-acceptance.md)
- [docs/frontend/screenshot-index.md](../../docs/frontend/screenshot-index.md)
- [docs/frontend/screenshots/login-focus-mobile.png](../../docs/frontend/screenshots/login-focus-mobile.png)
- [docs/frontend/screenshots/login-validation-mobile.png](../../docs/frontend/screenshots/login-validation-mobile.png)
- [docs/frontend/ui-ux-acceptance.md](../../docs/frontend/ui-ux-acceptance.md)
- [docs/frontend/ui-ux-spec.md](../../docs/frontend/ui-ux-spec.md)
- [docs/frontend/verification-report.md](../../docs/frontend/verification-report.md)
- [frontend/scripts/audit-api-consumers.mjs](../../frontend/scripts/audit-api-consumers.mjs)
- [frontend/scripts/verify-browser-zoom.mjs](../../frontend/scripts/verify-browser-zoom.mjs)
- [frontend/src/app/App.jsx](../../frontend/src/app/App.jsx)
- [frontend/src/app/providers/RealtimeProvider.jsx](../../frontend/src/app/providers/RealtimeProvider.jsx)
- [frontend/src/components/Avatar.jsx](../../frontend/src/components/Avatar.jsx)
- [frontend/src/components/ConfirmAction.jsx](../../frontend/src/components/ConfirmAction.jsx)
- [frontend/src/components/ToastViewport.jsx](../../frontend/src/components/ToastViewport.jsx)
- [frontend/src/components/chat/ChatWorkspace.jsx](../../frontend/src/components/chat/ChatWorkspace.jsx)
- [frontend/src/components/chat/MessageEntry.jsx](../../frontend/src/components/chat/MessageEntry.jsx)
- [frontend/src/components/forms/ValidatedForm.jsx](../../frontend/src/components/forms/ValidatedForm.jsx)
- [frontend/src/components/layout/ApplicationLayout.jsx](../../frontend/src/components/layout/ApplicationLayout.jsx)
- [frontend/src/components/layout/NavigationLinks.jsx](../../frontend/src/components/layout/NavigationLinks.jsx)
- [frontend/src/features/conversations/WorkspacePage.jsx](../../frontend/src/features/conversations/WorkspacePage.jsx)
- [frontend/src/features/files/AttachmentPreview.jsx](../../frontend/src/features/files/AttachmentPreview.jsx)
- [frontend/src/features/files/AvatarEditor.jsx](../../frontend/src/features/files/AvatarEditor.jsx)
- [frontend/src/features/groups/GroupPages.jsx](../../frontend/src/features/groups/GroupPages.jsx)
- [frontend/src/features/messages/MessageActions.jsx](../../frontend/src/features/messages/MessageActions.jsx)
- [frontend/src/features/messages/MessageSearch.jsx](../../frontend/src/features/messages/MessageSearch.jsx)
- [frontend/src/features/notifications/NotificationPreview.jsx](../../frontend/src/features/notifications/NotificationPreview.jsx)
- [frontend/src/features/notifications/NotificationsPage.jsx](../../frontend/src/features/notifications/NotificationsPage.jsx)
- [frontend/src/features/notifications/use-notifications.js](../../frontend/src/features/notifications/use-notifications.js)
- [frontend/src/features/users/AccountPage.jsx](../../frontend/src/features/users/AccountPage.jsx)
- [frontend/src/features/users/InterfacePreferences.jsx](../../frontend/src/features/users/InterfacePreferences.jsx)
- [frontend/src/i18n/locales/vi/common.json](../../frontend/src/i18n/locales/vi/common.json)
- [frontend/src/i18n/locales/vi/files.json](../../frontend/src/i18n/locales/vi/files.json)
- [frontend/src/i18n/locales/vi/groups.json](../../frontend/src/i18n/locales/vi/groups.json)
- [frontend/src/i18n/locales/vi/messages.json](../../frontend/src/i18n/locales/vi/messages.json)
- [frontend/src/i18n/locales/vi/notifications.json](../../frontend/src/i18n/locales/vi/notifications.json)
- [frontend/src/i18n/locales/vi/users.json](../../frontend/src/i18n/locales/vi/users.json)
- [frontend/src/stores/ui-feedback-store.js](../../frontend/src/stores/ui-feedback-store.js)
- [frontend/src/styles/index.css](../../frontend/src/styles/index.css)
- [frontend/tests/blob-url.test.jsx](../../frontend/tests/blob-url.test.jsx)
- [frontend/tests/e2e/auth-client.spec.js](../../frontend/tests/e2e/auth-client.spec.js)
- [frontend/tests/e2e/groups-client.spec.js](../../frontend/tests/e2e/groups-client.spec.js)
- [frontend/tests/e2e/improvement-media.spec.js](../../frontend/tests/e2e/improvement-media.spec.js)
- [frontend/tests/e2e/improvement-performance.spec.js](../../frontend/tests/e2e/improvement-performance.spec.js)
- [frontend/tests/e2e/improvement-ui.spec.js](../../frontend/tests/e2e/improvement-ui.spec.js)
- [frontend/tests/e2e/media-notifications-client.spec.js](../../frontend/tests/e2e/media-notifications-client.spec.js)
- [frontend/tests/e2e/messages-client.spec.js](../../frontend/tests/e2e/messages-client.spec.js)
- [frontend/tests/fixtures/zoom-extension/manifest.json](../../frontend/tests/fixtures/zoom-extension/manifest.json)
- [frontend/tests/fixtures/zoom-extension/worker.js](../../frontend/tests/fixtures/zoom-extension/worker.js)
- [frontend/tests/realtime.test.jsx](../../frontend/tests/realtime.test.jsx)
- [frontend/tests/ui-feedback.test.jsx](../../frontend/tests/ui-feedback.test.jsx)
- [frontend/tests/upload-draft.test.jsx](../../frontend/tests/upload-draft.test.jsx)
- [roadmapFE.md](../../roadmapFE.md)
- [scripts/audit-frontend-privacy.py](../../scripts/audit-frontend-privacy.py)
- [scripts/build-improvement-ledger.py](../../scripts/build-improvement-ledger.py)
- [scripts/finalize-improvement-evidence.py](../../scripts/finalize-improvement-evidence.py)
- [scripts/read-improvement-workbook.py](../../scripts/read-improvement-workbook.py)

Focused cuối: matrix hover/focus/capture10 PASS và ảnh CTA nhóm390px1 PASS; có ca trùng full176, không cộng thành full mới. Workbook gốc không được ghi lại bởi script này.
