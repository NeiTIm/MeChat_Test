# API contracts

Source DTO declarations below retain annotations, nullability and defaults. ASP.NET serializes JSON property names in camelCase. Guid values are strings; long cursors remain numeric (safe integer limits must be revisited if sequences exceed JS safe integers). Dates are UTC; localize only for display. CancellationToken is not sent. Multipart content-type boundaries must be set by the browser.

## HTTP parameters for every endpoint

| ID | Route | Method parameters (query markers, body type, defaults) |
| --- | --- | --- |
| API-001 | `/api/auth/register` | `RegisterRequest request, CancellationToken ct` |
| API-002 | `/api/auth/verify-email` | `TokenRequest request, CancellationToken ct` |
| API-003 | `/api/auth/resend-verification` | `EmailRequest request, CancellationToken ct` |
| API-004 | `/api/auth/forgot-password` | `EmailRequest request, CancellationToken ct` |
| API-005 | `/api/auth/reset-password` | `ResetPasswordRequest request, CancellationToken ct` |
| API-006 | `/api/auth/reactivate` | `ReactivateRequest request, CancellationToken ct` |
| API-007 | `/api/auth/login` | `LoginRequest request, CancellationToken ct` |
| API-008 | `/api/auth/refresh` | `CancellationToken ct` |
| API-009 | `/api/auth/logout` | `CancellationToken ct` |
| API-010 | `/api/auth/logout-others` | `CancellationToken ct` |
| API-011 | `/api/auth/change-password` | `ChangePasswordRequest request, CancellationToken ct` |
| API-012 | `/api/auth/deactivate` | `DeactivateRequest request, CancellationToken ct` |
| API-013 | `/api/auth/sessions` | `CancellationToken ct` |
| API-014 | `/api/auth/sessions/{id:guid}` | `Guid id, CancellationToken ct` |
| API-015 | `/api/conversations` | `CancellationToken ct` |
| API-016 | `/api/conversations/private` | `CreatePrivateConversationRequest request, CancellationToken ct` |
| API-017 | `/api/conversations/{id:guid}` | `Guid id, CancellationToken ct` |
| API-018 | `/api/conversations/{id:guid}/message-request` | `Guid id, NewMessageRequest request, CancellationToken ct` |
| API-019 | `/api/conversations/{id:guid}/messages` | `Guid id, SendMessageRequest request, CancellationToken ct` |
| API-020 | `/api/conversations/{id:guid}/messages` | `Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-021 | `/api/conversations/{id:guid}/for-me` | `Guid id, CancellationToken ct` |
| API-022 | `/api/conversations/{id:guid}/messages/search` | `Guid id, [FromQuery] string q, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-023 | `/api/message-requests` | `CancellationToken ct` |
| API-024 | `/api/message-requests/unread-count` | `CancellationToken ct` |
| API-025 | `/api/message-requests/{id:guid}/accept` | `Guid id, CancellationToken ct` |
| API-026 | `/api/message-requests/{id:guid}/reject` | `Guid id, CancellationToken ct` |
| API-027 | `/api/message-requests/{id:guid}/block` | `Guid id, CancellationToken ct` |
| API-028 | `/api/message-requests/{id:guid}/read-cursor` | `Guid id, RequestReadCursorDto input, CancellationToken ct` |
| API-029 | `/api/files` | `[FromForm] IFormFile file, [FromForm] string purpose, CancellationToken ct` |
| API-030 | `/api/files/{id:guid}/preview` | `Guid id, CancellationToken ct` |
| API-031 | `/api/files/{id:guid}/download` | `Guid id, CancellationToken ct` |
| API-032 | `/api/users/me/avatar` | `SetAvatarRequest input, CancellationToken ct` |
| API-033 | `/api/friends` | `CancellationToken ct` |
| API-034 | `/api/friends/requests` | `CancellationToken ct` |
| API-035 | `/api/friends/requests` | `FriendRequestCreate input, CancellationToken ct` |
| API-036 | `/api/friends/requests/{id:guid}/accept` | `Guid id, CancellationToken ct` |
| API-037 | `/api/friends/requests/{id:guid}/reject` | `Guid id, CancellationToken ct` |
| API-038 | `/api/friends/requests/{id:guid}/cancel` | `Guid id, CancellationToken ct` |
| API-039 | `/api/friends/{targetUserId:guid}` | `Guid targetUserId, CancellationToken ct` |
| API-040 | `/api/blocks` | `BlockCreate input, CancellationToken ct` |
| API-041 | `/api/blocks` | `CancellationToken ct` |
| API-042 | `/api/blocks/{targetUserId:guid}` | `Guid targetUserId, CancellationToken ct` |
| API-043 | `/api/groups/{id:guid}` | `Guid id, UpdateGroupRequest input, CancellationToken ct` |
| API-044 | `/api/groups/{id:guid}/members/{userId:guid}/role` | `Guid id, Guid userId, ChangeGroupRoleRequest input, CancellationToken ct` |
| API-045 | `/api/groups/{id:guid}/members/{userId:guid}` | `Guid id, Guid userId, CancellationToken ct` |
| API-046 | `/api/groups/{id:guid}/transfer-owner` | `Guid id, TransferGroupOwnerRequest input, CancellationToken ct` |
| API-047 | `/api/groups/{id:guid}/dissolve` | `Guid id, DissolveGroupRequest input, CancellationToken ct` |
| API-048 | `/api/groups/{id:guid}/pins` | `Guid id, CancellationToken ct` |
| API-049 | `/api/groups/{id:guid}/pins/page` | `Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-050 | `/api/groups/{id:guid}/pins` | `Guid id, PinGroupMessageRequest input, CancellationToken ct` |
| API-051 | `/api/groups/{id:guid}/pins/{messageId:guid}` | `Guid id, Guid messageId, CancellationToken ct` |
| API-052 | `/api/groups/{id:guid}/invite-links` | `Guid id, CreateGroupLinkRequest input, CancellationToken ct` |
| API-053 | `/api/groups/{id:guid}/invite-links` | `Guid id, CancellationToken ct` |
| API-054 | `/api/groups/{id:guid}/invite-links/{linkId:guid}` | `Guid id, Guid linkId, CancellationToken ct` |
| API-055 | `/api/group-invite-links/resolve` | `GroupLinkTokenRequest input, CancellationToken ct` |
| API-056 | `/api/group-invite-links/join` | `JoinGroupLinkRequest input, CancellationToken ct` |
| API-057 | `/api/groups/{id:guid}/for-me` | `Guid id, CancellationToken ct` |
| API-058 | `/api/groups` | `CancellationToken ct` |
| API-059 | `/api/groups` | `CreateGroupRequest input, CancellationToken ct` |
| API-060 | `/api/groups/{id:guid}` | `Guid id, CancellationToken ct` |
| API-061 | `/api/groups/{id:guid}/members` | `Guid id, CancellationToken ct` |
| API-062 | `/api/groups/{id:guid}/invitations` | `Guid id, InviteGroupRequest input, CancellationToken ct` |
| API-063 | `/api/group-invitations` | `CancellationToken ct` |
| API-064 | `/api/group-invitations/{id:guid}/accept` | `Guid id, CancellationToken ct` |
| API-065 | `/api/group-invitations/{id:guid}/reject` | `Guid id, CancellationToken ct` |
| API-066 | `/api/groups/{id:guid}/leave` | `Guid id, CancellationToken ct` |
| API-067 | `/api/groups/{id:guid}/messages` | `Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-068 | `/api/groups/{id:guid}/messages` | `Guid id, SendMessageRequest input, CancellationToken ct` |
| API-069 | `/api/conversations/{conversationId:guid}/delivery-acks` | `Guid conversationId, DeliveryAcksRequest input, CancellationToken ct` |
| API-070 | `/api/conversations/{conversationId:guid}/read-cursor` | `Guid conversationId, ReadCursorRequest input, CancellationToken ct` |
| API-071 | `/api/conversations/{conversationId:guid}/unread` | `Guid conversationId, CancellationToken ct` |
| API-072 | `/api/messages/{id:guid}/receipts` | `Guid id, CancellationToken ct` |
| API-073 | `/api/messages/{id:guid}` | `Guid id, CancellationToken ct` |
| API-074 | `/api/messages/{id:guid}/context` | `Guid id, CancellationToken ct` |
| API-075 | `/api/messages/{id:guid}` | `Guid id, EditMessageRequest input, CancellationToken ct` |
| API-076 | `/api/messages/{id:guid}/edit-history` | `Guid id, CancellationToken ct` |
| API-077 | `/api/messages/{id:guid}/recall` | `Guid id, CancellationToken ct` |
| API-078 | `/api/messages/{id:guid}/for-me` | `Guid id, CancellationToken ct` |
| API-079 | `/api/messages/{id:guid}/reaction` | `Guid id, ReactionRequest input, CancellationToken ct` |
| API-080 | `/api/messages/{id:guid}/reaction` | `Guid id, CancellationToken ct` |
| API-081 | `/api/messages/{id:guid}/forward` | `Guid id, ForwardMessageRequest input, CancellationToken ct` |
| API-082 | `/api/unread` | `CancellationToken ct` |
| API-083 | `/api/admin/dashboard` | `CancellationToken ct` |
| API-084 | `/api/admin/users` | `CancellationToken ct, [FromQuery] int skip = 0` |
| API-085 | `/api/admin/groups` | `CancellationToken ct, [FromQuery] int skip = 0` |
| API-086 | `/api/admin/reports` | `CancellationToken ct, [FromQuery] int skip = 0` |
| API-087 | `/api/admin/reports/{id:guid}/claim` | `Guid id, CancellationToken ct` |
| API-088 | `/api/admin/reports/{id:guid}` | `Guid id, CancellationToken ct` |
| API-089 | `/api/admin/reports/{id:guid}/evidence/{evidenceId:guid}/file` | `Guid id, Guid evidenceId, CancellationToken ct` |
| API-090 | `/api/admin/reports/{id:guid}/resolve` | `Guid id, ResolveReportRequest input, CancellationToken ct` |
| API-091 | `/api/admin/users/{id:guid}/warn` | `Guid id, AdminReasonRequest input, CancellationToken ct` |
| API-092 | `/api/admin/users/{id:guid}/suspend` | `Guid id, AdminReasonRequest input, CancellationToken ct` |
| API-093 | `/api/admin/users/{id:guid}/unsuspend` | `Guid id, AdminReasonRequest input, CancellationToken ct` |
| API-094 | `/api/admin/groups/{id:guid}/disable` | `Guid id, AdminReasonRequest input, CancellationToken ct` |
| API-095 | `/api/admin/appeals` | `CancellationToken ct, [FromQuery] int skip = 0` |
| API-096 | `/api/admin/appeals/{id:guid}/review` | `Guid id, ReviewAppealRequest input, CancellationToken ct` |
| API-097 | `/api/admin/audit` | `CancellationToken ct, [FromQuery] int skip = 0` |
| API-098 | `/api/admin/retention/preview` | `CancellationToken ct` |
| API-099 | `/api/admin/retention/files/preview` | `CancellationToken ct` |
| API-100 | `/api/admin/retention/plan` | `[FromQuery] string kind, [FromQuery] int offset = 0, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-101 | `/api/admin/retention/{kind}/{id:guid}/hold` | `string kind, Guid id, RetentionHoldRequest input, CancellationToken ct` |
| API-102 | `/api/appeals/authenticate` | `AppealLoginRequest input, CancellationToken ct` |
| API-103 | `/api/appeals` | `CreateAppealRequest input, CancellationToken ct` |
| API-104 | `/api/appeals/me` | `CancellationToken ct` |
| API-105 | `/api/reports` | `CreateReportRequest input, CancellationToken ct` |
| API-106 | `/api/reports/me` | `CancellationToken ct` |
| API-107 | `/api/notifications` | `[FromQuery] DateTime? before, [FromQuery] int limit = 30, CancellationToken ct = default, [FromQuery] Guid? beforeId = null` |
| API-108 | `/api/notifications/unread-count` | `CancellationToken ct` |
| API-109 | `/api/notifications/{id:guid}/read` | `Guid id, CancellationToken ct` |
| API-110 | `/api/notifications/read-all` | `CancellationToken ct` |
| API-111 | `/api/conversations/{id:guid}/settings` | `Guid id, CancellationToken ct` |
| API-112 | `/api/conversations/{id:guid}/settings` | `Guid id, ConversationSettingsDto input, CancellationToken ct` |
| API-113 | `/api/users/{id:guid}/presence` | `Guid id, CancellationToken ct` |
| API-114 | `/api/users/me` | `CancellationToken ct` |
| API-115 | `/api/users/me` | `UpdateProfileRequest request, CancellationToken ct` |
| API-116 | `/api/users/me/username` | `ChangeUsernameRequest request, CancellationToken ct` |
| API-117 | `/api/users/me/email-change` | `ChangeEmailRequest request, CancellationToken ct` |
| API-118 | `/api/users/me/confirm-email-change` | `ConfirmEmailRequest request, CancellationToken ct` |
| API-119 | `/api/users/me/privacy` | `CancellationToken ct` |
| API-120 | `/api/users/me/privacy` | `PrivacyDto request, CancellationToken ct` |
| API-121 | `/api/users/search` | `[FromQuery] string query, [FromQuery] string? cursor, [FromQuery] int limit = 30, CancellationToken ct = default` |
| API-122 | `/api/users/{id:guid}` | `Guid id, CancellationToken ct` |
| API-123 | `/api/health` | `none` |

## Source DTO declarations

### Features/Auth/DTOs/AuthDtos.cs

```csharp
public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(3), MaxLength(50)] string Username, [Required, MinLength(12)] string Password);
public sealed record TokenRequest([Required] string Token);
public sealed record LoginRequest([Required] string Identifier, [Required] string Password, string? DeviceName);
public sealed record AccessResponse(string AccessToken, DateTime ExpiresAt, Guid UserId, string Username);
public sealed record SessionResponse(Guid Id, string DeviceName, DateTime CreatedAt, DateTime LastActivityAt, bool IsCurrent);
public sealed record GenericResponse(string Message);
public sealed record EmailRequest([Required, EmailAddress] string Email);
public sealed record ResetPasswordRequest([Required] string Token, [Required, MinLength(12)] string NewPassword);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(12)] string NewPassword);
public sealed record ReactivateRequest([Required] string Identifier, [Required] string Password);
public sealed record DeactivateRequest([Required] string Password);
```

### Features/Conversations/DTOs/ConversationDtos.cs

```csharp
public sealed record CreatePrivateConversationRequest(
    [Required]
    Guid TargetUserId);
