import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/admin/AdminActions.jsx'
let source = await readFile(path, 'utf8')
source = source.replace('<Button variant={destructive ?', '<Button onClick={() => setActiveLabel(label)} variant={destructive ?')
  .replace('<DialogTitle>{label}</DialogTitle>', '<DialogTitle>{activeLabel}</DialogTitle>')
await writeFile(path, source)
const admin = 'src/features/admin/AdminPage.jsx'
source = await readFile(admin, 'utf8')
source = source.replace('=> <AdminAction label={t(label)}', '=> <AdminAction key={method} label={t(label)}')
await writeFile(admin, source)
const messages = 'src/features/conversations/WorkspacePage.jsx'
source = await readFile(messages, 'utf8')
source = source.replace("enabled: !!id && !requestMode && !history.error", "enabled: !!id && !requestMode && !!history.data && !history.error")
source = source.replace("<div className=\"live-thread-actions\">", "<div className=\"live-thread-actions\">{!requestMode && threadUnread.error && <StatePanel compact error={threadUnread.error} onRetry={threadUnread.refetch} />}")
source = source.replace("import { ConfirmAction }", "import { StatePanel } from '@/components/StatePanel';\nimport { ConfirmAction }")
await writeFile(messages, source)
