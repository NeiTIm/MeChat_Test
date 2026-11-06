"""Update source consumer mappings after recovering interrupted implementation. No API/DB writes."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
path = root / 'scripts/frontend-inventory.py'
text = path.read_text(encoding='utf-8')
start, end = text.index('CURRENT = {'), text.index('\nFEATURES = ')
controllers = {
 'Auth': ('auth', 'authApi', 'AuthPage/LoginPage/AccountPage', 'Register:register VerifyEmail:verifyEmail ResendVerification:resendVerification ForgotPassword:forgotPassword ResetPassword:resetPassword Reactivate:reactivate Login:login Logout:logout LogoutOthers:logoutOthers ChangePassword:changePassword Deactivate:deactivate Sessions:sessions RevokeSession:revokeSession'),
 'Users': ('users', 'usersApi', 'AccountPage/FriendsPage/UserPicker/WorkspacePage', 'Me:me Update:update Username:changeUsername EmailChange:changeEmail ConfirmEmailChange:confirmEmail Privacy:privacy UpdatePrivacy:updatePrivacy Search:search Public:profile'),
 'Friends': ('friends', 'friendsApi', 'FriendsPage real queries/actions', 'List:list Requests:requests Send:send Accept:accept Reject:reject Cancel:cancel Unfriend:unfriend Block:block Blocks:blocks Unblock:unblock'),
 'Conversations': ('conversations', 'conversationsApi', 'WorkspacePage/FriendsPage/MessageSearch', 'List:list Open:open Get:get Send:send History:history HideForMe:hide'),
 'MessageRequests': ('messages', 'requestsApi', 'WorkspacePage request inbox/RequestActions/useMessageReceipts', 'Inbox:list UnreadCount:count Accept:accept Reject:reject Block:block MarkRead:read'),
 'Groups': ('groups', 'groupsApi', 'WorkspacePage/GroupPages', 'Hide:hide List:list Create:create Get:get Members:members Invite:invite Invitations:invitations Accept:accept Reject:reject Leave:leave History:history Send:send'),
 'AdvancedGroups': ('groups', 'groupsApi', 'GroupPages/GroupPins/MessageActions', 'Update:update Role:role Remove:remove Transfer:transfer Dissolve:dissolve Pins:pins PinPage:pinPage Pin:pin Unpin:unpin'),
 'GroupInviteLinks': ('groups', 'groupsApi', 'GroupPages create/list/revoke/resolve/explicit join', 'Create:createLink List:links Revoke:revokeLink Resolve:resolveLink Join:joinLink'),
 'Messages': ('messages', 'messagesApi', 'MessageActions/MessageSearch/GroupPins', 'Get:get Context:context Edit:edit EditHistory:history Recall:recall DeleteForMe:deleteForMe React:react RemoveReaction:removeReaction Forward:forward'),
 'MessageReceipts': ('messages', 'messagesApi', 'MessageReceipt server facts', 'Receipt:receipts'),
 'ConversationStatus': ('messages', 'messagesApi', 'useMessageReceipts/useVisibleRead/WorkspacePage', 'Acknowledge:acknowledge MarkRead:read Unread:unread'),
 'Unread': ('messages', 'messagesApi', 'WorkspacePage server unread list badges', 'Get:summary'),
 'Presence': ('messages', 'messagesApi', 'WorkspacePage privacy-aware presence subtitle', 'Get:presence'),
 'Files': ('files', 'filesApi', 'AttachmentDraft/AttachmentPreview/AvatarEditor/Avatar', 'Upload:upload Preview:preview Download:download'),
 'UserAvatar': ('files', 'filesApi', 'AvatarEditor in AccountPage', 'Set:avatar'),
 'Notifications': ('notifications', 'notificationsApi', 'NotificationsPage/ConversationNotifications', 'List:list Unread:count Read:read ReadAll:readAll Settings:settings Mute:mute'),
 'Reports': ('moderation', 'reportsApi', 'ReportDialog/ReportForm/ReportsPage', 'Create:create Mine:mine'),
 'Appeals': ('moderation', 'appealsApi', 'AppealsPage isolated scope transport/state', 'Authenticate:authenticate Create:create Mine:mine'),
 'Admin': ('admin', 'adminApi', 'AdminPage/AdminActions: DB role guard + reason/confirmation', 'Dashboard:dashboard Users:list Groups:list Reports:list Claim:claim Case:case EvidenceFile:evidence Resolve:resolve Warn:warn Suspend:suspend Unsuspend:unsuspend DisableGroup:disableGroup Appeals:list ReviewAppeal:reviewAppeal Audit:list RetentionPreview:retention FileRetentionPreview:fileRetention RetentionPlan:plan Hold:hold'),
}
current = {('Auth', 'Refresh'): ('auth', 'refreshSession', 'AuthProvider/Axios401/SignalR accessToken')}
for controller, (feature, api, consumer, pairs) in controllers.items():
 for pair in pairs.split():
  action, method = pair.split(':')
  current[(controller, action)] = (feature, f'{api}.{method}', consumer)
current[('Conversations', 'NewRequest')] = ('messages', 'requestsApi.renew', 'WorkspacePage explicit confirmation')
current[('Conversations', 'Search')] = ('messages', 'messagesApi.search', 'MessageSearch server cursor query')
lines = ['CURRENT = {']
for key, value in current.items(): lines.append(f'    {key!r}: {value!r},')
lines.append('}')
text = text[:start] + '\n'.join(lines) + text[end:]
text = text.replace("row['module'] = f\"frontend/src/features/{row['feature']}/api.js\"", "row['module'] = f\"frontend/src/features/{current[0]}/api.js\"")
text = text.replace("'not created before approval'", "'not implemented'")
text = text.replace("'NEEDS_VERIFICATION (minimal live adapter)'", "'IMPLEMENTED_CLIENT; NEEDS_VERIFICATION'")
text = text.replace("    row['test'] = 'authenticated integration TODO'", "    row['test'] = 'client contract/browser tests by feature; authenticated backend NEEDS_VERIFICATION' if current else 'TODO'")
text = text.replace('The 14 minimal live adapters support the three representative screens; they are not full accepted feature slices.', 'Implemented consumers span the full client feature set; implementation does not imply accepted business integration.')
path.write_text(text, encoding='utf-8')
print(f'Mapped {len(current)} implemented controller consumers; regenerate inventory and inspect unmapped rows.')
