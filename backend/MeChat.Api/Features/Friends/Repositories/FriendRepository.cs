
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Friends.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Friends.Repositories;

public sealed class FriendRepository(
    AppDbContext db) : IFriendRepository
{
    // =====================================================
    // TRANSACTION / UNIT OF WORK
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        var maxAttempts = enableRetry ? 3 : 1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var result = await operation();

                await transaction.CommitAsync(ct);

                return result;
            }
            catch (Exception error)
                when (enableRetry &&
                      attempt < maxAttempts - 1 &&
                      IsRetryable(error))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    TimeSpan.FromMilliseconds(
                        30 * (attempt + 1)),
                    ct);
            }
        }

        throw new InvalidOperationException(
            "Friend operation retry limit reached.");
    }

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    private static bool IsRetryable(Exception error)
    {
        for (
            Exception? current = error;
            current is not null;
            current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }

    // =====================================================
    // USER ELIGIBILITY
    // =====================================================

    public Task<int> CountEligibleUsersAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.Users.CountAsync(
            user =>
                (user.Id == firstUserId ||
                 user.Id == secondUserId) &&
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null,
            ct);
    }

    public Task<bool> HasActiveSuspensionAsync(
        Guid firstUserId,
        Guid secondUserId,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions.AnyAsync(
            suspension =>
                (suspension.UserId == firstUserId ||
                 suspension.UserId == secondUserId) &&
                suspension.Status == "Active" &&
                suspension.RevokedAt == null &&
                (suspension.ExpiresAt == null ||
                 suspension.ExpiresAt > now),
            ct);
    }

    public Task<bool> HasActiveBlockAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.UserBlocks.AnyAsync(
            block =>
                (
                    (block.BlockerUserId == firstUserId &&
                     block.BlockedUserId == secondUserId) ||
                    (block.BlockerUserId == secondUserId &&
                     block.BlockedUserId == firstUserId)
                ) &&
                block.UnblockedAt == null,
            ct);
    }

    public Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users.AnyAsync(
            user => user.Id == userId,
            ct);
    }

    // =====================================================
    // FRIENDSHIPS
    // =====================================================

    public Task<bool> HasActiveFriendshipAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.Friendships.AnyAsync(
            friendship =>
                friendship.UserId1 == firstUserId &&
                friendship.UserId2 == secondUserId &&
                friendship.RemovedAt == null,
            ct);
    }

    public Task<Friendship?> GetActiveFriendshipAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.Friendships.SingleOrDefaultAsync(
            friendship =>
                friendship.UserId1 == firstUserId &&
                friendship.UserId2 == secondUserId &&
                friendship.RemovedAt == null,
            ct);
    }

    public void AddFriendship(Friendship friendship)
    {
        db.Friendships.Add(friendship);
    }

    public Task<List<FriendDto>> GetFriendsAsync(
        Guid actor,
        CancellationToken ct)
    {
        return (
            from friendship in db.Friendships.AsNoTracking()

            join user in db.Users.AsNoTracking()
                on (
                    friendship.UserId1 == actor
                        ? friendship.UserId2
                        : friendship.UserId1
                ) equals user.Id

            where friendship.RemovedAt == null &&
                  (friendship.UserId1 == actor ||
                   friendship.UserId2 == actor)

            orderby user.NormalizedUsername, user.Id

            select new FriendDto(
                user.Id,
                user.Username,
                user.DisplayName,
                user.AvatarFileId,
                friendship.CreatedAt)
        ).ToListAsync(ct);
    }

    // =====================================================
    // FRIEND REQUESTS
    // =====================================================

    public Task<FriendRequest?> GetPendingPairRequestAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.FriendRequests.SingleOrDefaultAsync(
            request =>
                request.PairUserId1 == firstUserId &&
                request.PairUserId2 == secondUserId &&
                request.Status == "Pending",
            ct);
    }

    public Task<FriendRequest?> GetPendingReceivedRequestAsync(
        Guid requestId,
        Guid receiverUserId,
        CancellationToken ct)
    {
        return db.FriendRequests.SingleOrDefaultAsync(
            request =>
                request.Id == requestId &&
                request.ReceiverUserId == receiverUserId &&
                request.Status == "Pending",
            ct);
    }

    public Task<FriendRequest?> GetPendingSentRequestAsync(
        Guid requestId,
        Guid senderUserId,
        CancellationToken ct)
    {
        return db.FriendRequests.SingleOrDefaultAsync(
            request =>
                request.Id == requestId &&
                request.SenderUserId == senderUserId &&
                request.Status == "Pending",
            ct);
    }

    public Task<List<FriendRequest>> GetPendingPairRequestsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.FriendRequests
            .Where(request =>
                request.PairUserId1 == firstUserId &&
                request.PairUserId2 == secondUserId &&
                request.Status == "Pending")
            .ToListAsync(ct);
    }

    public async Task<FriendRequestsDto> GetFriendRequestsAsync(
        Guid actor,
        CancellationToken ct)
    {
        var incoming = await db.FriendRequests
            .AsNoTracking()
            .Where(request =>
                request.ReceiverUserId == actor &&
                request.Status == "Pending")
            .OrderByDescending(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .Select(request => new FriendRequestDto(
                request.Id,
                request.SenderUserId,
                request.ReceiverUserId,
                request.Status,
                request.CreatedAt,
                request.RespondedAt))
            .ToListAsync(ct);

        var outgoing = await db.FriendRequests
            .AsNoTracking()
            .Where(request =>
                request.SenderUserId == actor &&
                request.Status == "Pending")
            .OrderByDescending(request => request.CreatedAt)
            .ThenBy(request => request.Id)
            .Select(request => new FriendRequestDto(
                request.Id,
                request.SenderUserId,
                request.ReceiverUserId,
                request.Status,
                request.CreatedAt,
                request.RespondedAt))
            .ToListAsync(ct);

        return new FriendRequestsDto(
            incoming,
            outgoing);
    }

    public void AddFriendRequest(FriendRequest request)
    {
        db.FriendRequests.Add(request);
    }

    // =====================================================
    // USER BLOCKS
    // =====================================================

    public Task<UserBlock?> GetActiveUserBlockAsync(
        Guid blockerUserId,
        Guid blockedUserId,
        CancellationToken ct)
    {
        return db.UserBlocks.SingleOrDefaultAsync(
            block =>
                block.BlockerUserId == blockerUserId &&
                block.BlockedUserId == blockedUserId &&
                block.UnblockedAt == null,
            ct);
    }

    public void AddUserBlock(UserBlock block)
    {
        db.UserBlocks.Add(block);
    }

    public Task<List<BlockedUserDto>> GetBlockedUsersAsync(
        Guid actor,
        CancellationToken ct)
    {
        return (
            from block in db.UserBlocks.AsNoTracking()

            join user in db.Users.AsNoTracking()
                on block.BlockedUserId equals user.Id

            where block.BlockerUserId == actor &&
                  block.UnblockedAt == null

            orderby block.CreatedAt descending, user.Id

            select new BlockedUserDto(
                user.Id,
                user.Username,
                user.DisplayName,
                block.CreatedAt)
        ).ToListAsync(ct);
    }

    // =====================================================
    // PRIVATE CONVERSATION
    // =====================================================

    public Task<PrivateConversationPair?> GetPrivateConversationPairAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.PrivateConversationPairs.SingleOrDefaultAsync(
            pair =>
                pair.UserId1 == firstUserId &&
                pair.UserId2 == secondUserId,
            ct);
    }

    // =====================================================
    // PRIVATE CHAT ACCESS
    // =====================================================

    public Task<PrivateChatAccess?> GetPrivateChatAccessAsync(
        Guid conversationId,
        Guid senderUserId,
        Guid receiverUserId,
        CancellationToken ct)
    {
        return db.PrivateChatAccess.SingleOrDefaultAsync(
            access =>
                access.ConversationId == conversationId &&
                access.SenderUserId == senderUserId &&
                access.ReceiverUserId == receiverUserId,
            ct);
    }

    public Task<List<PrivateChatAccess>> GetPairChatAccessesAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.PrivateChatAccess
            .Where(access =>
                (access.SenderUserId == firstUserId &&
                 access.ReceiverUserId == secondUserId) ||
                (access.SenderUserId == secondUserId &&
                 access.ReceiverUserId == firstUserId))
            .ToListAsync(ct);
    }

    public void AddPrivateChatAccess(
        PrivateChatAccess access)
    {
        db.PrivateChatAccess.Add(access);
    }

    // =====================================================
    // MESSAGE REQUESTS
    // =====================================================

    public Task<List<MessageRequest>> GetPendingConversationRequestsAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.MessageRequests
            .Where(request =>
                request.ConversationId == conversationId &&
                request.Status == "Pending")
            .ToListAsync(ct);
    }

    public Task<List<MessageRequest>> GetPendingPairMessageRequestsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.MessageRequests
            .Where(request =>
                (
                    (request.SenderUserId == firstUserId &&
                     request.ReceiverUserId == secondUserId) ||
                    (request.SenderUserId == secondUserId &&
                     request.ReceiverUserId == firstUserId)
                ) &&
                request.Status == "Pending")
            .ToListAsync(ct);
    }
}
