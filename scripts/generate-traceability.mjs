import { readFile, writeFile } from 'node:fs/promises'
import { createHash } from 'node:crypto'

// Reviewed implementation inventory, not a coverage or test-result inference engine.
const modules = {
  foundation: ['Data/DatabaseTarget.cs; Program.cs', 'App.jsx; index.css; vite.config.js', '/api/health', 'FoundationTests; backend/frontend build'],
  auth: ['Features/Auth/AuthService.cs; AuthController.cs', 'features/auth/AuthPage.jsx; AccountPage.jsx', '/api/auth/*', 'AuthIntegrationTests (verification/refresh); historical manual auth cases in roadmap'],
  users: ['Features/Users/UserService.cs; UsersController.cs', 'features/users/SettingsPage.jsx; features/friends/FriendsPage.jsx', '/api/users/*', 'UserIntegrationTests'],
  friends: ['Features/Friends/FriendService.cs; FriendsController.cs', 'features/friends/FriendsPage.jsx', '/api/friends/*; /api/blocks/*', 'FriendIntegrationTests (cross-request race, reject/retry, block/unblock)'],
  private: ['Features/Conversations/PrivateChatService.cs', 'features/chat/ChatPage.jsx', '/api/conversations/*; MessageCreated; JoinConversation', 'PrivateChatIntegrationTests.Friend_chat_is_durable_idempotent_and_hidden_from_outsiders; realtime-smoke.mjs'],
  requests: ['Features/Conversations/MessageRequestService.cs', 'features/chat/MessageRequestsPage.jsx', '/api/message-requests/*; MessageCreated', 'PrivateChatIntegrationTests.Stranger_request_is_directional_and_does_not_create_friendship'],
  actions: ['Features/Messages/MessageActionService.cs', 'features/chat/MessageBubble.jsx; MessageContext.jsx; ForwardPicker.jsx; MessageSearch.jsx', '/api/messages/* including context/edit-history/reaction; message search; MessageEdited/Recalled/ReactionChanged', 'PrivateChatIntegrationTests; GroupIntegrationTests (action/privacy assertions); current context/Recall/IDOR assertions PASS in SQL-backed regression'],
  groups: ['Features/Groups/GroupService.cs; AdvancedGroupService.cs; GroupInviteLinkService.cs', 'features/groups/GroupsPage.jsx; GroupControls.jsx', '/api/groups/*; /api/group-invitations/*; /api/group-invite-links/*; MessageCreated', 'GroupIntegrationTests (roles, membership intervals, cap, links, dissolve); group-realtime-smoke.mjs'],
  presence: ['Features/Presence/PresenceService.cs; Realtime/ChatHub.cs', 'features/chat/ChatPage.jsx; features/groups/GroupsPage.jsx', '/api/users/{id}/presence; StartTyping/StopTyping; PresenceChanged/TypingChanged', 'status-realtime-smoke.mjs'],
  status: ['Features/Messages/MessageStatusService.cs; MessageReceiptsController.cs', 'features/chat/MessageReceipt.jsx; ChatPage.jsx; features/groups/GroupsPage.jsx', '/api/messages/{id}/receipts; /api/conversations/{id}/delivery-acks, read-cursor, unread; MessageDelivered/MessageSeen', 'PrivateChatIntegrationTests; GroupIntegrationTests; status-realtime-smoke.mjs; current receipt assertions PASS in SQL-backed regression'],
  notifications: ['Features/Notifications/NotificationService.cs', 'features/notifications/NotificationsPage.jsx; ConversationMuteButton.jsx; chat/group retry handlers', '/api/notifications/*; conversation notification-settings; NotificationCreated', 'PrivateChatIntegrationTests mute/notification assertions; status-realtime-smoke.mjs'],
  files: ['Features/Files/FileService.cs; MessageAttachmentService.cs; FileRetentionService.cs', 'features/chat/FileComposer.jsx; MessageAttachments.jsx; features/groups/GroupControls.jsx', '/api/files; /api/files/{id}/preview, download; /api/admin/retention/files/preview', 'PrivateChatIntegrationTests media/forward/IDOR; GroupIntegrationTests media intervals; FileRetentionTests (fresh unit PASS)'],
  admin: ['Features/Moderation/ReportService.cs; AdminService.cs; AppealService.cs; RetentionPolicy.cs', 'features/moderation/ReportButton.jsx; AdminPage.jsx; AppealPage.jsx', '/api/reports/*; /api/admin/*; /api/appeals/*', 'ModerationIntegrationTests; RetentionPolicyTests'],
}
const assign = new Map()
function use(name, ids) { for (const id of ids) assign.set(id, name) }
function range(a, b) { return Array.from({ length: b - a + 1 }, (_, i) => a + i) }
use('foundation', range(1, 16)); use('auth', [3, 4, 5, 6, 17, ...range(20, 24)])
use('users', [18, 19, 36, 69]); use('friends', [25, 26, 32, 33, 34, 35, 109, 110])
use('private', [27, 46, 49, 100]); use('requests', [7, 28, 29, 30, 31, 101, 106, 118])
use('actions', [9, ...range(37, 45), 111, 113, 114, 117, 124])
use('groups', [11, ...range(47, 48), ...range(51, 66), 98, 99, 102, 104, 105, 115, 116, 121, 125])
use('presence', [12, 67, 68, 69, 70, 71]); use('status', [10, 72, 73, 74])
use('notifications', [13, ...range(75, 80)]); use('files', [8, 50, ...range(87, 96), 107, 112])
use('admin', [...range(81, 86), 97, 103, 108, 119, 120, 122, 123])
const gaps = new Map([
  [1, 'Visual consistency and mobile E2E NOT VERIFIED.'], [2, 'Full MVP/full-scope acceptance remains open.'],
  [6, 'Reset UI/API exist; broader automated auth lifecycle cases still needed.'],
  [9, 'History viewer, authorized reply context, reaction change/remove UI added 2026-10-30; live SQL/browser acceptance pending.'],
  [10, 'Aggregate /unread and per-conversation/navigation badges implemented; recipient ACK and viewing cursor remain required live acceptance.'],
  [14, '15 isolated MOCK UI cases passed across 360/768/1440px before latest feature edits; real API E2E and final UI regression NOT VERIFIED.'],
  [21, 'Historical manual PASS; dedicated automated change-password multi-device regression missing.'],
  [22, 'Historical manual lockout PASS; automated expiry/boundary regression missing.'],
  [23, 'Historical manual deactivate/reactivate PASS; automated lifecycle regression missing.'],
  [28, 'Pending-message integration exists; no daily quota, technical rate gate needs load validation.'],
  [35, 'Rate protection exists on selected operations; complete flood matrix not verified.'],
  [38, 'Edit history viewer added; current permission/Recall regression must run after restore.'],
  [42, 'Authorized/unavailable reply context DTO/UI added; IDOR/Recall assertions compile but not run after restore.'],
  [45, 'Reaction summary, change/remove selector added; SQL/browser regression pending.'],
  [46, 'Private/group hide REST/UI implemented with current hidden-through visibility; SQL acceptance NOT VERIFIED.'],
  [63, 'Sequence-cursor pin pagination and Load more implemented; live SQL paging/permission races NOT VERIFIED.'],
  [75, 'Initial/terminal SignalR retry, REST recovery on reconnect/focus/online/visibility and periodic refresh implemented; real missed-event/multi-device acceptance NOT VERIFIED.'],
  [76, 'Frozen in-memory send attempt retains clientMessageId/files until retry or explicit cancel; online retry implemented. Reload/unmount does not persist an outbox. Live offline E2E NOT VERIFIED.'],
  [77, 'Chat/group navigation includes notification badge; own NotificationRead fanout refreshes other devices. Live acceptance pending.'],
  [88, 'Bounded full-pixel SkiaSharp image decode, strict text, drained bounded ZIP entries and OOXML root/content-type/relationship checks implemented. FileContentValidatorTests PASS 3/3 on 2026-10-31. SQL upload/authorization NOT VERIFIED.'],
  [89, 'Limits implemented; exact 10/25MB HTTP boundary tests pending.'],
  [91, 'Preview/download/zoom link exists; browser evidence missing.'],
  [92, 'Cleanup job added disabled by default; SQL eligibility/crash/retry tests and approved operation pending.'],
  [93, 'Avatar editor and bounded actual PNG/JPEG/WebP decoding implemented; validator PASS 3/3 includes encoded real images, corruption/truncation/format mismatch. Exact 5MB HTTP and SQL bind acceptance NOT VERIFIED.'],
  [94, 'No business quota; upload rate/resource stress coverage incomplete.'],
  [100, 'Group/private hide and authorized resurface implemented; central group history/search/context/file/pin visibility honors hidden-through. SQL tests compile, NOT RUN.'],
  [107, 'All evidence rows protect bytes; eligibility query and cleanup SQL tests pending; no deletion run.'],
  [119, 'Closed-case 90-day eligibility, case-scoped hold/release, audited bounded dry-run and disabled approved purge executor implemented. SQL/retention races NOT VERIFIED; no purge executed.'],
  [120, 'No automatic Owner change; exception transfer denied pending separately approved workflow.'],
  [122, 'Exception transfer denied as frozen policy requires until separate workflow approval.'],
  [123, 'Calendar 12-month policy, authorized hold/release, bounded audited dry-run and disabled approved purge executor implemented. SQL execution NOT VERIFIED; no purge executed.'],
])
const missing = new Set()
const auditText = await readFile('docs/source-audit.json', 'utf8')
const audit = JSON.parse(auditText)
if (Object.keys(audit.requirements).length !== 125) throw new Error('Manual source audit must have 125 rows')
const auditHash = createHash('sha256').update(auditText).digest('hex')
const sourceFresh = (await Promise.all(Object.entries(audit.sourceSnapshot).map(async ([path, expected]) => {
  try { return createHash('sha256').update(await readFile(path)).digest('hex') === expected } catch { return false }
}))).every(Boolean)
const featureReviewed = new Set(Object.entries(audit.requirements).filter(([, entry]) => sourceFresh && entry.featureState === 'Feature Complete'
  && ['backendReview', 'frontendReview', 'apiRealtimeReview', 'authorizationReview'].every(key => entry[key]?.length > 0)).map(([id]) => Number(id)))