public sealed record ConversationDto(
    Guid Id,
    string Type,
    string Status,
    Guid OtherUserId,
    long LastSequence,
    DateTime UpdatedAt);
public sealed record SendMessageRequest(
    [Required]
    Guid ClientMessageId,

    [MaxLength(10000)]
    string? Content,

    Guid? ReplyToMessageId = null,

    IReadOnlyList<Guid>? AttachmentIds = null);
public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid ClientMessageId,
    long Sequence,
    string? Content,
    DateTime CreatedAt,
    bool Recalled,

    DateTime? EditedAt = null,

    bool IsForwarded = false,

    Guid? ReplyToMessageId = null,

    IReadOnlyList<AttachmentDto>? Attachments = null);
public sealed record MessagePageDto(
    IReadOnlyList<MessageDto> Items,
    long? NextCursor,
    bool HasMore);
```

### Features/Conversations/DTOs/MessageRequestDtos.cs

```csharp
public sealed record MessageRequestDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid ReceiverUserId,
    string Status,
    long LastSequence,
    long RequestReadSequence,
    DateTime CreatedAt);
public sealed record RequestReadCursorDto(
    long LastReadSequence);
```

### Features/Files/DTOs/AttachmentDto.cs

```csharp
public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);
```

### Features/Files/DTOs/FileDtos.cs

```csharp
public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status);
public sealed record FileRead(
    Stream Content,
    string ContentType,
    string FileName);
