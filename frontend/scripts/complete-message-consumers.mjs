import { readFile, writeFile } from 'node:fs/promises'
let path = 'src/features/messages/MessageActions.jsx'
let source = await readFile(path, 'utf8')
source = source.replace("import { ReportForm }", "import { ForwardDestinations } from './ForwardDestinations';\nimport { ReportForm }")
  .replace('destinations = [], ', '')
const from = source.indexOf("{operation === 'forward' && <div className=\"form-field\">")
const to = source.indexOf('\n      <div className="confirmation-actions">', from)
if (from < 0 || to < 0) throw new Error('Forward field anchor missing')
source = source.slice(0, from) + `{operation === 'forward' && <ForwardDestinations id={\`forward-\${message.id}\`} value={destination} onChange={setDestination} disabled={action.isPending} />}\n` + source.slice(to)
await writeFile(path, source)
path = 'src/features/conversations/WorkspacePage.jsx'
source = await readFile(path, 'utf8')
source = source.replace('  const presence = useQuery', "  const threadUnread = useQuery({ queryKey: ['workspace', 'threadUnread', id], queryFn: ({ signal }) => messagesApi.unread(id, signal), enabled: !!id && !requestMode && !history.error });\n  const presence = useQuery")
  .replace('<div className="live-thread-actions">', '<div className="live-thread-actions">{!requestMode && threadUnread.data && <p className="field-help">{t(\'messages:unreadDetail\', { count: threadUnread.data.count })}</p>}')
  .replace(" destinations={conversations.filter((item) => item.status === 'Active')}", '')
await writeFile(path, source)
path = 'src/i18n/locales/vi/messages.json'
const data = JSON.parse(await readFile(path, 'utf8'))
data.unreadDetail = '{{count}} tin nhắn chưa đọc trong cuộc trò chuyện.'
await writeFile(path, JSON.stringify(data, null, 2) + '\n')
path = 'src/i18n/locales/vi/admin.json'
const admin = JSON.parse(await readFile(path, 'utf8'))
admin.auditActions.PlaceRetentionHold = 'Bảo lưu dữ liệu'
admin.auditActions.ReleaseRetentionHold = 'Gỡ bảo lưu dữ liệu'
await writeFile(path, JSON.stringify(admin, null, 2) + '\n')