let backendState = null
try {
  const state = JSON.parse(await readFile('docs/backend-acceptance-state.json', 'utf8'))
  const manifestHash = createHash('sha256').update(await readFile('docs/backend-acceptance.json')).digest('hex')
  if (sourceFresh && state.sourceFresh && state.sourceAuditHash === auditHash && state.manifestHash === manifestHash) backendState = state
} catch { }
const verified = new Set(sourceFresh ? [16] : [])
let authEvidence = 'Auth lifecycle acceptance NOT VERIFIED.'
try {
  const trx = await readFile('backend/MeChat.Tests/TestResults/backend-qa-final-2026-11-05.trx', 'utf8')
  if (sourceFresh && backendState?.regressionPassed && ['Password_change_preserves_current_device', 'Self_deactivation_revokes_all_sessions', 'Fifth_failed_login_locks_until_exact'].every(name => trx.includes(name))) {
    for (const id of [21, 22, 23]) verified.add(id)
    authEvidence = 'AuthLifecycleIntegrationTests: 3/3 SQL-backed PASS 2026-11-05; change/reset session revocation, exact lockout boundary and deactivate/reactivate.'
  }
} catch { }
let browserEvidence = 'Real browser Seen acceptance NOT VERIFIED.'
try {
  const real = JSON.parse(await readFile('frontend/acceptance-results/real-seen-2026-11-04.json', 'utf8'))
  if (sourceFresh && real.status === 'PASS' && real.type === 'REAL API + SQL + BROWSER E2E' && real.sourceAuditHash === auditHash) {
    verified.add(73); verified.add(118); browserEvidence = 'Real API/SQL/headed Chromium Seen and pending-request privacy PASS: 14 checks, 2026-11-05 Asia/Bangkok (report filename retains UTC run date).'
  }
} catch { }