```

### Features/Files/DTOs/FileRetentionDtos.cs

```csharp
public sealed record FileRetentionFacts(
    string Status,
    DateTime CreatedAt,
    bool HasAvatar,
    bool HasEvidence,
    int MessageReferences,
    bool HasLiveMessage,
    DateTime? LastRecall);
public sealed record FileCleanupCandidate(
    Guid Id,
    string Reason,
    long SizeBytes);
public sealed record FileCleanupPreview(
    IReadOnlyList<FileCleanupCandidate> Items,
    bool HasMore,
    bool DeletionEnabled);
```

### Features/Friends/DTOs/FriendDtos.cs

```csharp
public sealed record FriendRequestCreate([Required] Guid TargetUserId);
public sealed record FriendRequestDto(Guid Id, Guid SenderUserId, Guid ReceiverUserId, string Status, DateTime CreatedAt, DateTime? RespondedAt);
public sealed record FriendDto(Guid UserId, string Username, string DisplayName, Guid? AvatarFileId, DateTime Since);
public sealed record FriendRequestsDto(IReadOnlyList<FriendRequestDto> Incoming, IReadOnlyList<FriendRequestDto> Outgoing);
public sealed record BlockCreate([Required] Guid TargetUserId);
public sealed record BlockedUserDto(Guid UserId, string Username, string DisplayName, DateTime BlockedAt);
```

### Features/Groups/DTOs/AdvancedGroupDtos.cs

```csharp
public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    Guid? AvatarFileId);
public sealed record ChangeGroupRoleRequest(
    string Role);
