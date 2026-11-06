# Phê duyệt Visual Baseline và Vietnamese-first — 06/11/2026

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. Product Owner đã duyệt phương án C — MeChat Hybrid và cả ba mẫu Login, Private Chat, Group Chat. Các màn hình tiếp theo dùng cùng design system; không hỏi duyệt lại cùng hướng thiết kế. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.

Ngôn ngữ bắt buộc: toàn bộ User/Admin UI dùng tiếng Việt, i18next vi-VN/fallback vi, HTML lang=vi, Intl Asia/Ho_Chi_Minh. Không dịch contracts hoặc nội dung người dùng. Xem [implementation-contract.md](implementation-contract.md). Các trạng thái chờ duyệt/ảnh tiếng Anh bên dưới là **lịch sử trước quyết định này**; gate hiện đã được mở. Việt hóa không mở lại Design Gate.

---

# MeChat Hybrid — Design Review

Ngày review: **2026-11-06**, Asia/Bangkok. Visual Direction chính thức: **C — MeChat Hybrid (Modern Messenger + Minimalist SaaS)**, được Product Owner chốt. Ba màn hình **chưa được duyệt**; giữ **BLOCKED: DESIGN_APPROVAL_REQUIRED**.

Đề xuất: **sẵn sàng để Product Owner xem xét duyệt UI mẫu**. Đây không phải nghiệm thu toàn bộ frontend, tích hợp backend, WCAG certification hoặc PORTFOLIO_READY.

## Phạm vi và cách đánh giá

Chỉ cải thiện Login, Private Chat, Group Chat và nền tảng UI dùng chung. Không sửa backend, database, DTO, business rules; không triển khai thêm module thật. Các hành vi bổ sung được gắn nhãn **Design preview / Sample / Local only**, chỉ sửa fixtures trong development. Production vẫn gọi các adapter thật hiện có; không thêm API mới trong lần review này.

Các điểm dưới đây là **tự đánh giá có chủ đích phục vụ phê duyệt**, không phải chứng nhận khách quan hay điểm do axe tự chấm. Điểm dựa trên source, ảnh browser thật, geometry assertions, kiểm thử tương tác và hạn chế còn tồn tại. Điểm cao của UI mẫu không đồng nghĩa chức năng thật đã hoàn tất. Không cộng điểm cho tính năng chưa kiểm tra; việc chưa xác minh thiết bị thật/assistive technology được trừ ở Responsive/Accessibility/Motion.

## Scorecard — Login: 91/100

| Tiêu chí | Tối đa | Điểm | Nhận xét và bằng chứng |
| --- | ---: | ---: | --- |
| Visual Hierarchy | 15 | 14 | Logo riêng và headline rõ; form 396px, tiêu đề 36/32px, CTA Indigo duy nhất. [Desktop](../screenshots/hybrid-after/login-1366-light.png), [mobile](../screenshots/hybrid-after/login-390-light.png). Chữ eyebrow vẫn nhỏ, giữ tính phụ trợ |
| Layout & Spacing | 15 | 14 | Form/label/input đồng bộ; mobile cuộn dọc tự nhiên khi nội dung dài, không ép vừa bằng cách cắt footer. 5 viewport không tràn ngang |
| Visual Consistency | 15 | 14 | Inter, shared Button/Input/Dialog, Indigo/Violet và focus ring dùng chung. [Dark](../screenshots/hybrid-after/login-1440-dark.png) |
| Usability & Navigation | 15 | 14 | Có show/hide password, Forgot Password/Register entry mở dialog có nhãn preview; không có liên kết chết. Chưa có Auth slice đầy đủ được duyệt |
| Responsive | 10 | 9 | 320/390/768/1366/1440 × light/dark đã kiểm tra. Cuộn form trên màn hình ngắn hợp lệ; thiết bị thật chưa kiểm tra |
| Accessibility | 10 | 8 | Label, described errors, fieldset disabled khi pending, keyboard order và modal focus; axe không phát hiện vi phạm ở 10 tổ hợp + 10 dialog. Password manager/screen reader thật chưa xác minh |
| Interaction States | 10 | 9 | Ready/loading/error, validation và confirmation preview có bằng chứng; không gửi email/đăng ký thật |
| Dark/Light & Motion | 5 | 4 | Theme và reduced-motion theo token; micro-interaction hữu hạn. Chưa đánh giá cảm nhận chuyển động trên nhiều thiết bị |
| Professional Polish | 5 | 5 | Identity nhất quán, không emoji navigation, không CAPTCHA/SSO/call giả, copy rõ ràng. Secondary actions không cạnh tranh CTA |
| **Tổng** | **100** | **91** | **Đạt ngưỡng tự đánh giá ≥85 cho UI mẫu** |

