import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/components/layout/ApplicationLayout.jsx'
let source = await readFile(path, 'utf8')
source = source.replace('{actions}<ThemeToggle />', '{actions}<div className="page-session-tools"><ThemeToggle />')
source = source.replace("<LogOut size={19} /></Button></div></header>", "<LogOut size={19} /></Button></div></div></header>")
await writeFile(path, source)
const test = 'tests/e2e/messages-client.spec.js'
await writeFile(test, (await readFile(test, 'utf8')).replace("path.endsWith('/unread')", "path === '/api/conversations/conversation/unread'"))