public sealed record TransferGroupOwnerRequest(
    Guid NewOwnerUserId);
public sealed record DissolveGroupRequest(
    bool Confirm);
public sealed record GroupPinDto(
    Guid MessageId,
    Guid PinnedByUserId,
    DateTime PinnedAt);
public sealed record GroupPinPageDto(
    IReadOnlyList<GroupPinDto> Items,
    long? NextCursor,
    bool HasMore);
public sealed record PinGroupMessageRequest(
    Guid MessageId);
public sealed record GroupPinRow(
    Guid MessageId,
    long Sequence,
    Guid PinnedByUserId,
    DateTime PinnedAt);
```

### Features/Groups/DTOs/GroupDtos.cs

```csharp
public sealed record CreateGroupRequest([Required, MinLength(1), MaxLength(100)] string Name, [MaxLength(500)] string? Description, IReadOnlyList<Guid>? InviteeUserIds);
public sealed record InviteGroupRequest([Required] Guid InviteeUserId);
public sealed record GroupDto(Guid Id, string Name, string? Description, string Status, Guid CreatedByUserId, long LastSequence, string? MyRole,
    Guid? AvatarFileId = null);
public sealed record GroupMemberDto(Guid UserId, string Role, long JoinSequence, DateTime JoinedAt, string? DisplayName = null, string? Username = null);
public sealed record GroupInvitationDto(Guid Id, Guid ConversationId, Guid InviterUserId, Guid InviteeUserId, string GroupName, DateTime CreatedAt);
```

### Features/Groups/DTOs/GroupInviteLinkDtos.cs

```csharp
public sealed record CreateGroupLinkRequest(
    DateTime? ExpiresAt);