## Scorecard — Private Chat: 89/100

| Tiêu chí | Tối đa | Điểm | Nhận xét và bằng chứng |
| --- | ---: | ---: | --- |
| Visual Hierarchy | 15 | 14 | Phân biệt list, header, bubble, composer; unread badge + current selection có nhãn. Receipt ghi rõ Sample. [Desktop](../screenshots/hybrid-after/private-1440-light.png) |
| Layout & Spacing | 15 | 14 | Tablet 768px chuyển từ thread 400px sang 692px; ≥1280px có optional details 280px. [Tablet](../screenshots/hybrid-after/private-768-light.png), [panel](../screenshots/hybrid-states/private-1366-light-details.png) |
| Visual Consistency | 15 | 14 | Cùng shell/token/composer với Group; outgoing bubble Indigo, incoming neutral, avatar dark có cặp màu riêng |
| Usability & Navigation | 15 | 13 | Back/bottom navigation, loaded-message filter, menu keyboard, jump-to-latest, reply/edit/recall/reaction preview hoạt động. Server search và message actions thật còn chờ tích hợp |
| Responsive | 10 | 9 | 10 tổ hợp hình học/axe; 390×480 và chuỗi dài tại 320px qua. Bàn phím điện thoại thật chưa xác minh |
| Accessibility | 10 | 8 | Menu và dialog quản lý focus, button chính 44px, form errors/connection có text. Axe không phát hiện vi phạm trong phạm vi kiểm tra; chưa có screen-reader audit |
| Interaction States | 10 | 9 | Loading/empty/error/permission/reconnecting, recall confirm, seen/delivered sample; scroll anchor và focus sau gửi/edit được assert. Receipt/reconnect thật chưa được kiểm thử hai tài khoản |
| Dark/Light & Motion | 5 | 4 | Hai theme; panel 220ms, reduced-motion gần như bỏ chuyển động; chưa có đánh giá frame pacing trên thiết bị yếu |
| Professional Polish | 5 | 4 | Composer tự tăng chiều cao, không mất focus khi pending; menu touch có thể thấy, desktop hover/focus. Per-thread draft recovery và full message tooling nằm ngoài checkpoint |
| **Tổng** | **100** | **89** | **Đạt ngưỡng tự đánh giá ≥85 cho UI mẫu** |

## Scorecard — Group Chat: 88/100

| Tiêu chí | Tối đa | Điểm | Nhận xét và bằng chứng |
| --- | ---: | ---: | --- |
| Visual Hierarchy | 15 | 14 | Header tên nhóm/member/role, pinned entry riêng, member roles rõ. Không có channel navigation. [Group](../screenshots/hybrid-after/group-1440-light.png) |
| Layout & Spacing | 15 | 13 | Chat giữ ưu tiên; Details inline desktop, modal/scroll trên tablet/mobile. Nội dung members/actions dài cần cuộn; không cố ép toàn bộ vào một màn hình |
| Visual Consistency | 15 | 14 | Cùng list/bubble/composer/menu với Private; role chips và invitation dialog dùng token chung |
| Usability & Navigation | 15 | 13 | Pin mở nội dung; Invitations/Group actions mở preview hữu ích; selector role giữ đúng một Owner. Không thêm destructive group action chưa được tích hợp |
| Responsive | 10 | 9 | 10 tổ hợp không tràn ngang, header/composer không ra ngoài viewport; details modal dưới 1280px. Thiết bị thật chưa xác minh |
| Accessibility | 10 | 8 | Member text/role không chỉ dựa vào màu; modal/menu keyboard, Escape không đóng nhầm parent panel. Axe không phát hiện vi phạm ở các tổ hợp và role dialog được test |
| Interaction States | 10 | 9 | Loading/empty/error/permission/reconnect; pin, invitation pending, role preview và message tools có test. Permission/invitation thật vẫn NEEDS_VERIFICATION |
| Dark/Light & Motion | 5 | 4 | Dark avatar/muted text/role tokens được sửa và kiểm tra; reduced-motion dùng chung |
| Professional Polish | 5 | 4 | Bỏ strip Hash dễ gợi cảm giác channel; pin và nhóm có cấu trúc rõ, sample labeling nhất quán. Quản trị nhóm đầy đủ vẫn chưa thuộc phạm vi được duyệt |
| **Tổng** | **100** | **88** | **Đạt ngưỡng tự đánh giá ≥85 cho UI mẫu** |

