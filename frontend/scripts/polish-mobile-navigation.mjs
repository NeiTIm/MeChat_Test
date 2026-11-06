import { readFile, writeFile } from 'node:fs/promises'
const chat = 'src/components/chat/ChatWorkspace.jsx'
let source = await readFile(chat, 'utf8')
source = source.replace('<span className="mobile-theme"><ThemeToggle /></span>', '<span className="mobile-theme"><ThemeToggle />{!preview && <Button variant="ghost" size="icon" className="icon-button" aria-label={t("chat:sign-out")} disabled={logoutPending} onClick={onLogout}><LogOut /></Button>}</span>')
source = source.replace('{!preview && <Button variant="ghost" onClick={onLogout} disabled={logoutPending}><LogOut size={19} />{t("chat:sign-out")}</Button>}', '')
await writeFile(chat, source)
const layout = 'src/components/layout/ApplicationLayout.jsx'
source = await readFile(layout, 'utf8')
source = source.replace('{actions}<ThemeToggle />', '{actions}<ThemeToggle /><Button className="mobile-session-exit" variant="ghost" size="icon" disabled={logout.isPending} onClick={() => logout.mutate()} aria-label={t(\'auth:signOut\')}><LogOut size={19} /></Button>')
await writeFile(layout, source)
