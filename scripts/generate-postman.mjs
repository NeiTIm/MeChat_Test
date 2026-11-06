import { mkdir, writeFile } from 'node:fs/promises'

const items = []
function request(name, method, path, status, body, actor = 'a', checks = []) {
  const header = actor ? [{ key: 'Authorization', value: `Bearer {{${actor}Token}}` }] : []
  if (body !== undefined) header.push({ key: 'Content-Type', value: 'application/json' })
  const item = { name, request: { method, url: `{{baseUrl}}/api${path}`, header,
    description: `Expected HTTP ${status}. Actor ${actor || 'anonymous'}. Run sequentially with synthetic verified accounts.`,
    ...(body === undefined ? {} : { body: { mode: 'raw', raw: JSON.stringify(body, null, 2) } }) },
    event: [{ listen: 'test', script: { type: 'text/javascript', exec: [
      `pm.test(${JSON.stringify(`HTTP ${status}`)}, () => pm.response.to.have.status(${status}));`,
      ...checks,
    ] } }] }
  items.push(item); return item
}
const save = (variable, field) => `if (pm.response.code < 300) pm.collectionVariables.set('${variable}', pm.response.json().${field});`
for (const actor of ['a', 'b', 'c']) request(`Login ${actor}`, 'POST', '/auth/login', 200,
  { identifier: `{{${actor}Email}}`, password: `{{${actor}Password}}`, deviceName: 'Newman acceptance' }, null,
  [save(`${actor}Token`, 'accessToken'), save(`${actor}Id`, 'userId')])
request('Anonymous profile denied', 'GET', '/users/me', 401, undefined, null)
request('A profile', 'GET', '/users/me', 200, undefined, 'a', ["pm.test('no password leak', () => pm.expect(pm.response.text()).not.to.include('passwordHash'));"])
request('Ordinary admin denied', 'GET', '/admin/dashboard', 403)
request('Unread aggregate', 'GET', '/unread', 200, undefined, 'a', ["pm.test('separate counts', () => { const v = pm.response.json(); for (const key of ['mainCount','requestCount','notificationCount']) pm.expect(v[key]).to.be.a('number'); });"])
request('Ordinary evidence plan denied', 'GET', '/admin/retention/plan?kind=Evidence', 403)
request('Ordinary hold denied', 'PUT', '/admin/retention/Evidence/00000000-0000-0000-0000-000000000001/hold', 403, { hold: true, reason: 'Synthetic authorization test' })
request('Ordinary cleanup preview denied', 'GET', '/admin/retention/files/preview', 403)
request('A invites B', 'POST', '/friends/requests', 201, { targetUserId: '{{bId}}' })
request('B reverse autoaccepts', 'POST', '/friends/requests', 200, { targetUserId: '{{aId}}' }, 'b')
request('Open private chat', 'POST', '/conversations/private', 200, { targetUserId: '{{bId}}' }, 'a', [save('conversationId', 'id')])
request('Send text', 'POST', '/conversations/{{conversationId}}/messages', 200,
  { clientMessageId: '{{sendId}}', content: 'newman original' }, 'a', [save('messageId', 'id'), save('messageSequence', 'sequence')])
request('Retry same ID returns canonical message', 'POST', '/conversations/{{conversationId}}/messages', 200,
  { clientMessageId: '{{sendId}}', content: 'must not replace original' }, 'a', ["pm.test('same canonical message', () => { pm.expect(pm.response.json().id).to.eql(pm.collectionVariables.get('messageId')); pm.expect(pm.response.json().content).to.eql('newman original'); });"])
request('Outsider cannot read chat', 'GET', '/conversations/{{conversationId}}/messages', 404, undefined, 'c')
request('Outsider cannot read context', 'GET', '/messages/{{messageId}}/context', 404, undefined, 'c')
request('Recipient cannot edit sender message', 'PATCH', '/messages/{{messageId}}', 404, { content: 'intrusion' }, 'b')
request('Sender edits text', 'PATCH', '/messages/{{messageId}}', 200, { content: 'newman edited' })
request('Recipient sees edit history', 'GET', '/messages/{{messageId}}/edit-history', 200, undefined, 'b', ["pm.test('two versions', () => pm.expect(pm.response.json().versions.length).to.eql(2));"])
request('Recipient reacts', 'PUT', '/messages/{{messageId}}/reaction', 204, { emoji: '❤️' }, 'b')
request('Context includes reaction', 'GET', '/messages/{{messageId}}/context', 200, undefined, 'a', ["pm.test('reaction summary', () => pm.expect(pm.response.json().reactions[0].count).to.eql(1));"])
request('Recipient removes reaction', 'DELETE', '/messages/{{messageId}}/reaction', 204, undefined, 'b')
request('Recipient replies', 'POST', '/conversations/{{conversationId}}/messages', 200,
  { clientMessageId: '{{replyId}}', content: 'reply survives', replyToMessageId: '{{messageId}}' }, 'b', [save('replyMessageId', 'id')])