## Vấn đề phát hiện và cách xử lý

| ID | Phát hiện | Cải tiến / kết quả |
| --- | --- | --- |
| DR-01 | Login thiếu Forgot/Register; chỉ có validation mẫu | Thêm entry mở dialog có nhãn local-only, email validation và neutral confirmation; không tạo luồng giả trong production |
| DR-02 | Chữ metadata/section ở baseline quá nhỏ, palette chưa theo đề xuất mới | Tăng type scale, chuẩn hóa #4F46E5/#7C3AED; tăng kích thước form và hit targets |
| DR-03 | 768px dùng rail + list + thread, thread chỉ 400px | Tablet dùng list OR thread + rail; thread 692px, có Back |
| DR-04 | Details chỉ có modal trên mọi kích thước | Thêm inline panel ≥1280px, focus heading, Close/Escape trả focus; nhỏ hơn dùng Radix Dialog |
| DR-05 | Message tools/receipts/reconnect chưa có mẫu rõ ràng | Thêm menu có handler local thật cho reply/edit/reaction/recall; confirmation, sample receipts và reconnect state có nhãn |
| DR-06 | Composer chỉ một dòng có resize handle; focus có thể mất khi disabled pending | Auto-grow tới 144px, sau đó internal scroll; pending readOnly; focus sau send/edit; Enter/Shift+Enter/IME test |
| DR-07 | Incoming không theo cuối nếu đang đọc cuối; không có gợi ý khi ở phía trên | Theo cuối có điều kiện, giữ vị trí đọc, jump-to-latest; tải lịch sử cũ giữ anchor bubble. Jump control nằm trong flow, không đè composer nhiều dòng |
| DR-08 | Group có Hash/topic strip dễ gợi channel, thiếu pin/invite/action preview | Bỏ strip; pin/member/role/invitation preview dựa vào capability có API, không có channels |
| DR-09 | Brightness filter của avatar dark làm contrast sát ngưỡng | Thay bằng explicit dark foreground/background pairs; kiểm tra cả details/members |
| DR-10 | Lỗi thật: Escape trong invitation dialog đóng luôn desktop parent panel | Chỉ xử lý Escape phát sinh trong DOM của panel; dialog portalled tự xử lý. Hai theme desktop regression tests qua |
| DR-11 | Contrast assertion chạy trong animation làm kết quả không ổn định | Chờ finite animation kết thúc trước axe; không vô hiệu contrast rule, không tắt motion toàn bộ suite |
| DR-12 | Test scroll đo wrapper chứa date divider, báo lệch 43px dù bubble anchor không đổi | Đo bubble nội dung mà người dùng đang đọc; assertion ≤3px qua. Không đổi business logic để chiều theo test |
| DR-13 | Project chưa có lint script | Thêm ESLint flat config, React JSX + Hooks checks; lint --max-warnings=0 qua |

Lần chạy đầu của suite mở rộng: 25 PASS / 22 FAIL (19 contrast-animation/old dark-avatar cases, 2 Escape parent-panel, 1 anchor-measurement). Sau sửa, 6 case mục tiêu qua; lần chạy tổng kết **49/49 PASS**. Một unit test mới dùng nhầm matcher Playwright `toBeFocused` trong Vitest; sửa thành `toHaveFocus`, suite cuối **19/19 PASS**. Không coi failure là PASS trước khi sửa và chạy lại.

## Mandatory gates và kết quả

| Gate | Kết quả | Bằng chứng / giới hạn |
| --- | --- | --- |
| Không horizontal overflow | PASS trong phạm vi test | 30 tổ hợp populated + states; header/composer bounds; long URL 320px |
| Không có nút giả không ghi rõ | PASS review mẫu | Tất cả control có hành động thật hoặc mở dialog thông tin có nhãn preview; preview actions không gọi /api hoặc /hubs |
| Light/Dark contrast | PASS trong phạm vi axe | 30 populated + 30 details/recovery surfaces; role dialogs ở 390/1440 cả theme. Không phải full WCAG certification |
| Keyboard/focus | PASS trong phạm vi test | Login tab order; menus bằng Enter; dialog Escape/return focus; panel Close; composer focus |
| Composer/scroll usability | PASS trên browser emulation | Multiline, readOnly pending, IME event, anchor prepend, incoming/new indicator, growing composer/jump không overlap, resized viewport |
| Không overlap/clipping/broken layout | PASS các layout đã đo/xem | Không cắt pane/composer ra ngoài viewport; ellipsis có chủ đích và nội dung scrollable không tính là broken clipping. Thiết bị thật vẫn cần kiểm tra |
| Không runtime error nghiêm trọng | PASS | Matrix theo dõi pageerror và console.error; captures/metrics cũng 0 page errors |
| Có bằng chứng thực tế | PASS | [Screenshot index](screenshot-index.md), before/after metrics, [browser summary](hybrid-test-summary.json) |
| Product Owner approval | **BLOCKED** | Chưa nhận; chỉ Product Owner có quyền duyệt |