public sealed record GroupLinkDto(
    Guid Id,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string? Token = null);
public sealed record GroupLinkTokenRequest(
    string Token);
public sealed record JoinGroupLinkRequest(
    string Token,
    bool Confirm);
public sealed record GroupLinkPreviewDto(
    Guid GroupId,
    string Name,
    string? Description);
```

### Features/Messages/DTOs/MessageActionDtos.cs

```csharp
public sealed record EditMessageRequest(
    string Content);
public sealed record ReactionRequest(
    string Emoji);
public sealed record ForwardMessageRequest(
    Guid DestinationConversationId,
    Guid ClientMessageId);
public sealed record EditVersionDto(
    int Version,
    string Content,
    DateTime EditedAt);
public sealed record ReplyPreviewDto(
    bool Available,
    string? Content);
public sealed record ReactionCountDto(
    string Emoji,
    int Count);
public sealed record MessageContextDto(
    ReplyPreviewDto? Reply,
    IReadOnlyList<ReactionCountDto> Reactions,
    string? MyReaction);
```

### Features/Messages/DTOs/MessageStatusDtos.cs

```csharp
public sealed record DeliveryAcksRequest(
    IReadOnlyList<Guid> MessageIds);
public sealed record ReadCursorRequest(
    long LastReadSequence);
