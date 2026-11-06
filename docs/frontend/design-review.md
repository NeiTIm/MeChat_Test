# MeChat Hybrid — Design Review và triển khai tiếp

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER** — 2026-11-06. Product Owner phê duyệt phương án C — MeChat Hybrid và ba mẫu Login, Private Chat, Group Chat làm Visual Baseline. Không xin duyệt lại cùng hướng; màn hình mới dùng chung design system. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.

## Bằng chứng hiện hành

Đã Việt hóa ba mẫu và mở rộng Auth/account/Friends/chat/Groups/Files/Notifications/moderation/Admin theo cùng baseline. Ảnh Chromium thật: [Login](screenshots/vi/samples/login-390-light.png), [Private](screenshots/vi/integration/private-390-dark.png), [Groups](screenshots/vi/groups/manage-390-light.png), [Admin desktop](screenshots/vi/admin/dashboard-1440-dark.png), [Notifications](screenshots/vi/notifications/list-390-light.png). Ảnh client integration dùng HTTP simulation có nhãn trong report; không phải tài khoản Backend thật. Xem screenshot-index.md.

Vấn đề đã xử lý: bản dịch thiếu/English copy; label logo/profile; tương phản nút destructive trên Dark; blob URL null gây runtime; trạng thái chưa đọc thiếu role; headings mobile; nested-dialog test phải chờ exit/pending; retry idempotency phân biệt reply/attachment; đăng xuất và nhãn navigation mobile. Build split vendor tránh entry >500 kB; DEV routes/fixtures bị loại khỏi production. Bộ hồi quy cuối đã hoàn tất, không dùng kết quả lịch sử để che lỗi mới.

## Scorecards của Visual Baseline đã được duyệt

Giữ nguyên điểm tự đánh giá đã nộp: Login 91, Private 89, Group 88. Đây là đánh giá UI mẫu trước phê duyệt, không phải chứng nhận hoặc điểm nghiệm thu full features. Nhận xét “chưa tích hợp/chưa duyệt” trong các bảng sau mô tả thời điểm checkpoint đó; trạng thái hiện tại ở phần đầu và roadmap. Không tự tăng điểm vì thêm source.

## Scorecard — Login: 91/100

| Tiêu chí | Tối đa | Điểm | Nhận xét và bằng chứng |
| --- | ---: | ---: | --- |
| Visual Hierarchy | 15 | 14 | Logo riêng và headline rõ; form 396px, tiêu đề 36/32px, CTA Indigo duy nhất. [Desktop](screenshots/hybrid-after/login-1366-light.png), [mobile](screenshots/hybrid-after/login-390-light.png). Chữ eyebrow vẫn nhỏ, giữ tính phụ trợ |
| Layout & Spacing | 15 | 14 | Form/label/input đồng bộ; mobile cuộn dọc tự nhiên khi nội dung dài, không ép vừa bằng cách cắt footer. 5 viewport không tràn ngang |
| Visual Consistency | 15 | 14 | Inter, shared Button/Input/Dialog, Indigo/Violet và focus ring dùng chung. [Dark](screenshots/hybrid-after/login-1440-dark.png) |
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
| Visual Hierarchy | 15 | 14 | Phân biệt list, header, bubble, composer; unread badge + current selection có nhãn. Receipt ghi rõ Sample. [Desktop](screenshots/hybrid-after/private-1440-light.png) |
| Layout & Spacing | 15 | 14 | Tablet 768px chuyển từ thread 400px sang 692px; ≥1280px có optional details 280px. [Tablet](screenshots/hybrid-after/private-768-light.png), [panel](screenshots/hybrid-states/private-1366-light-details.png) |
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
| Visual Hierarchy | 15 | 14 | Header tên nhóm/member/role, pinned entry riêng, member roles rõ. Không có channel navigation. [Group](screenshots/hybrid-after/group-1440-light.png) |
| Layout & Spacing | 15 | 13 | Chat giữ ưu tiên; Details inline desktop, modal/scroll trên tablet/mobile. Nội dung members/actions dài cần cuộn; không cố ép toàn bộ vào một màn hình |
| Visual Consistency | 15 | 14 | Cùng list/bubble/composer/menu với Private; role chips và invitation dialog dùng token chung |
| Usability & Navigation | 15 | 13 | Pin mở nội dung; Invitations/Group actions mở preview hữu ích; selector role giữ đúng một Owner. Không thêm destructive group action chưa được tích hợp |
| Responsive | 10 | 9 | 10 tổ hợp không tràn ngang, header/composer không ra ngoài viewport; details modal dưới 1280px. Thiết bị thật chưa xác minh |
| Accessibility | 10 | 8 | Member text/role không chỉ dựa vào màu; modal/menu keyboard, Escape không đóng nhầm parent panel. Axe không phát hiện vi phạm ở các tổ hợp và role dialog được test |
| Interaction States | 10 | 9 | Loading/empty/error/permission/reconnect; pin, invitation pending, role preview và message tools có test. Permission/invitation thật vẫn NEEDS_VERIFICATION |
| Dark/Light & Motion | 5 | 4 | Dark avatar/muted text/role tokens được sửa và kiểm tra; reduced-motion dùng chung |
| Professional Polish | 5 | 4 | Bỏ strip Hash dễ gợi cảm giác channel; pin và nhóm có cấu trúc rõ, sample labeling nhất quán. Quản trị nhóm đầy đủ vẫn chưa thuộc phạm vi được duyệt |
| **Tổng** | **100** | **88** | **Đạt ngưỡng tự đánh giá ≥85 cho UI mẫu** |


## Hạn chế và đề xuất

Visual Baseline đã được duyệt; không cần mở lại gate. Successful auth/business/two-user/admin/file authorization thật, native browser/mobile keyboard/screen reader và full performance còn NEEDS_VERIFICATION. Có thể xem các luồng client; chưa nghiệm thu portfolio toàn bộ. [Bản review checkpoint đầy đủ](history/design-review-design-checkpoint.md).

## Kết quả checkpoint cuối — 06/11/2026

Full browser128 PASS; final copy2 PASS; secure input1 PASS; hồi quy lại message/media29 PASS sau sửa cache binary/reply. Không cộng các lần chạy có ca trùng thành một full mới. Unit/component33 PASS; build/lint/localization646/API122/production scan/smoke PASS. Real boundaries4 PASS, successful auth1 SKIPPED; authenticated/two-user/native/device/performance NEEDS_VERIFICATION. [Báo cáo chi tiết](verification-report.md). Không nghiệm thu 100% hoặc mở lại Design Gate.
