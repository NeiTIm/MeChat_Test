# Nghiệm thu UI/UX và chức năng

**APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER** — 2026-11-06. Product Owner phê duyệt phương án C — MeChat Hybrid và ba mẫu Login, Private Chat, Group Chat làm Visual Baseline. Không xin duyệt lại cùng hướng; màn hình mới dùng chung design system. Thay đổi đáng kể Visual Direction cần phê duyệt mới. Phê duyệt UI không phải nghiệm thu chức năng/API.

## Điều kiện bắt buộc cho mọi task

- Giữ MeChat Hybrid, tokens chung, Inter, desktop/mobile, Light/Dark và reduced-motion.
- Toàn bộ copy/aria/feedback bằng tiếng Việt; không dịch contracts hoặc nội dung người dùng.
- Loading/empty/error/retry/success/pending/validation thật; không có nút giả hoặc dữ liệu giả trong production.
- Không horizontal overflow/overlap/clipping; keyboard/focus/modal/composer/scroll dùng được; không runtime error nghiêm trọng.
- API/JWT/cookie/CSRF/role/pagination/SignalR/upload đúng source; UI không thay thế authorization Backend.
- Build/lint/unit/component/browser/localization checks có bằng chứng. Không PASS runtime khi chưa chạy Backend thật.

## Bằng chứng và trạng thái

| Phạm vi | Trạng thái hiện tại |
| --- | --- |
| Visual Direction + 3 mẫu | APPROVED bởi Product Owner, 06/11/2026 |
| Recovery build/lint + 33 unit/component | PASS; hồi quy cuối được ghi riêng trong verification-report |
| Groups client | 13/13 PASS chạy lại khi phục hồi |
| Private client | 13/13 PASS trước bổ sung audit; hồi quy mới đang chạy |
| Files/Avatar/Notifications client | 13/13 PASS sau sửa blob/aria |
| Admin/moderation client | 14 checks PASS trong full128; metadata copy focused PASS |
| API source mapping | 122 controller consumers + 1 health, không phải API runtime PASS |
| Backend boundaries | 4 PASS thật, successful auth 1 SKIPPED theo lựa chọn người dùng |
| Production build/isolation/smoke | PASS sau loại DEV navigation strings; build chia chunks |
| Authenticated business / 2-user / real devices | NEEDS_VERIFICATION |

Frontend chưa PORTFOLIO_READY khi chưa đủ business evidence. Không yêu cầu Product Owner duyệt lại cùng Visual Direction. [Điểm tự đánh giá và lịch sử](design-review.md), [checkpoint cũ](history/ui-ux-acceptance-design-checkpoint.md).

## Kết quả checkpoint cuối — 06/11/2026

Full browser128 PASS; final copy2 PASS; secure input1 PASS; hồi quy lại message/media29 PASS sau sửa cache binary/reply. Không cộng các lần chạy có ca trùng thành một full mới. Unit/component33 PASS; build/lint/localization646/API122/production scan/smoke PASS. Real boundaries4 PASS, successful auth1 SKIPPED; authenticated/two-user/native/device/performance NEEDS_VERIFICATION. [Báo cáo chi tiết](verification-report.md). Không nghiệm thu 100% hoặc mở lại Design Gate.