public sealed record UnreadDto(
    int Count,
    long LastReadSequence);
public sealed record MessageReceiptDto(
    string Status,
    int DeliveredCount,
    int SeenCount);
```

### Features/Messages/DTOs/UnreadSummaryDtos.cs

```csharp
public sealed record ConversationUnreadDto(
    Guid ConversationId,
    string Type,
    int Count,
    long LastReadSequence);
public sealed record UnreadSummaryDto(
    int MainCount,
    int RequestCount,
    IReadOnlyList<ConversationUnreadDto> Conversations,
    int NotificationCount);
```

### Features/Moderation/DTOs/AdminDtos.cs

```csharp
public sealed record AdminActivityDto(
    DateTime Date,
    int NewUsers,
    int Messages);
public sealed record AdminDashboardDto(
    int Users,
    int Messages,
    int Groups,
    int PendingReports,
    IReadOnlyList<AdminActivityDto> Activity);
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string Username,
    string SystemRole,
    bool Suspended,
    DateTime CreatedAt);
public sealed record AdminGroupDto(
    Guid Id,
    string Name,
    string Status,
    int MemberCount);
public sealed record AdminReportDto(
    Guid Id,
    Guid ReporterUserId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    Guid? ReviewedByAdminId,
    DateTime? ReviewedAt);
public sealed record AdminEvidenceDto(
    Guid Id,
    Guid? SourceMessageId,
    string? EvidenceContent,
    Guid? StoredFileId,
    DateTime CapturedAt,
    DateTime? RetainUntil,
    bool LegalHold);
public sealed record AdminCaseDto(
    AdminReportDto Report,
    IReadOnlyList<AdminEvidenceDto> Evidence);
public sealed record ResolveReportRequest(
    string Status,
    string Note);
public sealed record AdminReasonRequest(
    string Reason,
    DateTime? ExpiresAt = null);
public sealed record ReviewAppealRequest(
    string Status,
    string Note);
```

### Features/Moderation/DTOs/AppealDtos.cs

```csharp
public sealed record AppealLoginRequest(
    string Identifier,
    string Password);
public sealed record AppealAccessDto(
    string AccessToken,
    DateTime ExpiresAt);
public sealed record CreateAppealRequest(
    string Reason);
public sealed record AppealDto(
    Guid Id,
    Guid SuspensionId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewNote,
    Guid? UserId = null,
    string? Username = null);
```

### Features/Moderation/DTOs/ReportDtos.cs

```csharp
public sealed record CreateReportRequest(
    string TargetType,
    Guid TargetId,
    string Reason);
public sealed record ReportDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
```

### Features/Moderation/DTOs/RetentionDtos.cs

```csharp
public sealed record RetentionPreviewDto(
    int EvidenceEligible,
    int AuditEligible,
    int EvidenceHeld,
    int AuditHeld);
public sealed record RetentionHoldRequest(
    bool Hold,
    string Reason);