request('Forward creates independent copy', 'POST', '/messages/{{messageId}}/forward', 200,
  { destinationConversationId: '{{conversationId}}', clientMessageId: '{{forwardId}}' }, 'b', [save('forwardMessageId', 'id')])
request('Recipient ACK', 'POST', '/conversations/{{conversationId}}/delivery-acks', 204, { messageIds: ['{{messageId}}'] }, 'b')
request('Sender cannot ACK own message', 'POST', '/conversations/{{conversationId}}/delivery-acks', 404, { messageIds: ['{{messageId}}'] })
request('Recall source', 'POST', '/messages/{{messageId}}/recall', 204)
request('Recalled edit history denied', 'GET', '/messages/{{messageId}}/edit-history', 404, undefined, 'b')
request('Reply source unavailable', 'GET', '/messages/{{replyMessageId}}/context', 200, undefined, 'b', ["pm.test('no source leak', () => { pm.expect(pm.response.json().reply.available).to.eql(false); pm.expect(pm.response.json().reply.content).to.eql(null); });"])
const upload = request('Upload private TXT', 'POST', '/files', 200, undefined, 'a', [save('fileId', 'id')])
upload.request.body = { mode: 'formdata', formdata: [{ key: 'purpose', value: 'Message', type: 'text' }, { key: 'file', type: 'file', src: 'docs/postman/fixtures/proof.txt' }] }
request('Outsider pending-file IDOR', 'GET', '/files/{{fileId}}/download', 404, undefined, 'c')
request('Send attachment', 'POST', '/conversations/{{conversationId}}/messages', 200,
  { clientMessageId: '{{mediaId}}', content: 'newman media', attachmentIds: ['{{fileId}}'] }, 'a', [save('mediaMessageId', 'id')])
request('Recipient downloads media', 'GET', '/files/{{fileId}}/download', 200, undefined, 'b')
request('Report media before recall', 'POST', '/reports', 200, { targetType: 'Message', targetId: '{{mediaMessageId}}', reason: 'Synthetic acceptance evidence' }, 'b')
request('Recall media source', 'POST', '/messages/{{mediaMessageId}}/recall', 204)
request('Recipient media revoked', 'GET', '/files/{{fileId}}/download', 404, undefined, 'b')
request('Outsider cannot report inaccessible message', 'POST', '/reports', 404, { targetType: 'Message', targetId: '{{replyMessageId}}', reason: 'Invalid outsider report' }, 'c')
request('Create group', 'POST', '/groups', 200, { name: 'Newman acceptance group', inviteeUserIds: ['{{bId}}'] }, 'a', [save('groupId', 'id')])
request('Member invitation inbox', 'GET', '/group-invitations', 200, undefined, 'b', ["const invite = pm.response.json().find(x => x.groupId === pm.collectionVariables.get('groupId') || x.conversationId === pm.collectionVariables.get('groupId')); if (invite) pm.collectionVariables.set('inviteId', invite.id); pm.test('invitation found', () => pm.expect(invite).to.exist);"])
request('Accept group invitation', 'POST', '/group-invitations/{{inviteId}}/accept', 204, undefined, 'b')
request('Outsider cannot read group', 'GET', '/groups/{{groupId}}/messages', 404, undefined, 'c')
request('Member creates link', 'POST', '/groups/{{groupId}}/invite-links', 200, { expiresAt: null }, 'b', [save('linkToken', 'token')])
request('C disables inbound invitations', 'PATCH', '/users/me/privacy', 200,
  { showOnline: true, showLastSeen: true, allowStrangerMessages: true, allowGroupInvites: false }, 'c')
