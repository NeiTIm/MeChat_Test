import { readFile, writeFile } from 'node:fs/promises'
async function patch(path, pairs) { let text = await readFile(path, 'utf8'); for (const [from, to] of pairs) { if (!text.includes(from)) throw new Error(`Missing anchor ${path}`); text = text.replace(from, to) } await writeFile(path, text) }
await patch('src/features/auth/LoginPage.jsx', [["<Link to=\"/reactivate\">{t('auth:pages.reactivate.link')}</Link>", "<Link to=\"/reactivate\">{t('auth:pages.reactivate.link')}</Link><Link to=\"/appeals\">{t('appeals:title')}</Link>"]])
await patch('src/features/users/AccountPage.jsx', [
  ["import { NavLink, useNavigate, useParams }", "import { Link, NavLink, useNavigate, useParams }"],
  ["</section></div></ApplicationLayout>", "</section></div><div className=\"account-service-links\"><Link to=\"/reports\">{t('reports:title')}</Link><Link to=\"/appeals\">{t('appeals:title')}</Link>{me.data?.systemRole === 'Admin' && <Link to=\"/admin\">{t('common:admin')}</Link>}</div></ApplicationLayout>"],
])
await patch('src/features/friends/FriendsPage.jsx', [
  ["import { friendsApi } from './api';", "import { friendsApi } from './api';\nimport { ReportDialog } from '@/features/moderation/ReportsPage';"],
  ["{profile.data.bio || t('friends:noBio')}</p></div>", "{profile.data.bio || t('friends:noBio')}</p><ReportDialog targetType=\"User\" targetId={id} /></div>"],
])
await patch('src/features/conversations/WorkspacePage.jsx', [
  ["import { GroupPins }", "import { ReportDialog } from '@/features/moderation/ReportsPage';\nimport { GroupPins }"],
  ["<div className=\"live-thread-actions\">", "<div className=\"live-thread-actions\">{selected && <ReportDialog targetType={area === 'group' ? 'Group' : 'User'} targetId={area === 'group' ? id : selected.otherUserId} />}"]
])
await patch('src/features/messages/MessageActions.jsx', [
  ["import { sanitizeMessageCache }", "import { ReportForm } from '@/features/moderation/ReportsPage';\nimport { sanitizeMessageCache }"],
  ["    <DropdownMenu.Item className=\"menu-danger\" onSelect={() => setOperation('delete')}>", "    <DropdownMenu.Item onSelect={() => setOperation('report')}>{t('reports:report')}</DropdownMenu.Item>\n    <DropdownMenu.Item className=\"menu-danger\" onSelect={() => setOperation('delete')}>"],
  ["t(operation === 'pin' ? 'groups:pin' : `messages:", "t(operation === 'report' ? 'reports:report' : operation === 'pin' ? 'groups:pin' : `messages:"],
  ["t(operation === 'pin' ? 'groups:pinsDescription'", "t(operation === 'report' ? 'reports:reportDescription' : operation === 'pin' ? 'groups:pinsDescription'"],
  ["{operation === 'history' ?", "{operation === 'report' ? <ReportForm targetType=\"Message\" targetId={message.id} /> : operation === 'history' ?"],
])