## Kết quả kiểm tra và cách tái lập

- `npm.cmd run build`: PASS; vendor PURE-comment warnings có sẵn, không có build failure.
- `npm.cmd run lint`: PASS, zero warnings theo cấu hình.
- `npm.cmd test`: **19 tests PASS**, 3 files.
- `npm.cmd run test:e2e`: **49 tests PASS**. Trong đó 30 case chạy đủ ma trận 3 screen × 5 width × 2 theme; mỗi case còn kiểm tra loading/error và các state phù hợp, details/recovery và axe.
- `npm.cmd run verify:production`: PASS; design routes/fixtures/preview module không xuất hiện trong output theo các marker kiểm tra.
- `npm.cmd audit`: 0 vulnerabilities tại checkpoint.
- `node scripts/capture-review.mjs before|after`: capture có kiểm soát, lưu geometry/errors; không chạy lại `before` sau khi đã đổi giao diện nếu muốn giữ baseline lần review này.
- `node scripts/write-review-summary.mjs`: xuất tổng kết browser đã chạy; không thay thế việc chạy tests.

Chạy từ `frontend`. Trình duyệt: bundled Chromium của Playwright trên Windows; không nhầm với kiểm tra đồng thời Chrome/Edge release thật. Không dùng credentials hoặc dữ liệu user thật để chụp ảnh. 30 ảnh trước và 30 ảnh sau lưu riêng; state/interaction screenshots có index. Visual regression review đối chiếu trước/sau: thay đổi chủ đích ở palette/type/tablet/details/tools, không phát hiện layout regression trong ma trận cuối. Ảnh là candidate evidence, **chưa phải owner-approved frozen pixel baseline**.

Tài liệu nền dùng để cấu hình tooling/textarea: [ESLint flat configuration](https://eslint.org/docs/latest/use/configure/configuration-files), [React textarea](https://react.dev/reference/react-dom/components/textarea). Đây là tham chiếu kỹ thuật, không phải bằng chứng UI đã đạt điểm.

## Còn tồn tại / NEEDS_VERIFICATION

1. Bàn phím thật iOS/Android, safe-area khi có browser chrome, predictive text và IME thực. Test 390×480 chỉ chứng minh reflow khi viewport thu nhỏ; synthetic IME event không thay thế nhập liệu thiết bị thật.
2. Password-manager autofill thật; hiện chỉ xác minh attributes và form interaction. Chưa có native Chrome/Edge release, screen-reader hoặc 200% native browser zoom audit; root font scaling là kiểm tra khác.
3. Real successful auth/session recovery, hai tài khoản SignalR/reconnect/receipts, group role/invitation permissions thật vẫn NEEDS_VERIFICATION. Sample labels không che giấu việc này.
4. Server message search, đầy đủ message actions, per-thread draft recovery, user/group management vẫn là các slice sau gate. Forgot/Register hiện mới có preview entry/dialog; chưa phải Auth slice hoàn chỉnh.
5. Chưa có performance/frame-time audit trên thiết bị yếu hoặc full production load/regression suite.
6. Tooling ESLint 9 được giữ để tương thích peer range của eslint-plugin-react hiện cài; npm báo nhánh này không còn được hỗ trợ. Lint chạy được và audit 0, nhưng nên nâng bộ plugin/toolchain cùng nhau ở checkpoint bảo trì, không dùng force/ignore peer dependencies.

## Đề xuất phê duyệt

Ba mẫu đạt ngưỡng tự đánh giá và các browser gates trong phạm vi đã kiểm thử. **Đề xuất Product Owner xem và quyết định duyệt phần UI/UX mẫu**, hoặc yêu cầu sửa cụ thể. Không tự chuyển gate sang APPROVED. Visual Direction C đã chính thức; việc duyệt màn hình, tích hợp backend và nghiệm thu portfolio là các quyết định riêng. Dừng phát triển module tiếp theo để chờ Product Owner.