request('C voluntarily joins link Q125', 'POST', '/group-invite-links/join', 200, { token: '{{linkToken}}', confirm: true }, 'c')
request('Member cannot transfer Owner', 'POST', '/groups/{{groupId}}/transfer-owner', 404, { newOwnerUserId: '{{cId}}' }, 'b')
request('Group send', 'POST', '/groups/{{groupId}}/messages', 200, { clientMessageId: '{{groupSendId}}', content: 'group history' }, 'b', [save('groupMessageId', 'id')])
request('Member pins', 'POST', '/groups/{{groupId}}/pins', 204, { messageId: '{{groupMessageId}}' }, 'c')
request('Pin page', 'GET', '/groups/{{groupId}}/pins/page?limit=1', 200, undefined, 'c')
request('B hides group history', 'DELETE', '/groups/{{groupId}}/for-me', 204, undefined, 'b')
request('C sends after B hide', 'POST', '/groups/{{groupId}}/messages', 200, { clientMessageId: '{{resurfaceId}}', content: 'new visible group message' }, 'c')
request('B sees only post-hide message', 'GET', '/groups/{{groupId}}/messages', 200, undefined, 'b', ["pm.test('old history stays hidden', () => { const rows = pm.response.json().items; pm.expect(rows.some(m => m.id === pm.collectionVariables.get('groupMessageId'))).to.eql(false); pm.expect(rows.some(m => m.content === 'new visible group message')).to.eql(true); });"])
request('Owner dissolves with consent', 'POST', '/groups/{{groupId}}/dissolve', 204, { confirm: true })
request('Dissolved denies recall Q121', 'POST', '/messages/{{groupMessageId}}/recall', 404, undefined, 'b')
request('Dissolved retains permitted history', 'GET', '/groups/{{groupId}}/messages', 200, undefined, 'c', ["pm.test('SQL-reloaded message timestamps are UTC', () => pm.expect(pm.response.json().items.every(row => row.createdAt.endsWith('Z'))).to.eql(true));"])
request('A blocks B', 'POST', '/blocks', 204, { targetUserId: '{{bId}}' })
request('Blocked private send denied', 'POST', '/conversations/{{conversationId}}/messages', 404, { clientMessageId: '{{blockedSendId}}', content: 'must fail' }, 'b')
request('Own reports', 'GET', '/reports/me', 200, undefined, 'b')
const collection = {
  info: { name: 'MeChat acceptance A B C', schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
    description: 'Synthetic fresh verified A/B/C accounts required. Mutates only local MeChat through API; no physical cleanup. Use root working directory and --delay-request 2200 to respect auth anti-abuse. NOT VERIFIED until run; do not reuse accounts after block/dissolve.' },
  event: [{ listen: 'prerequest', script: { type: 'text/javascript', exec: [
    "for (const key of ['sendId','replyId','forwardId','mediaId','groupSendId','blockedSendId','resurfaceId']) if (!pm.collectionVariables.get(key)) pm.collectionVariables.set(key, pm.variables.replaceIn('{{$guid}}'));",
  ] } }], item: items,
}
await mkdir('docs/postman/fixtures', { recursive: true })
await writeFile('docs/postman/fixtures/proof.txt', 'Synthetic MeChat acceptance attachment.\n')
await writeFile('docs/postman/MeChat.Acceptance.postman_collection.json', JSON.stringify(collection, null, 2) + '\n')
const anonymousRoutes = [
  ['GET', '/api/health', 200], ['GET', '/api/users/me', 401], ['GET', '/api/conversations', 401],
  ['GET', '/api/groups', 401], ['POST', '/api/files', 401], ['GET', '/api/notifications', 401],
  ['GET', '/api/admin/dashboard', 401], ['GET', '/api/admin/retention/files/preview', 401],
  ['GET', '/api/reports/me', 401], ['GET', '/api/appeals/me', 401],
  ['POST', '/hubs/chat/negotiate?negotiateVersion=1', 401],
]
await writeFile('docs/postman/MeChat.Anonymous.postman_collection.json', JSON.stringify({
  info: { name: 'MeChat anonymous gates (no SQL)', schema: collection.info.schema,
    description: 'Only health and unauthenticated authorization gates. Does not prove authenticated DB/business behavior.' },
  item: anonymousRoutes.map(([method, path, status]) => ({
    name: `${method} ${path}`, request: { method, url: `{{baseUrl}}${path}` },
    event: [{ listen: 'test', script: { type: 'text/javascript', exec: [`pm.test('HTTP ${status}', () => pm.response.to.have.status(${status}));`] } }],
  })),
}, null, 2) + '\n')
console.log(`Generated ${items.length} sequential acceptance requests. Execution status: NOT VERIFIED.`)
