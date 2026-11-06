
using MeChat.Api.Features.Friends;

namespace MeChat.Api.Features.Friends.Repositories.Interfaces;

public interface IFriendRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false);

    Task<int> SaveChangesAsync(CancellationToken ct);

    // =====================================================
    // USER ELIGIBILITY
    // =====================================================

    Task<int> CountEligibleUsersAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<bool> HasActiveSuspensionAsync(
        Guid firstUserId,
        Guid secondUserId,
        DateTime now,
        CancellationToken ct);

    Task<bool> HasActiveBlockAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // FRIENDSHIPS
    // =====================================================

    Task<bool> HasActiveFriendshipAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<Friendship?> GetActiveFriendshipAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    void AddFriendship(Friendship friendship);

    Task<List<FriendDto>> GetFriendsAsync(
        Guid actor,
        CancellationToken ct);

    // =====================================================
    // FRIEND REQUESTS
    // =====================================================

    Task<FriendRequest?> GetPendingPairRequestAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<FriendRequest?> GetPendingReceivedRequestAsync(
        Guid requestId,
        Guid receiverUserId,
        CancellationToken ct);

    Task<FriendRequest?> GetPendingSentRequestAsync(
        Guid requestId,
        Guid senderUserId,
        CancellationToken ct);

    Task<List<FriendRequest>> GetPendingPairRequestsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<FriendRequestsDto> GetFriendRequestsAsync(
        Guid actor,
        CancellationToken ct);

    void AddFriendRequest(FriendRequest request);

    // =====================================================
    // USER BLOCKS
    // =====================================================

    Task<UserBlock?> GetActiveUserBlockAsync(
        Guid blockerUserId,
        Guid blockedUserId,
        CancellationToken ct);

    void AddUserBlock(UserBlock block);

    Task<List<BlockedUserDto>> GetBlockedUsersAsync(
        Guid actor,
        CancellationToken ct);

    // =====================================================
    // PRIVATE CONVERSATION
    // =====================================================

    Task<PrivateConversationPair?> GetPrivateConversationPairAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    // =====================================================
    // PRIVATE CHAT ACCESS
    // =====================================================

    Task<PrivateChatAccess?> GetPrivateChatAccessAsync(
        Guid conversationId,
        Guid senderUserId,
        Guid receiverUserId,
        CancellationToken ct);

    Task<List<PrivateChatAccess>> GetPairChatAccessesAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    void AddPrivateChatAccess(PrivateChatAccess access);

    // =====================================================
    // MESSAGE REQUESTS
    // =====================================================

    Task<List<MessageRequest>> GetPendingConversationRequestsAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<List<MessageRequest>> GetPendingPairMessageRequestsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);
}
