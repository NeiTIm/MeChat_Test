"""Read-only backend contract inventory. Does not load config, secrets or private uploads."""
from pathlib import Path
import re
import json

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / 'backend/MeChat.Api'
OUT = ROOT / 'docs/frontend'
OUT.mkdir(parents=True, exist_ok=True)
boundary_file = OUT / 'runtime-boundary-evidence.json'
boundary = json.loads(boundary_file.read_text(encoding='utf-8')) if boundary_file.exists() else {}
boundary_date = boundary.get('date', 'not recorded')
health_verified = any(test.get('title') == 'health through same-origin frontend proxy' and test.get('status') == 'PASS' for test in boundary.get('tests', []))

RESPONSES = {
    'Auth': {'Register': 'GenericResponse', 'ResendVerification': 'GenericResponse', 'ForgotPassword': 'GenericResponse', 'Login': 'AccessResponse', 'Refresh': 'AccessResponse', 'Sessions': 'SessionResponse[]'},
    'Conversations': {'List': 'ConversationDto[]', 'Open': 'ConversationDto', 'Get': 'ConversationDto', 'Send': 'MessageDto', 'History': 'MessagePageDto', 'Search': 'MessagePageDto'},
    'MessageRequests': {'Inbox': 'MessageRequestDto[]', 'UnreadCount': '{count:int}'},
    'Users': {'Me': 'PrivateProfileDto', 'Update': 'PrivateProfileDto', 'Privacy': 'PrivacyDto', 'UpdatePrivacy': 'PrivacyDto', 'Search': 'SearchPageDto', 'Public': 'PublicProfileDto'},
    'Friends': {'List': 'FriendDto[]', 'Requests': 'FriendRequestsDto', 'Blocks': 'BlockedUserDto[]', 'Send': '201 or {status:Accepted}'},
    'Groups': {'List': 'GroupDto[]', 'Create': 'GroupDto', 'Get': 'GroupDto', 'Members': 'GroupMemberDto[]', 'Invitations': 'GroupInvitationDto[]', 'History': 'MessagePageDto', 'Send': 'MessageDto'},
    'AdvancedGroups': {'Pins': 'GroupPinDto[]', 'PinPage': 'GroupPinPageDto'},
    'GroupInviteLinks': {'Create': 'GroupLinkDto', 'List': 'GroupLinkDto[]', 'Resolve': 'GroupLinkPreviewDto', 'Join': '{groupId:Guid}'},
    'Messages': {'Get': 'MessageDto', 'Context': 'MessageContextDto', 'Edit': 'MessageDto', 'EditHistory': '{versions:EditVersionDto[]}', 'Forward': 'MessageDto'},
    'MessageReceipts': {'Receipt': 'MessageReceiptDto'},
    'ConversationStatus': {'Unread': 'UnreadDto'},
    'Unread': {'Get': 'UnreadSummaryDto'},
    'Files': {'Upload': 'StoredFileDto', 'Preview': 'authorized binary stream', 'Download': 'authorized binary + Content-Disposition'},
    'Presence': {'Get': 'PresenceDto'},
    'Notifications': {'List': 'NotificationPageDto', 'Unread': 'NotificationCountDto', 'Settings': 'ConversationSettingsDto'},
    'Reports': {'Create': 'ReportDto', 'Mine': 'ReportDto[]'},
    'Appeals': {'Authenticate': 'AppealAccessDto', 'Create': 'AppealDto', 'Mine': 'AppealDto[]'},
    'Admin': {'Dashboard': 'AdminDashboardDto', 'Users': 'AdminUserDto[]', 'Groups': 'AdminGroupDto[]', 'Reports': 'AdminReportDto[]', 'Case': 'AdminCaseDto', 'EvidenceFile': 'authorized binary', 'Appeals': 'AppealDto[]', 'Audit': 'audit projection[] (see controller)', 'RetentionPreview': 'RetentionPreviewDto', 'FileRetentionPreview': 'FileCleanupPreview', 'RetentionPlan': 'RetentionPlanDto'},
}

