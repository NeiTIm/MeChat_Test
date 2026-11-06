using System.ComponentModel.DataAnnotations;

namespace MeChat.Api.Features.Friends;

public sealed record FriendRequestCreate([Required] Guid TargetUserId);
public sealed record FriendRequestDto(Guid Id, Guid SenderUserId, Guid ReceiverUserId, string Status, DateTime CreatedAt, DateTime? RespondedAt);
public sealed record FriendDto(Guid UserId, string Username, string DisplayName, Guid? AvatarFileId, DateTime Since);
public sealed record FriendRequestsDto(IReadOnlyList<FriendRequestDto> Incoming, IReadOnlyList<FriendRequestDto> Outgoing);
public sealed record BlockCreate([Required] Guid TargetUserId);
public sealed record BlockedUserDto(Guid UserId, string Username, string DisplayName, DateTime BlockedAt);
