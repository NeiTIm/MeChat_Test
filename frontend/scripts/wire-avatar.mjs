import { readFile, writeFile } from 'node:fs/promises'
async function patch(path, changes) { let text = await readFile(path, 'utf8'); for (const [from, to] of changes) text = text.replaceAll(from, to); await writeFile(path, text) }
await patch('src/features/users/AccountPage.jsx', [
  ["import { usersApi } from './api'", "import { usersApi } from './api'\nimport { AvatarEditor } from '@/features/files/AvatarEditor'\nimport { filesApi } from '@/features/files/api'"],
  ["successMessage={t('users:profileSaved')} />", "successMessage={t('users:profileSaved')} /><AvatarEditor onSave={filesApi.avatar} />"],
])
await patch('src/features/groups/GroupPages.jsx', [
  ["import { groupsApi } from './api'", "import { groupsApi } from './api'\nimport { AvatarEditor } from '@/features/files/AvatarEditor'"],
  ["successMessage={t('groups:profileSaved')} />", "successMessage={t('groups:profileSaved')} /><AvatarEditor group onSave={async (avatarFileId) => { const current = await groupsApi.get(id); await groupsApi.update(id, { name: current.name, description: current.description, avatarFileId }) }} />"],
])
await patch('src/features/conversations/WorkspacePage.jsx', [
  ['      ...conversation,', '      ...conversation,\n      avatarFileId: conversation.avatarFileId || profile?.avatarFileId,'],
  ['    ...detail.data,', '    ...detail.data,\n    avatarFileId: detail.data.avatarFileId || selectedProfile?.avatarFileId,'],
])
await patch('src/components/chat/ChatWorkspace.jsx', [
  ['name={selected.name}', 'fileId={selected.avatarFileId} name={selected.name}'],
  ['name={conversation.name}', 'fileId={conversation.avatarFileId} name={conversation.name}'],
])
await patch('src/components/layout/ApplicationLayout.jsx', [['<Avatar name={me.data?.displayName', '<Avatar fileId={me.data?.avatarFileId} name={me.data?.displayName']])
await patch('src/features/friends/FriendsPage.jsx', [['<Avatar name={fallback.displayName', '<Avatar fileId={fallback.avatarFileId} name={fallback.displayName'], ['<Avatar name={profile.data.displayName}', '<Avatar fileId={profile.data.avatarFileId} name={profile.data.displayName}']])