RULES = {
    'Auth': 'AuthService: verified + active + unsuspended account; session validation on every authorized request; login lockout; refresh rotation and replay revocation. Refresh/logout require double-submit CSRF.',
    'Users': 'UserService: own mutable profile; username uniqueness + 30-day cooldown; email token verification; public profile projection only; privacy flags respected by downstream services.',
    'Friends': 'FriendService: directional requests; reverse request can accept; block overrides friendship and private access; no self-action; actor must own/respond to the relevant request.',
    'Conversations': 'PrivateChatService + MessageActionService: active nonblocked pair, accepted access or eligible pending request; sequence visibility and per-user hiding; clientMessageId idempotency; search 2–100 chars.',
    'MessageRequests': 'MessageRequestService: receiver inbox; accept/reject/read by receiver; rejection prevents implicit renewal; explicit renewal requires confirm and eligible privacy/access.',
    'Groups': 'GroupService: active eligible members; invitation acceptance bounds visible history by join sequence; owner cannot leave; send idempotency; history enforces visibility intervals.',
    'AdvancedGroups': 'AdvancedGroupService: group roles, not SystemRole; Owner/Admin management; only Owner demotes Admin, transfers or dissolves; Admin can promote Member and remove Member; pins require service permission.',
    'GroupInviteLinks': 'GroupInviteLinkService: authorized management, expiry/revocation and removed-member checks; token only returned on creation; resolve then explicit confirm to join.',
    'Messages': 'MessageActionService: visible accessible message; sender-only edit within 15 minutes; nonblank ≤10000 chars; recall and forwarding reauthorize; one grapheme reaction (1–16 UTF-16 units); deleted/hidden/recalled visibility rules.',
    'MessageReceipts': 'MessageStatusService: visible message and eligible conversation; counts are server facts, never inferred from SignalR connectivity.',
    'ConversationStatus': 'MessageStatusService: eligible reader, monotonic/bounded cursor; acknowledge actual accessible message IDs only; never mark hidden/background content seen.',
    'Unread': 'UnreadSummaryService: server-derived main/request/notification counts filtered to actor-visible conversations.',
    'Files': 'FileService: purpose Message/Avatar; extension and signature checks; image 10 MiB, other file 25 MiB, avatar image 5 MiB; authenticated reads reauthorize attachment visibility.',
    'UserAvatar': 'FileService: actor-owned valid uploaded Avatar purpose; no arbitrary URL or file binding.',
    'Presence': 'PresenceService: privacy-filtered Online nullable and LastSeenAt; null does not mean offline.',
    'Notifications': 'NotificationService: recipient-only read; cursor timestamp + ID tie-break; mute settings require conversation access.',
    'Reports': 'ReportService: accessible User/Group/Message target; evidence captured server-side; actor can read own reports only.',
    'Appeals': 'AppealService: separate scope=appeal identity only for verified active suspended account; token restricted to /api/appeals; never install in normal chat session.',
    'Admin': 'AdminService/RetentionService: DB SystemRole Admin, not just route guard; claim/assignment and review eligibility checked in service; hold requires reason; read-only previews do not execute deletion.',
}

