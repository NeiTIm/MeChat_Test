import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/components/chat/ChatWorkspace.jsx'
await writeFile(path, (await readFile(path, 'utf8')).replace("`${selectedId}-${composerContext?.id || ''}-${composerContext?.mode || ''}`", "`${selectedId}-${composerContext?.mode === 'edit' ? `edit-${composerContext.id}` : 'draft'}`"))
const fixture = 'tests/e2e/messages-client.spec.js'
let test = await readFile(fixture, 'utf8')
test = test.replace("if (path === '/api/conversations')", "if (path === '/api/groups') data = []\n      else if (path.endsWith('/unread')) data = { count: 0, lastReadSequence: 0 }\n      else if (path === '/api/conversations')")
await writeFile(fixture, test)
