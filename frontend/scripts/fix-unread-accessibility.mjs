import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/notifications/NotificationsPage.jsx'
await writeFile(path, (await readFile(path, 'utf8')).replace('<span className="notification-unread-dot" aria-label={t(\'notifications:newLabel\')} />', '<span className="notification-unread-dot"><span className="sr-only">{t(\'notifications:newLabel\')}</span></span>'))
