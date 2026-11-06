# Phê duyệt Visual Baseline và Vietnamese-first — 06/11/2026

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. Product Owner đã duyệt phương án C — MeChat Hybrid và cả ba mẫu Login, Private Chat, Group Chat. Các màn hình tiếp theo dùng cùng design system; không hỏi duyệt lại cùng hướng thiết kế. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.

Ngôn ngữ bắt buộc: toàn bộ User/Admin UI dùng tiếng Việt, i18next vi-VN/fallback vi, HTML lang=vi, Intl Asia/Ho_Chi_Minh. Không dịch contracts hoặc nội dung người dùng. Xem [implementation-contract.md](implementation-contract.md). Các trạng thái chờ duyệt/ảnh tiếng Anh bên dưới là **lịch sử trước quyết định này**; gate hiện đã được mở. Việt hóa không mở lại Design Gate.

---

# MeChat Hybrid — UI/UX approval submission

2026-11-06, Asia/Bangkok. **BLOCKED: DESIGN_APPROVAL_REQUIRED**.

Product Owner đã chốt **C — MeChat Hybrid (Modern Messenger + Minimalist SaaS)** là Visual Direction chính thức. Product Owner **chưa duyệt ba màn hình**. Báo cáo này đề xuất xem xét duyệt UI mẫu; không tự thay đổi trạng thái thành APPROVED và không nghiệm thu chức năng/backend/portfolio.

| Màn hình | Tự đánh giá | Ngưỡng | Đề xuất |
| --- | ---: | ---: | --- |
| Login | **91/100** | 85 | Sẵn sàng để Product Owner xem xét |
| Private Chat | **89/100** | 85 | Sẵn sàng để Product Owner xem xét |
| Group Chat | **88/100** | 85 | Sẵn sàng để Product Owner xem xét |

Điểm là tự đánh giá phục vụ phê duyệt, không phải chứng nhận khách quan. [Scorecard riêng theo đủ 9 tiêu chí, nhận xét và bằng chứng](design-review.md).

Mở giao diện đang chạy:

