
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Groups.Repositories;

public sealed class GroupInviteLinkRepository(
    AppDbContext db) : IGroupInviteLinkRepository
{
    // =====================================================
    // TRANSACTION / RETRY
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

                // A null or false result means that the
                // current business operation did not succeed.
                // Roll back and detach pending changes.
                if (result is null ||
                    result is bool success && !success)
                {
                    await transaction.RollbackAsync(ct);
                    db.ChangeTracker.Clear();

                    return result;
                }

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
                    30 * (attempt + 1),
                    ct);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException(
            "Group invite link retry limit reached.");
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
    // MEMBERSHIP
    // =====================================================

    public Task<ConversationMember?> GetActiveMemberAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationMembers
            .SingleOrDefaultAsync(
                member =>
                    member.ConversationId == groupId &&
                    member.UserId == userId &&
                    member.LeftAt == null,
                ct);
    }

    public Task<bool> HasActiveMemberAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationMembers.AnyAsync(
            member =>
                member.ConversationId == groupId &&
                member.UserId == userId &&
                member.LeftAt == null,
            ct);
    }

    public Task<int> CountActiveMembersAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return db.ConversationMembers.CountAsync(
            member =>
                member.ConversationId == groupId &&
                member.LeftAt == null,
            ct);
    }

    public Task<DateTime?> GetLastRemovalAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationMembers
            .Where(member =>
                member.ConversationId == groupId &&
                member.UserId == userId &&
                member.LeftReason == "Removed")
            .MaxAsync(
                member => (DateTime?)member.LeftAt,
                ct);
    }

    public Task<Guid?> GetLinkCreatorUserIdAsync(
        Guid membershipId,
        CancellationToken ct)
    {
        return db.ConversationMembers
            .Where(member => member.Id == membershipId)
            .Select(member => (Guid?)member.UserId)
            .SingleOrDefaultAsync(ct);
    }

    public void AddMember(
        ConversationMember member)
    {
        db.ConversationMembers.Add(member);
    }

    // =====================================================
    // CREATE INVITE LINK
    // =====================================================

    public void AddLink(GroupInviteLink link)
    {
        db.GroupInviteLinks.Add(link);
    }

    // =====================================================
    // GET ACTIVE LINK
    // =====================================================

    public Task<GroupInviteLink?> GetActiveLinkAsync(
        Guid groupId,
        Guid linkId,
        CancellationToken ct)
    {
        return db.GroupInviteLinks
            .SingleOrDefaultAsync(
                link =>
                    link.Id == linkId &&
                    link.ConversationId == groupId &&
                    link.RevokedAt == null,
                ct);
    }

    // =====================================================
    // RESOLVE LINK ENTITY BY HASH
    // =====================================================

    public Task<GroupInviteLink?> GetValidLinkByHashAsync(
        string tokenHash,
        DateTime now,
        CancellationToken ct)
    {
        return db.GroupInviteLinks
            .SingleOrDefaultAsync(
                link =>
                    link.TokenHash == tokenHash &&
                    link.RevokedAt == null &&
                    (
                        link.ExpiresAt == null ||
                        link.ExpiresAt > now
                    ),
                ct);
    }

    // =====================================================
    // LIST INVITE LINKS
    // =====================================================

    public Task<List<GroupLinkDto>> GetLinksAsync(
        Guid groupId,
        Guid actor,
        bool privileged,
        CancellationToken ct)
    {
        return (
            from link in db.GroupInviteLinks.AsNoTracking()

            join creator in db.ConversationMembers.AsNoTracking()
                on link.CreatedByMembershipId equals creator.Id

            where link.ConversationId == groupId &&
                  link.RevokedAt == null &&
                  (
                      privileged ||
                      creator.UserId == actor
                  )

            orderby link.CreatedAt descending

            select new GroupLinkDto(
                link.Id,
                creator.UserId,
                link.CreatedAt,
                link.ExpiresAt,
                null)
        ).ToListAsync(ct);
    }

    // =====================================================
    // GROUP LINK PREVIEW
    // =====================================================

    public Task<GroupLinkPreviewDto?> GetLinkPreviewAsync(
        string tokenHash,
        DateTime now,
        CancellationToken ct)
    {
        return (
            from link in db.GroupInviteLinks.AsNoTracking()

            join conversation in db.Conversations.AsNoTracking()
                on link.ConversationId equals conversation.Id

            join profile in db.GroupProfiles.AsNoTracking()
                on conversation.Id equals profile.ConversationId

            where link.TokenHash == tokenHash &&
                  link.RevokedAt == null &&
                  (
                      link.ExpiresAt == null ||
                      link.ExpiresAt > now
                  ) &&
                  conversation.Status == "Active"

            select new GroupLinkPreviewDto(
                conversation.Id,
                profile.Name,
                profile.Description)
        ).SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // ACTIVE GROUP WITH SQL SERVER UPDATE LOCK
    // =====================================================

    public Task<Conversation?> GetActiveGroupForUpdateAsync(
        Guid groupId,
        CancellationToken ct)
    {
        // This query is executed inside the Serializable
        // transaction opened by ExecuteAtomicAsync.
        //
        // FromSqlInterpolated safely parameterizes groupId.

        return db.Conversations
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Conversations] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {groupId}
                """)
            .SingleOrDefaultAsync(
                conversation =>
                    conversation.Type == "Group" &&
                    conversation.Status == "Active",
                ct);
    }

    // =====================================================
    // USER ACTIVE STATUS
    // =====================================================

    public async Task<bool> IsActiveUserAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(
            user =>
                user.Id == userId &&
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null,
            ct);

        if (!exists)
            return false;

        var suspended =
            await db.AccountSuspensions.AnyAsync(
                suspension =>
                    suspension.UserId == userId &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    ),
                ct);

        return !suspended;
    }

    // =====================================================
    // BLOCK CHECK (BOTH DIRECTIONS)
    // =====================================================

    public Task<bool> IsBlockedBetweenAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return db.UserBlocks.AnyAsync(
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
}
