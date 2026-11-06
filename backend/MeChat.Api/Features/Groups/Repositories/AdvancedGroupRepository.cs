
using System.Data;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Groups.Repositories;

public sealed class AdvancedGroupRepository(
    AppDbContext db) : IAdvancedGroupRepository
{
    // =====================================================
    // TRANSACTION
    // =====================================================

    public async Task<bool> ExecuteAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            var success = await operation();

            if (!success)
            {
                await transaction.RollbackAsync(ct);

                db.ChangeTracker.Clear();

                return false;
            }

            await transaction.CommitAsync(ct);

            return true;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
            }
            finally
            {
                db.ChangeTracker.Clear();
            }

            throw;
        }
    }

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // GROUP PROFILE
    // =====================================================

    public Task<GroupProfile?> GetGroupProfileAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return db.GroupProfiles.SingleOrDefaultAsync(
            profile =>
                profile.ConversationId == groupId,
            ct);
    }

    // =====================================================
    // AVATAR FILE
    // =====================================================

    public Task<StoredFile?> GetPendingAvatarAsync(
        Guid fileId,
        Guid uploadedByUserId,
        CancellationToken ct)
    {
        return db.StoredFiles.SingleOrDefaultAsync(
            file =>
                file.Id == fileId &&
                file.UploadedByUserId == uploadedByUserId &&
                file.Purpose == "Avatar" &&
                file.Status == "Pending" &&
                file.DeletedAt == null,
            ct);
    }

    // =====================================================
    // ACTIVE GROUP MEMBER
    // =====================================================

    public Task<ConversationMember?> GetActiveMemberAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct)
    {
        return db.ConversationMembers.SingleOrDefaultAsync(
            member =>
                member.ConversationId == groupId &&
                member.UserId == userId &&
                member.LeftAt == null,
            ct);
    }

    // =====================================================
    // ACTIVE USER
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
    // ACTIVE CONVERSATION
    // =====================================================

    public Task<Conversation?> GetActiveConversationAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return db.Conversations.SingleOrDefaultAsync(
            conversation =>
                conversation.Id == groupId &&
                conversation.Status == "Active",
            ct);
    }

    // =====================================================
    // ACTIVE GROUP CONVERSATION
    // =====================================================

    public Task<Conversation?> GetActiveGroupConversationAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return db.Conversations.SingleOrDefaultAsync(
            conversation =>
                conversation.Id == groupId &&
                conversation.Type == "Group" &&
                conversation.Status == "Active",
            ct);
    }

    // =====================================================
    // ACTIVE LINKS CREATED BY MEMBERSHIP
    // =====================================================

    public Task<List<GroupInviteLink>>
        GetActiveLinksByMembershipAsync(
            Guid membershipId,
            CancellationToken ct)
    {
        return db.GroupInviteLinks
            .Where(link =>
                link.CreatedByMembershipId == membershipId &&
                link.RevokedAt == null)
            .ToListAsync(ct);
    }

    // =====================================================
    // ACTIVE GROUP LINKS
    // =====================================================

    public Task<List<GroupInviteLink>>
        GetActiveGroupLinksAsync(
            Guid groupId,
            CancellationToken ct)
    {
        return db.GroupInviteLinks
            .Where(link =>
                link.ConversationId == groupId &&
                link.RevokedAt == null)
            .ToListAsync(ct);
    }

    // =====================================================
    // PENDING INVITATIONS
    // =====================================================

    public Task<List<GroupInvitation>>
        GetPendingInvitationsForUserAsync(
            Guid groupId,
            Guid userId,
            CancellationToken ct)
    {
        return db.GroupInvitations
            .Where(invitation =>
                invitation.ConversationId == groupId &&
                invitation.InviteeUserId == userId &&
                invitation.Status == "Pending")
            .ToListAsync(ct);
    }

    // =====================================================
    // GROUP PIN PAGE
    // =====================================================

    public Task<List<GroupPinRow>> GetPinPageRowsAsync(
        Guid actor,
        Guid groupId,
        long? before,
        int take,
        CancellationToken ct)
    {
        return (
            from pin in db.GroupPins.AsNoTracking()

            join message in db.Messages.AsNoTracking()
                on pin.MessageId equals message.Id

            where pin.ConversationId == groupId &&
                  message.RecalledAt == null &&
                  (
                      before == null ||
                      message.Sequence < before
                  ) &&

                  db.ConversationMembers.Any(member =>
                      member.ConversationId == groupId &&
                      member.UserId == actor &&
                      message.Sequence >= member.JoinSequence &&
                      (
                          member.LeaveSequence == null ||
                          message.Sequence <= member.LeaveSequence
                      )) &&

                  !db.ConversationUserStates.Any(state =>
                      state.ConversationId == groupId &&
                      state.UserId == actor &&
                      message.Sequence <=
                          state.HiddenThroughSequence) &&

                  !db.MessageUserStates.Any(state =>
                      state.MessageId == message.Id &&
                      state.UserId == actor)

            orderby message.Sequence descending

            select new GroupPinRow(
                message.Id,
                message.Sequence,
                pin.PinnedByUserId,
                pin.PinnedAt)
        )
        .Take(take)
        .ToListAsync(ct);
    }

    // =====================================================
    // ALL GROUP PIN ROWS
    // =====================================================

    public Task<List<GroupPinRow>> GetPinRowsAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return (
            from pin in db.GroupPins.AsNoTracking()

            join message in db.Messages.AsNoTracking()
                on pin.MessageId equals message.Id

            where pin.ConversationId == groupId &&
                  message.RecalledAt == null

            orderby pin.PinnedAt descending

            select new GroupPinRow(
                message.Id,
                message.Sequence,
                pin.PinnedByUserId,
                pin.PinnedAt)
        )
        .ToListAsync(ct);
    }

    // =====================================================
    // GET GROUP PIN
    // =====================================================

    public Task<GroupPin?> GetGroupPinAsync(
        Guid groupId,
        Guid messageId,
        CancellationToken ct)
    {
        return db.GroupPins.SingleOrDefaultAsync(
            pin =>
                pin.ConversationId == groupId &&
                pin.MessageId == messageId,
            ct);
    }

    // =====================================================
    // ADD GROUP PIN
    // =====================================================

    public void AddGroupPin(GroupPin pin)
    {
        db.GroupPins.Add(pin);
    }

    // =====================================================
    // REMOVE GROUP PIN
    // =====================================================

    public void RemoveGroupPin(GroupPin pin)
    {
        db.GroupPins.Remove(pin);
    }

    // =====================================================
    // GET MESSAGE
    // =====================================================

    public Task<Message?> GetActiveGroupMessageAsync(
        Guid groupId,
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.ConversationId == groupId &&
                message.RecalledAt == null,
            ct);
    }

    // =====================================================
    // MESSAGE DELETED FOR USER
    // =====================================================

    public Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct)
    {
        return db.MessageUserStates.AnyAsync(
            state =>
                state.MessageId == messageId &&
                state.UserId == userId,
            ct);
    }
}