public sealed record RetentionPlanItem(
    Guid Id,
    DateTime EligibleSince);
public sealed record RetentionPlanDto(
    string Kind,
    IReadOnlyList<RetentionPlanItem> Items,
    int? NextOffset,
    bool DryRun = true);
```

### Features/Notifications/DTOs/NotificationDtos.cs

```csharp
public sealed record NotificationDto(
    Guid Id,
    string Type,
    Guid? ReferenceId,
    DateTime CreatedAt,
    DateTime? ReadAt,
    string? Text = null);
public sealed record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    DateTime? NextCursor,
    bool HasMore,
    Guid? NextCursorId = null);
public sealed record NotificationCountDto(
    int Count);
public sealed record ConversationSettingsDto(
    DateTime? MutedUntil);
```

### Features/Presence/DTOs/PresenceDto.cs

```csharp
public sealed record PresenceDto(
    Guid UserId,
    bool? Online,
    DateTime? LastSeenAt);
```

### Features/Users/DTOs/UserDtos.cs

```csharp
public sealed record PrivateProfileDto(Guid Id, string Email, string Username, string DisplayName, string? Bio, Guid? AvatarFileId, string SystemRole, DateTime CreatedAt, DateTime? LastUsernameChangedAt);
public sealed record PublicProfileDto(Guid Id, string Username, string DisplayName, string? Bio, Guid? AvatarFileId);
public sealed record UpdateProfileRequest([Required, MaxLength(100)] string DisplayName, [MaxLength(500)] string? Bio);
public sealed record ChangeUsernameRequest([Required, MinLength(3), MaxLength(50)] string NewUsername);
public sealed record ChangeEmailRequest([Required, EmailAddress] string NewEmail);
public sealed record ConfirmEmailRequest([Required] string Token);
public sealed record PrivacyDto(bool ShowOnline, bool ShowLastSeen, bool AllowStrangerMessages, bool AllowGroupInvites);
public sealed record SearchPageDto(IReadOnlyList<PublicProfileDto> Items, string? NextCursor, bool HasMore);
```

### Common/HealthResponse.cs

```csharp
public sealed record HealthResponse(string Status, string Service);
```

Inline controller DTOs: `NewMessageRequest(bool Confirm)`; `SetAvatarRequest(Guid StoredFileId)`.

## Authentication transport

- AccessResponse has no role. Fetch `/api/users/me` for `systemRole`; server DB authorization remains authoritative.
- Refresh cookie `webchat_refresh`: HttpOnly, Strict, path `/api/auth`, 30 days. CSRF cookie `webchat_csrf`: readable, Strict, path `/`. Send current cookie in `X-CSRF-Token` on refresh/logout. Cookie rotates after refresh; never cache CSRF value.
- Same-origin proxy required in dev. Backend CORS currently omits AllowCredentials; do not use a credentialed cross-origin Axios base URL.
- Refresh is a single in-flight request; coordinate across tabs with Web Locks where available. Never store access/refresh tokens in localStorage. Retry a 401 once after refresh; do not refresh public login failures.
- SQL transactions, uniqueness, lifecycle and visibility checks remain backend responsibilities. Do not infer permissions from a successful list response.

## Pagination and errors

- Message history and search: `beforeSequence`, `limit` default 30, service clamp 1–50; response items newest first, render ascending; `nextCursor`, `hasMore`.
- Users: `query`, opaque `cursor`, `limit`; retain server cursor unchanged.
- Notifications: `before` + `beforeId`, `limit`; use both returned cursor fields.
- Admin lists: `skip`; retention plan `kind`, `offset`, `limit`; do not invent total counts.
- 400: validation/invalid action; 401: reauthenticate; 403: permission; 404: unavailable or intentionally hidden; 409: conflict/cooldown; 429: wait/manual retry; 500: generic server failure. Network/timeout/offline are separate. Do not automatically replay mutations.
- DTO success shapes are static source evidence, not proof that SQL dependencies are configured. Runtime status remains NEEDS_VERIFICATION.