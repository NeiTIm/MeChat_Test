using System.ComponentModel.DataAnnotations;

namespace MeChat.Api.Features.Groups;

public sealed record CreateGroupRequest([Required, MinLength(1), MaxLength(100)] string Name, [MaxLength(500)] string? Description, IReadOnlyList<Guid>? InviteeUserIds);
public sealed record InviteGroupRequest([Required] Guid InviteeUserId);
public sealed record GroupDto(Guid Id, string Name, string? Description, string Status, Guid CreatedByUserId, long LastSequence, string? MyRole,
    Guid? AvatarFileId = null);
public sealed record GroupMemberDto(Guid UserId, string Role, long JoinSequence, DateTime JoinedAt, string? DisplayName = null, string? Username = null);
public sealed record GroupInvitationDto(Guid Id, Guid ConversationId, Guid InviterUserId, Guid InviteeUserId, string GroupName, DateTime CreatedAt);
