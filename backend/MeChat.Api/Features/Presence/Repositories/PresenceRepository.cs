
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Presence.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Presence.Repositories;

public sealed class PresenceRepository(
    AppDbContext db) : IPresenceRepository
{
    // =====================================================
    // ACTIVE USER
    // =====================================================

    public Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Id == userId &&
                    user.EmailVerifiedAt != null &&
                    user.DeactivatedAt == null,
                ct);
    }

    // =====================================================
    // ACTIVE ACCOUNT SUSPENSIONS
    // =====================================================

    public Task<bool> HasActiveSuspensionAsync(
        Guid firstUserId,
        Guid secondUserId,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions
            .AsNoTracking()
            .AnyAsync(
                suspension =>
                    (
                        suspension.UserId == firstUserId ||
                        suspension.UserId == secondUserId
                    ) &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    ),
                ct);
    }

    // =====================================================
    // LAST SEEN
    // =====================================================

    public Task<DateTime?> GetLastSeenAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.LastSeenAt)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // UPDATE LAST SEEN
    // =====================================================

    public async Task UpdateLastSeenAsync(
        Guid userId,
        DateTime lastSeenAt,
        CancellationToken ct)
    {
        // Direct database update.
        // No need to load and track the User entity.

        await db.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    user => user.LastSeenAt,
                    lastSeenAt),
                ct);
    }

    // =====================================================
    // PRIVACY SETTINGS
    // =====================================================

    public async Task<(bool ShowOnline, bool ShowLastSeen)>
        GetPrivacyAsync(
            Guid userId,
            CancellationToken ct)
    {
        var settings =
            await db.UserPrivacySettings
                .AsNoTracking()
                .Where(setting =>
                    setting.UserId == userId)
                .Select(setting => new
                {
                    setting.ShowOnline,
                    setting.ShowLastSeen
                })
                .SingleOrDefaultAsync(ct);

        if (settings is null)
        {
            return (false, false);
        }

        return (
            settings.ShowOnline,
            settings.ShowLastSeen);
    }

    // =====================================================
    // FRIENDSHIP
    // =====================================================

    public Task<bool> AreFriendsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.Friendships
            .AsNoTracking()
            .AnyAsync(
                friendship =>
                    (
                        (
                            friendship.UserId1 == firstUserId &&
                            friendship.UserId2 == secondUserId
                        ) ||
                        (
                            friendship.UserId1 == secondUserId &&
                            friendship.UserId2 == firstUserId
                        )
                    ) &&
                    friendship.RemovedAt == null,
                ct);
    }

    // =====================================================
    // ACCEPTED PRIVATE CHAT ACCESS
    // =====================================================

    public Task<bool> HasAcceptedPrivateChatAccessAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.PrivateChatAccess
            .AsNoTracking()
            .AnyAsync(
                access =>
                    (
                        (
                            access.SenderUserId == firstUserId &&
                            access.ReceiverUserId == secondUserId
                        ) ||
                        (
                            access.SenderUserId == secondUserId &&
                            access.ReceiverUserId == firstUserId
                        )
                    ) &&
                    access.Status == "Accepted",
                ct);
    }

    // =====================================================
    // BLOCKED RELATIONSHIP
    // =====================================================

    public Task<bool> IsBlockedBetweenAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block =>
                    (
                        (
                            block.BlockerUserId == firstUserId &&
                            block.BlockedUserId == secondUserId
                        ) ||
                        (
                            block.BlockerUserId == secondUserId &&
                            block.BlockedUserId == firstUserId
                        )
                    ) &&
                    block.UnblockedAt == null,
                ct);
    }

    // =====================================================
    // ACTIVE USER SESSION
    // =====================================================

    public Task<bool> HasActiveSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct)
    {
        return db.UserSessions
            .AsNoTracking()
            .AnyAsync(
                session =>
                    session.Id == sessionId &&
                    session.UserId == userId &&
                    session.RevokedAt == null,
                ct);
    }

    // =====================================================
    // CONVERSATION TYPE
    // =====================================================

    public Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.Id == conversationId)
            .Select(conversation => conversation.Type)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GROUP TYPING RECIPIENTS
    // =====================================================

    public Task<List<Guid>> GetGroupRecipientsAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct)
    {
        return db.ConversationMembers
            .AsNoTracking()
            .Where(member =>
                member.ConversationId == conversationId &&
                member.UserId != actor &&
                member.LeftAt == null)
            .Select(member => member.UserId)
            .ToListAsync(ct);
    }

    // =====================================================
    // PRIVATE TYPING RECIPIENTS
    // =====================================================

    public async Task<List<Guid>> GetPrivateRecipientsAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct)
    {
        var pair =
            await db.PrivateConversationPairs
                .AsNoTracking()
                .Where(item =>
                    item.ConversationId == conversationId)
                .Select(item => new
                {
                    item.UserId1,
                    item.UserId2
                })
                .SingleOrDefaultAsync(ct);

        if (pair is null)
            return [];

        if (pair.UserId1 == actor)
            return [pair.UserId2];

        if (pair.UserId2 == actor)
            return [pair.UserId1];

        return [];
    }

    // =====================================================
    // PRESENCE BROADCAST VIEWERS
    // =====================================================

    public async Task<List<Guid>> GetPresenceViewerIdsAsync(
        Guid actor,
        CancellationToken ct)
    {
        // =================================================
        // FRIENDS
        // =================================================

        var friends =
            await db.Friendships
                .AsNoTracking()
                .Where(friendship =>
                    (
                        friendship.UserId1 == actor ||
                        friendship.UserId2 == actor
                    ) &&
                    friendship.RemovedAt == null)
                .Select(friendship =>
                    friendship.UserId1 == actor
                        ? friendship.UserId2
                        : friendship.UserId1)
                .ToListAsync(ct);

        // =================================================
        // ACCEPTED PRIVATE CHAT PARTICIPANTS
        // =================================================

        var accepted =
            await db.PrivateChatAccess
                .AsNoTracking()
                .Where(access =>
                    (
                        access.SenderUserId == actor ||
                        access.ReceiverUserId == actor
                    ) &&
                    access.Status == "Accepted")
                .Select(access =>
                    access.SenderUserId == actor
                        ? access.ReceiverUserId
                        : access.SenderUserId)
                .ToListAsync(ct);

        // =================================================
        // DEDUPLICATE VIEWERS
        // =================================================

        return friends
            .Concat(accepted)
            .Distinct()
            .ToList();
    }
}