CURRENT = {
    ('Auth', 'Refresh'): ('auth', 'refreshSession', 'AuthProvider/Axios401/SignalR accessToken'),
    ('Auth', 'Register'): ('auth', 'authApi.register', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'VerifyEmail'): ('auth', 'authApi.verifyEmail', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'ResendVerification'): ('auth', 'authApi.resendVerification', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'ForgotPassword'): ('auth', 'authApi.forgotPassword', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'ResetPassword'): ('auth', 'authApi.resetPassword', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'Reactivate'): ('auth', 'authApi.reactivate', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'Login'): ('auth', 'authApi.login', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'Logout'): ('auth', 'authApi.logout', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'LogoutOthers'): ('auth', 'authApi.logoutOthers', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'ChangePassword'): ('auth', 'authApi.changePassword', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'Deactivate'): ('auth', 'authApi.deactivate', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'Sessions'): ('auth', 'authApi.sessions', 'AuthPage/LoginPage/AccountPage'),
    ('Auth', 'RevokeSession'): ('auth', 'authApi.revokeSession', 'AuthPage/LoginPage/AccountPage'),
    ('Users', 'Me'): ('users', 'usersApi.me', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'Update'): ('users', 'usersApi.update', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'Username'): ('users', 'usersApi.changeUsername', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'EmailChange'): ('users', 'usersApi.changeEmail', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'ConfirmEmailChange'): ('users', 'usersApi.confirmEmail', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'Privacy'): ('users', 'usersApi.privacy', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'UpdatePrivacy'): ('users', 'usersApi.updatePrivacy', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'Search'): ('users', 'usersApi.search', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Users', 'Public'): ('users', 'usersApi.profile', 'AccountPage/FriendsPage/UserPicker/WorkspacePage'),
    ('Friends', 'List'): ('friends', 'friendsApi.list', 'FriendsPage real queries/actions'),
    ('Friends', 'Requests'): ('friends', 'friendsApi.requests', 'FriendsPage real queries/actions'),
    ('Friends', 'Send'): ('friends', 'friendsApi.send', 'FriendsPage real queries/actions'),
    ('Friends', 'Accept'): ('friends', 'friendsApi.accept', 'FriendsPage real queries/actions'),
    ('Friends', 'Reject'): ('friends', 'friendsApi.reject', 'FriendsPage real queries/actions'),
    ('Friends', 'Cancel'): ('friends', 'friendsApi.cancel', 'FriendsPage real queries/actions'),
    ('Friends', 'Unfriend'): ('friends', 'friendsApi.unfriend', 'FriendsPage real queries/actions'),
    ('Friends', 'Block'): ('friends', 'friendsApi.block', 'FriendsPage real queries/actions'),
    ('Friends', 'Blocks'): ('friends', 'friendsApi.blocks', 'FriendsPage real queries/actions'),
    ('Friends', 'Unblock'): ('friends', 'friendsApi.unblock', 'FriendsPage real queries/actions'),
    ('Conversations', 'List'): ('conversations', 'conversationsApi.list', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('Conversations', 'Open'): ('conversations', 'conversationsApi.open', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('Conversations', 'Get'): ('conversations', 'conversationsApi.get', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('Conversations', 'Send'): ('conversations', 'conversationsApi.send', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('Conversations', 'History'): ('conversations', 'conversationsApi.history', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('Conversations', 'HideForMe'): ('conversations', 'conversationsApi.hide', 'WorkspacePage/FriendsPage/MessageSearch'),
    ('MessageRequests', 'Inbox'): ('messages', 'requestsApi.list', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('MessageRequests', 'UnreadCount'): ('messages', 'requestsApi.count', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('MessageRequests', 'Accept'): ('messages', 'requestsApi.accept', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('MessageRequests', 'Reject'): ('messages', 'requestsApi.reject', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('MessageRequests', 'Block'): ('messages', 'requestsApi.block', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('MessageRequests', 'MarkRead'): ('messages', 'requestsApi.read', 'WorkspacePage request inbox/RequestActions/useMessageReceipts'),
    ('Groups', 'Hide'): ('groups', 'groupsApi.hide', 'WorkspacePage/GroupPages'),
    ('Groups', 'List'): ('groups', 'groupsApi.list', 'WorkspacePage/GroupPages'),
    ('Groups', 'Create'): ('groups', 'groupsApi.create', 'WorkspacePage/GroupPages'),
    ('Groups', 'Get'): ('groups', 'groupsApi.get', 'WorkspacePage/GroupPages'),
    ('Groups', 'Members'): ('groups', 'groupsApi.members', 'WorkspacePage/GroupPages'),
    ('Groups', 'Invite'): ('groups', 'groupsApi.invite', 'WorkspacePage/GroupPages'),
    ('Groups', 'Invitations'): ('groups', 'groupsApi.invitations', 'WorkspacePage/GroupPages'),
    ('Groups', 'Accept'): ('groups', 'groupsApi.accept', 'WorkspacePage/GroupPages'),
    ('Groups', 'Reject'): ('groups', 'groupsApi.reject', 'WorkspacePage/GroupPages'),
    ('Groups', 'Leave'): ('groups', 'groupsApi.leave', 'WorkspacePage/GroupPages'),
    ('Groups', 'History'): ('groups', 'groupsApi.history', 'WorkspacePage/GroupPages'),
    ('Groups', 'Send'): ('groups', 'groupsApi.send', 'WorkspacePage/GroupPages'),
    ('AdvancedGroups', 'Update'): ('groups', 'groupsApi.update', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Role'): ('groups', 'groupsApi.role', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Remove'): ('groups', 'groupsApi.remove', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Transfer'): ('groups', 'groupsApi.transfer', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Dissolve'): ('groups', 'groupsApi.dissolve', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Pins'): ('groups', 'groupsApi.pins', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'PinPage'): ('groups', 'groupsApi.pinPage', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Pin'): ('groups', 'groupsApi.pin', 'GroupPages/GroupPins/MessageActions'),
    ('AdvancedGroups', 'Unpin'): ('groups', 'groupsApi.unpin', 'GroupPages/GroupPins/MessageActions'),
    ('GroupInviteLinks', 'Create'): ('groups', 'groupsApi.createLink', 'GroupPages create/list/revoke/resolve/explicit join'),
    ('GroupInviteLinks', 'List'): ('groups', 'groupsApi.links', 'GroupPages create/list/revoke/resolve/explicit join'),
    ('GroupInviteLinks', 'Revoke'): ('groups', 'groupsApi.revokeLink', 'GroupPages create/list/revoke/resolve/explicit join'),
    ('GroupInviteLinks', 'Resolve'): ('groups', 'groupsApi.resolveLink', 'GroupPages create/list/revoke/resolve/explicit join'),
    ('GroupInviteLinks', 'Join'): ('groups', 'groupsApi.joinLink', 'GroupPages create/list/revoke/resolve/explicit join'),
    ('Messages', 'Get'): ('messages', 'messagesApi.get', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'Context'): ('messages', 'messagesApi.context', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'Edit'): ('messages', 'messagesApi.edit', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'EditHistory'): ('messages', 'messagesApi.history', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'Recall'): ('messages', 'messagesApi.recall', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'DeleteForMe'): ('messages', 'messagesApi.deleteForMe', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'React'): ('messages', 'messagesApi.react', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'RemoveReaction'): ('messages', 'messagesApi.removeReaction', 'MessageActions/MessageSearch/GroupPins'),
    ('Messages', 'Forward'): ('messages', 'messagesApi.forward', 'MessageActions/MessageSearch/GroupPins'),
    ('MessageReceipts', 'Receipt'): ('messages', 'messagesApi.receipts', 'MessageReceipt server facts'),
    ('ConversationStatus', 'Acknowledge'): ('messages', 'messagesApi.acknowledge', 'useMessageReceipts/useVisibleRead/WorkspacePage'),
    ('ConversationStatus', 'MarkRead'): ('messages', 'messagesApi.read', 'useMessageReceipts/useVisibleRead/WorkspacePage'),
    ('ConversationStatus', 'Unread'): ('messages', 'messagesApi.unread', 'useMessageReceipts/useVisibleRead/WorkspacePage'),
    ('Unread', 'Get'): ('messages', 'messagesApi.summary', 'WorkspacePage server unread list badges'),
    ('Presence', 'Get'): ('messages', 'messagesApi.presence', 'WorkspacePage privacy-aware presence subtitle'),
    ('Files', 'Upload'): ('files', 'filesApi.upload', 'AttachmentDraft/AttachmentPreview/AvatarEditor/Avatar'),
    ('Files', 'Preview'): ('files', 'filesApi.preview', 'AttachmentDraft/AttachmentPreview/AvatarEditor/Avatar'),
    ('Files', 'Download'): ('files', 'filesApi.download', 'AttachmentDraft/AttachmentPreview/AvatarEditor/Avatar'),
    ('UserAvatar', 'Set'): ('files', 'filesApi.avatar', 'AvatarEditor in AccountPage'),
    ('Notifications', 'List'): ('notifications', 'notificationsApi.list', 'NotificationsPage/ConversationNotifications'),
    ('Notifications', 'Unread'): ('notifications', 'notificationsApi.count', 'NotificationsPage/ConversationNotifications'),
    ('Notifications', 'Read'): ('notifications', 'notificationsApi.read', 'NotificationsPage/ConversationNotifications'),
    ('Notifications', 'ReadAll'): ('notifications', 'notificationsApi.readAll', 'NotificationsPage/ConversationNotifications'),
    ('Notifications', 'Settings'): ('notifications', 'notificationsApi.settings', 'NotificationsPage/ConversationNotifications'),
    ('Notifications', 'Mute'): ('notifications', 'notificationsApi.mute', 'NotificationsPage/ConversationNotifications'),
    ('Reports', 'Create'): ('moderation', 'reportsApi.create', 'ReportDialog/ReportForm/ReportsPage'),
    ('Reports', 'Mine'): ('moderation', 'reportsApi.mine', 'ReportDialog/ReportForm/ReportsPage'),
    ('Appeals', 'Authenticate'): ('moderation', 'appealsApi.authenticate', 'AppealsPage isolated scope transport/state'),
    ('Appeals', 'Create'): ('moderation', 'appealsApi.create', 'AppealsPage isolated scope transport/state'),
    ('Appeals', 'Mine'): ('moderation', 'appealsApi.mine', 'AppealsPage isolated scope transport/state'),
    ('Admin', 'Dashboard'): ('admin', 'adminApi.dashboard', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Users'): ('admin', 'adminApi.list', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Groups'): ('admin', 'adminApi.list', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Reports'): ('admin', 'adminApi.list', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Claim'): ('admin', 'adminApi.claim', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Case'): ('admin', 'adminApi.case', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'EvidenceFile'): ('admin', 'adminApi.evidence', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Resolve'): ('admin', 'adminApi.resolve', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Warn'): ('admin', 'adminApi.warn', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Suspend'): ('admin', 'adminApi.suspend', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Unsuspend'): ('admin', 'adminApi.unsuspend', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'DisableGroup'): ('admin', 'adminApi.disableGroup', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Appeals'): ('admin', 'adminApi.list', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'ReviewAppeal'): ('admin', 'adminApi.reviewAppeal', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Audit'): ('admin', 'adminApi.list', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'RetentionPreview'): ('admin', 'adminApi.retention', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'FileRetentionPreview'): ('admin', 'adminApi.fileRetention', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'RetentionPlan'): ('admin', 'adminApi.plan', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Admin', 'Hold'): ('admin', 'adminApi.hold', 'AdminPage/AdminActions: DB role guard + reason/confirmation'),
    ('Conversations', 'NewRequest'): ('messages', 'requestsApi.renew', 'WorkspacePage explicit confirmation'),
    ('Conversations', 'Search'): ('messages', 'messagesApi.search', 'MessageSearch server cursor query'),
}
FEATURES = {'Auth': 'auth', 'Users': 'users', 'Friends': 'friends', 'Conversations': 'conversations', 'MessageRequests': 'message-requests', 'Messages': 'messages', 'MessageReceipts': 'messages', 'ConversationStatus': 'messages', 'Unread': 'messages', 'Groups': 'groups', 'AdvancedGroups': 'groups', 'GroupInviteLinks': 'groups', 'Files': 'files', 'UserAvatar': 'files', 'Presence': 'presence', 'Notifications': 'notifications', 'Reports': 'reports', 'Appeals': 'appeals', 'Admin': 'admin', 'Program': 'diagnostics'}
rows = []
controllers = sorted(API.glob('Features/**/Controllers/*Controller.cs'))
for file in controllers:
    source = file.read_text(encoding='utf-8-sig')
    name = file.stem.removesuffix('Controller')
    prefix = re.search(r'\[Route\("([^"]+)"\)\]', source).group(1)
    class_at = source.index('public sealed class')
    class_auth = '[Authorize]' in source[:class_at]
    matches = list(re.finditer(r'\[Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?\]', source))
    for i, match in enumerate(matches):
        chunk = source[match.end():matches[i + 1].start() if i + 1 < len(matches) else len(source)]
        method = re.search(r'public async Task<IActionResult> (\w+)\((.*?)\)\s*(?:=>|\{)', chunk, re.S)
        if not method:
            raise RuntimeError(f'Unparsed action {file}:{match.start()}')
        action, params = method.groups()
        body = re.findall(r'\b(\w+(?:Request|Create|Dto))\s+(\w+)', params)
        request = ', '.join(x[0] for x in body) or ('multipart file + purpose' if '[FromForm]' in params else 'none')
        query = re.sub(r'\s+', ' ', params).replace('|', '/')
        auth = class_auth or '[Authorize]' in source[source.rfind('}', class_at, match.start()) + 1:match.start()]
        if name == 'Auth': auth = action in ['Logout', 'LogoutOthers', 'ChangePassword', 'Deactivate', 'Sessions', 'RevokeSession']
        if name == 'Appeals': auth = action != 'Authenticate'
        route = '/' + prefix + ('/' + match.group(2) if match.group(2) else '')
        statuses = []
        for call, status in [('NotFound(', '404'), ('BadRequest(', '400'), ('Unauthorized(', '401'), ('Forbid(', '403'), ('Conflict(', '409')]:
            if call in chunk: statuses.append(status)
        statuses += ['400 (binding)', '429', '500']
        if auth and '401' not in statuses: statuses.insert(0, '401')
        response = RESPONSES.get(name, {}).get(action, '204 empty')
        consumer = 'UI action'
        if action in ['Refresh', 'Acknowledge', 'MarkRead', 'Unread', 'UnreadCount'] or name in ['Unread', 'Presence', 'MessageReceipts']: consumer = 'internal flow'
        if name == 'Admin' or (name == 'Appeals' and action == 'Authenticate') or 'Token' in request or 'Link' in name: consumer = 'special flow / UI'
        rows.append(dict(id=f'API-{len(rows)+1:03}', method=match.group(1).upper(), route=route, controller=name, action=action,
                         request=request, parameters=query, response=response, authentication='Bearer' if auth else 'anonymous',
                         authorization='DB Admin' if name == 'Admin' else ('appeal scope or eligible normal identity' if name == 'Appeals' and auth else ('actor + service resource checks' if auth else 'public lifecycle')),
                         errors=', '.join(statuses), consumer=consumer, rules=RULES.get(name, ''), source=str(file.relative_to(ROOT)).replace('\\','/'), line=source[:match.start()].count('\n')+1,
                         implementation='TODO', verification='NEEDS_VERIFICATION'))
rows.append(dict(id=f'API-{len(rows)+1:03}', method='GET', route='/api/health', controller='Program', action='GetHealth', request='none', parameters='none', response='HealthResponse', authentication='anonymous', authorization='public', errors='500', consumer='internal diagnostics', rules='Liveness only; does not validate SQL or API business journeys.', source='backend/MeChat.Api/Program.cs', line=1, implementation='TODO', verification='NEEDS_VERIFICATION'))

for row in rows:
    key = (row['controller'], row['action'])
    row['feature'] = FEATURES[row['controller']]
    row['validation'] = ('multipart purpose + size/extension/signature checks; see FileService' if '[FromForm]' in row['parameters'] else f"{row['request']} DTO annotations + path/query binding; exact declarations and parameters in api-contracts.md")
    current = CURRENT.get(key)
    row['api_function'] = current[1] if current else f"planned: {row['feature']}.{row['action'][0].lower() + row['action'][1:]}"
    row['hook_or_consumer'] = current[2] if current else f"planned {row['feature']} vertical slice"
    row['module'] = f"frontend/src/features/{current[0]}/api.js" if current and key != ('Auth', 'Refresh') else ('frontend/src/lib/http.js' if current else 'not implemented')
    row['test'] = 'client contract/browser tests by feature; authenticated backend NEEDS_VERIFICATION' if current else 'TODO'
    if current: row['implementation'] = 'IMPLEMENTED_CLIENT; NEEDS_VERIFICATION'
    if row['controller'] == 'Auth' and row['action'] in ['Login', 'Refresh', 'Logout']:
        row['test'] = 'tests/session.test.js; tests/e2e/auth.live.spec.js (SKIPPED: no credentials)'
    if key in [('Users', 'Me'), ('Conversations', 'List'), ('Groups', 'List'), ('Admin', 'Dashboard'), ('Auth', 'Login'), ('Auth', 'Refresh')]:
        row['verification'] = f'NEEDS_VERIFICATION; anonymous/invalid boundary evidence dated {boundary_date}'
    if key == ('Program', 'GetHealth'):
        row.update(api_function='Playwright request.get (diagnostics)', hook_or_consumer='boundary.live.spec.js health probe', module='frontend/tests/e2e/boundary.live.spec.js', test=f'boundary.live.spec.js; evidence {boundary_date}', implementation='DONE (diagnostic consumer)' if health_verified else 'IMPLEMENTED_CLIENT; NEEDS_VERIFICATION', verification=f'PASS (200 + exact response through proxy; evidence {boundary_date})' if health_verified else 'NEEDS_VERIFICATION')

(OUT / 'api-inventory.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
lines = ['# Backend HTTP inventory', '', f'Source census: **{len(controllers)} controllers, {len(rows)-1} controller actions + 1 health endpoint = {len(rows)} HTTP endpoints**. Hub transport is counted separately. Generated with `python scripts/frontend-inventory.py`; no configuration, database, uploads or mail are read.', '', 'Static source audit is not runtime acceptance. DTO constraints are catalogued in api-contracts.md; feature rules and service authority below apply to every row of that controller. Common failures include binding ProblemDetails, empty bodies, code objects and plain network failures. Success codes are defined by source (usually 200/204; register 201, email-change 202).', '', '| ID | Method / route | Action / source | Request → response | Auth / authorization | Validation / business rules | Errors | Consumer | Implementation / verification |', '| --- | --- | --- | --- | --- | --- | --- | --- | --- |']
for r in rows:
    lines.append(f"| {r['id']} | {r['method']} `{r['route']}` | [{r['controller']}.{r['action']}](../../{r['source']}#L{r['line']}) | {r['request']} → {r['response']} | {r['authentication']}; {r['authorization']} | {r['validation']}; business rules: {r['controller']} section below | {r['errors']} | {r['consumer']} | {r['implementation']} / {r['verification']} |")
lines += ['', '## Validation and business rules by controller', '']
for key, value in RULES.items(): lines += [f'### {key}', '', value, '']
(OUT / 'api-inventory.md').write_text('\n'.join(lines), encoding='utf-8')

mapping = ['# API to feature / integration matrix', '', 'All source endpoints are included, even when no standalone screen is needed. `planned:` entries are not implemented files. Implemented consumers span the full client feature set; implementation does not imply accepted business integration. Authenticated runtime verification is pending. Health is a real diagnostic consumer in the boundary test.', '', '| API | Feature / use | API function / module | Hook or consumer / interaction | Test | Implementation | Verification |', '| --- | --- | --- | --- | --- | --- | --- |']
for r in rows:
    mapping.append(f"| {r['id']} {r['method']} `{r['route']}` | {r['feature']}; {r['consumer']} | {r['api_function']} — {r['module']} | {r['hook_or_consumer']} | {r['test']} | {r['implementation']} | {r['verification']} |")
(OUT / 'api-to-feature-map.md').write_text('\n'.join(mapping) + '\n', encoding='utf-8')

contracts = ['# API contracts', '', 'Source DTO declarations below retain annotations, nullability and defaults. ASP.NET serializes JSON property names in camelCase. Guid values are strings; long cursors remain numeric (safe integer limits must be revisited if sequences exceed JS safe integers). Dates are UTC; localize only for display. CancellationToken is not sent. Multipart content-type boundaries must be set by the browser.', '', '## HTTP parameters for every endpoint', '', '| ID | Route | Method parameters (query markers, body type, defaults) |', '| --- | --- | --- |']
for r in rows: contracts.append(f"| {r['id']} | `{r['route']}` | `{r['parameters']}` |")
contracts += ['', '## Source DTO declarations', '']
for file in sorted(API.glob('Features/**/DTOs/*.cs')) + [API / 'Common/HealthResponse.cs']:
    source = file.read_text(encoding='utf-8-sig')
    # Records terminate at semicolon, including multiline parameter annotations.
    decls = re.findall(r'public sealed record .*?;', source, re.S)
    contracts += [f'### {file.relative_to(API).as_posix()}', '', '```csharp', '\n'.join(decls), '```', '']
contracts += ['Inline controller DTOs: `NewMessageRequest(bool Confirm)`; `SetAvatarRequest(Guid StoredFileId)`.', '', '## Authentication transport', '', '- AccessResponse has no role. Fetch `/api/users/me` for `systemRole`; server DB authorization remains authoritative.', '- Refresh cookie `webchat_refresh`: HttpOnly, Strict, path `/api/auth`, 30 days. CSRF cookie `webchat_csrf`: readable, Strict, path `/`. Send current cookie in `X-CSRF-Token` on refresh/logout. Cookie rotates after refresh; never cache CSRF value.', '- Same-origin proxy required in dev. Backend CORS currently omits AllowCredentials; do not use a credentialed cross-origin Axios base URL.', '- Refresh is a single in-flight request; coordinate across tabs with Web Locks where available. Never store access/refresh tokens in localStorage. Retry a 401 once after refresh; do not refresh public login failures.', '- SQL transactions, uniqueness, lifecycle and visibility checks remain backend responsibilities. Do not infer permissions from a successful list response.', '', '## Pagination and errors', '', '- Message history and search: `beforeSequence`, `limit` default 30, service clamp 1–50; response items newest first, render ascending; `nextCursor`, `hasMore`.', '- Users: `query`, opaque `cursor`, `limit`; retain server cursor unchanged.', '- Notifications: `before` + `beforeId`, `limit`; use both returned cursor fields.', '- Admin lists: `skip`; retention plan `kind`, `offset`, `limit`; do not invent total counts.', '- 400: validation/invalid action; 401: reauthenticate; 403: permission; 404: unavailable or intentionally hidden; 409: conflict/cooldown; 429: wait/manual retry; 500: generic server failure. Network/timeout/offline are separate. Do not automatically replay mutations.', '- DTO success shapes are static source evidence, not proof that SQL dependencies are configured. Runtime status remains NEEDS_VERIFICATION.']
(OUT / 'api-contracts.md').write_text('\n'.join(contracts), encoding='utf-8')
print(f'{len(controllers)} controllers; {len(rows)} HTTP endpoints. Wrote inventory and DTO contracts.')
