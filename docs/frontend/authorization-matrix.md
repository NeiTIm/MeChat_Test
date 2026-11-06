# Authorization matrix

Controller authorization is only the first boundary. All protected requests validate active session, verified/active user and no active suspension in JwtSessionValidationEvents. Service + repository visibility checks remain authoritative.

| Actor | Allowed UI / operations | Server authority | Frontend requirement |
| --- | --- | --- | --- |
| Anonymous | Login, register, verify/reset/reactivate, appeal authentication, health | AuthController / AppealService | No protected data; no refresh retry of failed login |
| Normal active session | Own profile, privacy, sessions; public search/profile; friends/blocks | UserService, AuthService, FriendService | Guard routes; show own-user actions only |
| Private participant with accepted access | History/send/receipts/settings within visible range | PrivateChatService, MessageStatusService | Block/access state may change; discard cache on permission event |
| Eligible stranger / pending request | Request flow, limited send/read eligibility | PrivateChatService, MessageRequestService | No assumption that opening grants accepted access |
| Request receiver | Accept/reject/block; request read cursor | MessageRequestService | Separate request inbox from main inbox |
| Message sender | Edit in 15-minute window, recall subject to service checks | MessageActionService | Hide ineligible actions; 404 still handled |
| Message viewer | Reply, reaction, context, forwarding, delete-for-me within visibility | MessageActionService | Reauthorize destination and attachments; no guessed receipts |
| Group Member | Read visible history, send if active, member list; leave if not Owner | GroupService | Respect join/leave sequence boundaries |
| Group Admin | Service-permitted profile/invite/pin operations; promote Member; remove Member | AdvancedGroupService | Cannot demote Admin, remove Owner, transfer or dissolve |
| Group Owner | Role management, transfer ownership, dissolve with confirmation | AdvancedGroupService | Cannot leave while Owner; confirm destructive actions |
| System Admin | Dashboard, users/groups/reports/appeals/audit/retention | AdminService.IsAdminAsync checks database SystemRole | Separate layout + role route; do not confuse group Admin with system Admin |
| Claimed-report reviewer | Resolve/report evidence under service assignment rules | AdminService | Claim and assignment must be server-confirmed |
| Suspended eligible user | Separate appeal-only identity | JwtSessionValidationEvents restricts scope=appeal to /api/appeals | Isolate appeal token; never attach to hub or normal API |
| File reader | Only server-authorized avatar/message/evidence reads | FileService / AdminService | Fetch authenticated blob; revoke object URLs; no public uploads URLs |

Source route/action details and statuses for all 123 endpoints are in api-inventory.md / JSON. No feature is runtime-verified by this matrix alone.
