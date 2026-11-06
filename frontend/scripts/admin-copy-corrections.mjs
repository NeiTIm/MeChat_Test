import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/i18n/locales/vi/admin.json'
const data = JSON.parse(await readFile(path, 'utf8'))
data.cleanupReasons = { ExpiredPendingUpload: 'Tệp chờ đã hết hạn', UnreferencedAvatar: 'Ảnh đại diện không còn được sử dụng', AllMessageReferencesRecalled: 'Mọi tin nhắn tham chiếu đã được thu hồi' }
data.reviewAppealDescription += ' Chấp nhận khiếu nại không tự động gỡ đình chỉ. Việc gỡ đình chỉ là thao tác riêng tại mục Người dùng.'
data.auditActions.SetLegalHold = 'Bảo lưu dữ liệu'
data.auditActions.ReleaseLegalHold = 'Gỡ bảo lưu dữ liệu'
await writeFile(path, JSON.stringify(data, null, 2) + '\n')