let groupEvidence = 'Expanded group/actions/media browser acceptance NOT VERIFIED.'
try {
  const real = JSON.parse(await readFile('frontend/acceptance-results/real-group-2026-11-05.json', 'utf8'))
  const trx = await readFile('backend/MeChat.Tests/TestResults/backend-qa-final-2026-11-05.trx', 'utf8')
  const regressionPassed = backendState?.regressionPassed === true
  if (sourceFresh && regressionPassed && trx.includes('Concurrent_acceptance_never_exceeds_one_hundred_members')) verified.add(53)
  if (sourceFresh && regressionPassed && real.status === 'PASS' && real.sourceAuditHash === auditHash) {
    for (const id of [55,65,111]) verified.add(id)
    groupEvidence = 'Real SQL/API/two-browser group/actions/media PASS 17 checks plus 43/43 xUnit: consent, Owner-only confirmed dissolve and forward independence. Positive scenario paced; load gate separate.'
  }
} catch { }

const overrides = new Map([
  [10, ['Features/Messages/UnreadSummaryService.cs; MessageStatusService.cs; UnreadController.cs', 'features/chat/useUnreadSummary.js; UnreadNavigation.jsx; MessageReceipt.jsx', '/api/unread; delivery-acks/read-cursor/receipts; MessageSeen', 'PrivateChatIntegrationTests; GroupIntegrationTests (current SQL assertions PASS); isolated MOCK UI badge case PASS before latest edits']],
  [46, ['Features/Groups/GroupService.cs; Features/Conversations/PrivateChatService.cs', 'features/chat/HideConversationButton.jsx; ChatPage.jsx; features/groups/GroupsPage.jsx', 'DELETE /api/groups/{id}/for-me; DELETE /api/conversations/{id}/for-me; ConversationHidden', 'GroupIntegrationTests hide scenario PASS against SQL; isolated MOCK UI hide/resurface PASS before latest edits']],
  [75, ['Realtime/ChatHub.cs; SessionConnections.cs; StateEvents.cs; REST snapshots in feature controllers', 'services/hubRecovery.js; features/chat/useRecovery.js; ChatPage.jsx; MessageRequestsPage.jsx; features/groups/GroupsPage.jsx', 'JoinConversation; GroupChanged/PermissionsChanged/MessageRequestChanged; authorized REST recovery', 'Node fake-hub retry test PASS 1/1; isolated MOCK UI focus/revocation PASS before latest edits; real status-realtime-smoke reconnect and REST recovery PASS; full transport fault matrix open']],
  [88, ['Features/Files/FileContentValidator.cs; FileService.cs', 'features/chat/FileComposer.jsx; MessageAttachments.jsx', '/api/files; /api/files/{id}/preview, download', 'FileContentValidatorTests included in 43/43 PASS; SQL media/IDOR and real TXT browser upload/download PASS; complete file-type/boundary matrix NOT VERIFIED']],
  [93, ['Features/Files/FileContentValidator.cs; FileService.cs; Features/Users/UsersController.cs', 'features/users/AvatarEditor.jsx; SettingsPage.jsx; features/groups/GroupControls.jsx', '/api/files; /api/users/me/avatar; /api/groups/{id}/profile', 'FileContentValidatorTests PASS 3/3 2026-10-31; avatar HTTP limits/SQL NOT VERIFIED']],
  [119, ['Features/Moderation/RetentionPolicy.cs; RetentionMaintenance.cs; AdminController.cs', 'features/moderation/RetentionControls.jsx; AdminPage.jsx', '/api/admin/retention/preview; /plan; /{kind}/{id}/hold', 'RetentionPolicyTests part of isolated 15/15 PASS; SQL purge/hold/permission scenarios NOT RUN']],
  [123, ['Features/Moderation/RetentionPolicy.cs; RetentionMaintenance.cs; AdminController.cs', 'features/moderation/RetentionControls.jsx; AdminPage.jsx', '/api/admin/retention/preview; /plan; /{kind}/{id}/hold', 'RetentionPolicyTests part of isolated 15/15 PASS; SQL audit purge/hold races NOT RUN']],
])
overrides.set(100, overrides.get(46))
const source = await readFile('docs/spec/12-requirements-traceability.md', 'utf8')
const requirements = [...source.matchAll(/^\| Q(\d{3}) \| (.*?) \|/gm)]
if (requirements.length !== 125) throw new Error(`Expected 125 rules, found ${requirements.length}`)
const rows = requirements.map(([, number, title]) => {
  const id = Number(number); const module = overrides.get(id) ?? modules[assign.get(id)]
  if (!module) throw new Error(`Missing mapping for Q${number}`)
  const status = verified.has(id) ? 'Test Verified' : missing.has(id) ? 'Missing' : 'Implemented / Not Verified'
  const evidence = id === 16 ? '2026-11-04 SQL target/schema and FoundationTests passed in full xUnit regression.'
    : `${module[3]}. Current SQL-backed xUnit and real SignalR evidence is dated 2026-11-04 in roadmap; named suites cover subsets, not whole-rule acceptance. ${[73,118].includes(id) ? browserEvidence : [21,22,23].includes(id) ? authEvidence : [53,55,65,111].includes(id) ? groupEvidence : ''}`
  const note = audit.requirements[number]
  if (!note) throw new Error(`Missing manual audit for Q${number}`)
  const review = `Backend: ${note.backendReview}. UI: ${note.frontendReview}. API/Realtime: ${note.apiRealtimeReview}. Authorization: ${note.authorizationReview}.`
  const next = verified.has(id) ? 'Listed gate verified; project-wide acceptance remains separate.' : 'Feature code reviewed; remaining real per-rule UI/security/stress acceptance is NOT VERIFIED. See roadmap.'
  return `| Q${number} | ${title} | ${module[0]} | ${module[1]} | ${module[2]} | ${evidence} | ${featureReviewed.has(id) ? 'Feature Complete (source review)' : 'Implemented; source review stale'} | ${status} | ${backendState?.requirements[number] ?? 'Not Verified (Backend proof unavailable/stale)'} | ${review} | ${next} |`

})
await writeFile('docs/requirements-traceability.md', `# Q001-Q125 source and acceptance matrix

Reviewed 2026-11-04; acceptance updated 2026-11-05 Asia/Bangkok at E:/Learn/Codex/MeChat. Official SQL target: lpc:.\\MSSQLSERVER01 / MeChat, encrypted Windows Authentication. The previous restore/localhost environment is historical and superseded. Database was rebuilt from the two existing migrations; see database-rebuild-report.md and environment-change-log.md.

Feature Complete means manually inspected Backend, React integration, REST/SignalR and authorization branches with no identified missing functional branch after the recorded repairs. Each row has its own conclusion in docs/source-audit.json. The ${Object.keys(audit.sourceSnapshot).length} source hashes detect subsequent changes; they do not infer completeness from file existence. Snapshot current: ${sourceFresh}. Source-reviewed complete: ${featureReviewed.size}/125. No Phase Completed or Release Ready claim follows from this state.

Test Verified is a separate requirement gate with named evidence; all other rows stay Implemented / Not Verified. Current named requirement gates verified: ${verified.size}/125. Current SQL-backed regression and SignalR results cover named cases, not every possible acceptance branch. ${browserEvidence} ${groupEvidence} Full responsive/Admin/media browser acceptance and operational purge approval/testing remain separate. No real secrets or private uploads are embedded here.

Backend-only QA is separate: ${backendState ? `${backendState.verified}/${backendState.applicable} named Backend gates Verified, ${backendState.partial} Partial, ${backendState.notApplicable} UI-only N/A; overall ${backendState.status}` : 'proof unavailable/stale'}. See backend-acceptance.md for all125 Backend assessments and open tooling/security gates. This never upgrades frontend/project acceptance.

Backend paths are relative to backend/MeChat.Api; frontend paths to frontend/src. Historical evidence in named tests is not silently upgraded by source review.

| Requirement ID | Requirement | Backend implementation | Frontend implementation | API / SignalR | Test evidence | Feature state | Acceptance status | Backend-only gate | Individual source review | Remaining acceptance |
|---|---|---|---|---|---|---|---|---|---|---|
${rows.join('\n')}

Generated from scripts/generate-traceability.mjs plus the manually authored docs/source-audit.json. Re-review changed source before refreshing its snapshot; never infer PASS from counts or file presence.
`, 'utf8')
console.log(`Wrote ${rows.length} rows; Feature Complete ${featureReviewed.size}; Test Verified ${verified.size}; source snapshot current=${sourceFresh}`)
