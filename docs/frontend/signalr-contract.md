# SignalR contract

Authenticated hub: `/hubs/chat`. `CloseOnAuthenticationExpiration = true`. JWT `access_token` query accepted only for hub path. Server delivers most events to authenticated user IDs, not just joined rooms. Joining a room does not replace HTTP authorization.

| Hub method | Argument | Permission |
| --- | --- | --- |
| JoinConversation | conversationId GUID | Active session + private read/request read OR active group sender |
| LeaveConversation | conversationId GUID | Remove current connection from room |
| StartTyping | conversationId GUID | PresenceService checks session + conversation and privacy |
| StopTyping | conversationId GUID | Same eligibility |

Source census: **18 distinct server event names** (including StateEvents.GroupChangedAsync). Payloads are invalidation hints, not authoritative message data.

| Event | Payload fields / source | Cache consumer / recovery | Runtime status |
| --- | --- | --- | --- |
| MessageCreated | eventId, conversationId, messageId, sequence — private/group/action services | Message history + inbox + unread | NEEDS_VERIFICATION |
| MessageEdited | eventId, conversationId, messageId — MessageActionService.PublishAsync | History + context + edit history | NEEDS_VERIFICATION |
| MessageRecalled | Same ID hint | Remove stale content, reauthorize history/file/context | NEEDS_VERIFICATION |
| MessageDeletedForMe | messageId (no eventId/conversationId) | Discard actor message caches; refetch history | NEEDS_VERIFICATION |
| MessageReactionChanged | MessageActionService.PublishAsync ID hint | Context/reactions | NEEDS_VERIFICATION |
| MessageDelivered | eventId, conversationId, messageId, recipientUserId, deliveredAt | Refetch receipt | NEEDS_VERIFICATION |
| MessageSeen | eventId, conversationId, userId, lastReadSequence | Refetch receipts/unread | NEEDS_VERIFICATION |
| ConversationHidden | conversationId; eventId not uniform | Remove/refetch inbox; no content replay | NEEDS_VERIFICATION |
| TypingChanged | eventId, conversationId, actorId, isTyping, expiresAt | Ephemeral typing expires after server time; clear on disconnect | NEEDS_VERIFICATION |
| PresenceChanged | PresenceDto userId, online nullable, lastSeenAt | Refetch privacy-filtered presence | NEEDS_VERIFICATION |
| FriendshipChanged | eventId | Friends, requests, eligible private data | NEEDS_VERIFICATION |
| PermissionsChanged | eventId | Cancel/remove protected resource caches then HTTP reauthorize | NEEDS_VERIFICATION |
| MessageRequestChanged | eventId + conversation/request hints (service variants) | Inbox, requests, unread | NEEDS_VERIFICATION |
| GroupChanged | eventId, conversationId | Cancel/remove group/member/message data then reauthorize | NEEDS_VERIFICATION |
| GroupInvitationChanged | eventId + group invitation hints | Invitations and group list | NEEDS_VERIFICATION |
| NotificationCreated | notificationId, type, referenceId; eventId not uniform | Notifications + unread, never fabricate toast content | NEEDS_VERIFICATION |
| NotificationRead | eventId | Notifications + unread | NEEDS_VERIFICATION |
| SessionRevoked | eventId — AdminService suspension | Clear identity/cache; abort hub. Other revocations may only disconnect | NEEDS_VERIFICATION |

Provider design: one connection per app instance, token factory calls shared session manager, automatic reconnect plus retry for initial-start failure, reconnect/focus/online HTTP resync, periodic foreground reconciliation, bounded eventId dedupe; idempotent invalidation when eventId absent. Register handlers once and clean up on identity changes. Multi-tab logout broadcasts no credentials. Web Locks serialize cookie rotations across tabs where supported; no token sharing or storage. Revocation/membership changes evict potentially stale content before refetch. Do not mark messages delivered/seen from connectivity. Visual approval is granted. Full event behavior still needs two authorized test accounts; do not request design approval again.

Recovery evidence 06/11/2026: realtime.test.jsx verifies hint dedupe, cache reset on PermissionsChanged, typing TTL/invocation whitelist, revoked-session cleanup and ignored late events. These are client unit tests, not actual authenticated transport acceptance. All event runtime rows remain NEEDS_VERIFICATION.

Recovery evidence 06/11/2026: realtime.test.jsx verifies hint dedupe, cache reset on PermissionsChanged, typing TTL/invocation whitelist, revoked-session cleanup and ignored late events. These are client unit tests, not actual authenticated transport acceptance. All event runtime rows remain NEEDS_VERIFICATION.
