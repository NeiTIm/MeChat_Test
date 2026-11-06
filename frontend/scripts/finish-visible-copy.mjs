import { readFile, writeFile } from 'node:fs/promises'
const adminFile = 'src/features/admin/AdminPage.jsx'
let source = await readFile(adminFile, 'utf8')
source = source.replace("import { adminApi } from './api'", "import { adminApi } from './api'\nimport { auditDetails } from './format'")
source = source.replace('<dd>{item.details}</dd>', '<dd>{auditDetails(item)}</dd>')
await writeFile(adminFile, source)
const preview = 'src/features/files/AttachmentPreview.jsx'
source = await readFile(preview, 'utf8')
source = source.replace('function TextPreview({ blob }) {', 'function TextPreview({ blob }) {\n  const { t } = useTranslation()')
source = source.replace('<pre className="text-file-preview">{text}</pre>', '<><pre className="text-file-preview">{text}</pre>{blob.size > 100000 && <p className="field-help" role="status">{t(\'files:partialText\')}</p>}</>')
await writeFile(preview, source)
for (const [namespace, copy] of Object.entries({ admin: { dryRunDetails: 'Vị trí bắt đầu: {{offset}} · Tối đa {{limit}} mục · Không thực hiện xóa.', dryRunSummary: 'Đã xem kế hoạch lưu trữ. Không thực hiện xóa.' }, files: { partialText: 'Đang hiển thị phần đầu tệp. Tải về để xem đầy đủ nội dung.' } })) {
  const path = `src/i18n/locales/vi/${namespace}.json`
  const data = JSON.parse(await readFile(path, 'utf8'))
  await writeFile(path, JSON.stringify({ ...data, ...copy }, null, 2) + '\n')
}