- [Login](http://127.0.0.1:5173/design/login)
- [Private Chat](http://127.0.0.1:5173/design/private)
- [Group Chat](http://127.0.0.1:5173/design/group)

Nếu server đã dừng: chạy `npm.cmd --prefix frontend run dev` từ repository root. Bản mẫu development không cần backend hoặc tài khoản thật để review. Các control thao tác dữ liệu đều có nhãn preview/sample và chỉ thay đổi dữ liệu mẫu cục bộ. Không có channels, call/video hoặc chức năng backend không hỗ trợ. Các luồng thật chưa hoàn thiện không được tính là DONE.

## Screenshot matrix

| Width | Login Light / Dark | Private Light / Dark | Group Light / Dark |
| --- | --- | --- | --- |
| 320 | [Light](../screenshots/hybrid-after/login-320-light.png) · [Dark](../screenshots/hybrid-after/login-320-dark.png) | [Light](../screenshots/hybrid-after/private-320-light.png) · [Dark](../screenshots/hybrid-after/private-320-dark.png) | [Light](../screenshots/hybrid-after/group-320-light.png) · [Dark](../screenshots/hybrid-after/group-320-dark.png) |
| 390 | [Light](../screenshots/hybrid-after/login-390-light.png) · [Dark](../screenshots/hybrid-after/login-390-dark.png) | [Light](../screenshots/hybrid-after/private-390-light.png) · [Dark](../screenshots/hybrid-after/private-390-dark.png) | [Light](../screenshots/hybrid-after/group-390-light.png) · [Dark](../screenshots/hybrid-after/group-390-dark.png) |
| 768 | [Light](../screenshots/hybrid-after/login-768-light.png) · [Dark](../screenshots/hybrid-after/login-768-dark.png) | [Light](../screenshots/hybrid-after/private-768-light.png) · [Dark](../screenshots/hybrid-after/private-768-dark.png) | [Light](../screenshots/hybrid-after/group-768-light.png) · [Dark](../screenshots/hybrid-after/group-768-dark.png) |
| 1366 | [Light](../screenshots/hybrid-after/login-1366-light.png) · [Dark](../screenshots/hybrid-after/login-1366-dark.png) | [Light](../screenshots/hybrid-after/private-1366-light.png) · [Dark](../screenshots/hybrid-after/private-1366-dark.png) | [Light](../screenshots/hybrid-after/group-1366-light.png) · [Dark](../screenshots/hybrid-after/group-1366-dark.png) |
| 1440 | [Light](../screenshots/hybrid-after/login-1440-light.png) · [Dark](../screenshots/hybrid-after/login-1440-dark.png) | [Light](../screenshots/hybrid-after/private-1440-light.png) · [Dark](../screenshots/hybrid-after/private-1440-dark.png) | [Light](../screenshots/hybrid-after/group-1440-light.png) · [Dark](../screenshots/hybrid-after/group-1440-dark.png) |

Lưu **30 Before + 30 After + 159 state/interaction PNGs** từ browser. [Index toàn bộ](screenshot-index.md) gồm loading, empty, error, permission, reconnect, modal/panel, recall, roles và viewport nhập liệu. Selected rows/focus/menu thể hiện active states. Đã đối chiếu trực quan ma trận trước/sau bằng ảnh gốc và inspection sheets; các ảnh gốc không bị sửa. Hình mobile thread đang ở cuối lịch sử là vị trí cuộn có chủ đích; nội dung trước đó vẫn cuộn được. Login dài hơn viewport cuộn dọc tự nhiên.

## Kết quả tại checkpoint

| Kiểm tra | Kết quả |
| --- | --- |
| Build | PASS |
| Lint | PASS — `eslint . --max-warnings=0` |
| Unit/component | **19 PASS**, 3 files |
| Browser suite | **49 PASS**, không skipped; 30 case matrix + states và 19 case bổ sung |
| Contrast/semantics | PASS trong phạm vi axe: 30 populated + 30 recovery/details surfaces, thêm role dialogs 390/1440 cả theme; không phát hiện violations |
| Overflow/geometry | PASS matrix và state assertions; long URL 320px; header/composer bounds, không document overflow |
| Keyboard/focus/scroll | PASS browser emulation — menu Enter, Escape/return focus, modal/panel, gửi/edit, prepend history, jump-to-latest, multiline/IME event, không overlap jump/composer |
| Motion/theme | PASS — normal panel animation ≤250ms, reduced-motion <1ms, theme persistence |
| Runtime errors | PASS — matrix theo dõi pageerror/console.error, capture metrics 0 page errors |
| Production isolation | PASS — marker scan; fixtures/routes/preview module không được ship trong build |
| Dependency audit | 0 vulnerabilities tại lần chạy này |

[Kết quả từng browser case](hybrid-test-summary.json) · [Before geometry](hybrid-before-metrics.json) · [After geometry](hybrid-after-metrics.json).

## Chưa được xác minh

- Bàn phím/IME thực trên iOS/Android, safe-area/browser chrome, native password-manager autofill.
- Chrome/Edge release thật, screen reader, 200% browser zoom và full WCAG conformance. Root font scaling không thay thế native zoom.
- Auth thành công/session recovery, hai tài khoản realtime/receipts/reconnect, group permissions/invitation thật. Lần này không dùng credentials hay dữ liệu thật để test.
- Các slice còn lại, performance/load trên thiết bị yếu và frozen visual regression baseline sau khi Product Owner duyệt.

Các mục này giữ **NEEDS_VERIFICATION**; không báo PASS dựa trên dữ liệu mẫu. Danh sách vấn đề, sửa lỗi, lịch sử test thất bại và giới hạn tooling nằm trong [design-review.md](design-review.md).

**Quyết định còn chờ:** Product Owner duyệt hoặc yêu cầu sửa ba màn hình. Visual Direction C đã được chốt; Design Approval Gate vẫn BLOCKED. Không triển khai hàng loạt module tiếp theo trong khi chờ.
