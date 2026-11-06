import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/conversations/WorkspacePage.jsx'
await writeFile(path, (await readFile(path, 'utf8')).replace("{id && <MessageSearch conversationId={id} />}", "{id && <MessageSearch conversationId={id} />}{id && !requestMode && <ConversationNotifications id={id} />}"))
