using System.ComponentModel.DataAnnotations;

namespace MeChat.Api.Features.Friends;

public sealed record FriendRequestCreate([Required] Guid TargetUserId);
public sealed record FriendRequestDto(Guid Id, Guid SenderUserId, Guid ReceiverUserId, string Status, DateTime CreatedAt, DateTime? RespondedAt, DateTime? SeenAt = null);
public sealed record FriendRequestUnreadCountDto(int UnreadCount);
public sealed record FriendRequestSeenInput(IReadOnlyList<Guid>? RequestIds)
{
    public static bool TryNormalize(IReadOnlyList<Guid>? ids, out Guid[] normalized)
    {
        normalized = [];
        if (ids is null || ids.Count is < 1 or > 100 || ids.Any(id => id == Guid.Empty))
            return false;
        normalized = ids.Distinct().ToArray();
        return true;
    }
}
public sealed record FriendDto(Guid UserId, string Username, string DisplayName, Guid? AvatarFileId, DateTime Since);
public sealed record FriendRequestsDto(IReadOnlyList<FriendRequestDto> Incoming, IReadOnlyList<FriendRequestDto> Outgoing);
public sealed record BlockCreate([Required] Guid TargetUserId);
public sealed record BlockedUserDto(Guid UserId, string Username, string DisplayName, DateTime BlockedAt);
