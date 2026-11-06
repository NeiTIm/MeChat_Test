import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/friends/FriendsPage.jsx'
await writeFile(path, (await readFile(path, 'utf8')).replace("name: fallback.displayName || fallback.username || t('friends:profileTitle')", "name: `${fallback.displayName || fallback.username || t('friends:profileTitle')}${fallback.username ? ` @${fallback.username}` : ''}`"))
const test = 'tests/e2e/friends-client.spec.js'
await writeFile(test, (await readFile(test, 'utf8')).replace("name: 'Xem hồ sơ Nguyễn Hoàng Gia Huy',", "name: 'Xem hồ sơ Nguyễn Hoàng Gia Huy @giahuy',"))
