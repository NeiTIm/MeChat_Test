
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Messages.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Messages.Repositories;

public sealed class MessageStatusRepository(
    AppDbContext db) : IMessageStatusRepository
{
    // =====================================================
    // TRANSACTION / RETRY
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> shouldCommit,
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

                if (!shouldCommit(result))
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
            "Message status retry limit reached.");
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
    // MESSAGES
    // =====================================================

    public Task<Message?> GetMessageAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                message => message.Id == messageId,
                ct);
    }

    public Task<Message?> GetSentMessageAsync(
        Guid senderUserId,
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                message =>
                    message.Id == messageId &&
                    message.SenderUserId == senderUserId,
                ct);
    }

    public Task<List<Message>> GetMessagesByIdsAsync(
        Guid conversationId,
        Guid[] messageIds,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .Where(message =>
                messageIds.Contains(message.Id) &&
                message.ConversationId == conversationId)
            .ToListAsync(ct);
    }

    public Task<Message?> GetMessageAtSequenceAsync(
        Guid conversationId,
        long sequence,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                message =>
                    message.ConversationId == conversationId &&
                    message.Sequence == sequence,
                ct);
    }

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

    // =====================================================
    // DELIVERIES
    // =====================================================

    public Task<List<Guid>> GetDeliveredRecipientIdsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageDeliveries
            .AsNoTracking()
            .Where(delivery =>
                delivery.MessageId == messageId)
            .Select(delivery =>
                delivery.RecipientUserId)
            .ToListAsync(ct);
    }

    public Task<List<Guid>> GetExistingDeliveryMessageIdsAsync(
        Guid recipientUserId,
        Guid[] messageIds,
        CancellationToken ct)
    {
        return db.MessageDeliveries
            .AsNoTracking()
            .Where(delivery =>
                messageIds.Contains(delivery.MessageId) &&
                delivery.RecipientUserId == recipientUserId)
            .Select(delivery =>
                delivery.MessageId)
            .ToListAsync(ct);
    }

    public void AddDelivery(MessageDelivery delivery)
    {
        db.MessageDeliveries.Add(delivery);
    }

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    public Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.Id == conversationId)
            .Select(conversation =>
                conversation.Type)
            .SingleOrDefaultAsync(ct);
    }

    public Task<bool> IsInactiveGroupAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations.AnyAsync(
            conversation =>
                conversation.Id == conversationId &&
                conversation.Type == "Group" &&
                conversation.Status != "Active",
            ct);
    }

    public Task<long?> GetLastSequenceAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.Id == conversationId)
            .Select(conversation =>
                (long?)conversation.LastSequence)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // READ CURSORS
    // =====================================================

    public Task<bool> HasReadThroughAsync(
        Guid conversationId,
        Guid userId,
        long sequence,
        CancellationToken ct)
    {
        return db.ConversationReadCursors.AnyAsync(
            cursor =>
                cursor.ConversationId == conversationId &&
                cursor.UserId == userId &&
                cursor.LastReadSequence >= sequence,
            ct);
    }

    public Task<ConversationReadCursor?> GetReadCursorAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        // Tracking enabled for updating the cursor.
        return db.ConversationReadCursors
            .SingleOrDefaultAsync(
                cursor =>
                    cursor.ConversationId == conversationId &&
                    cursor.UserId == userId,
                ct);
    }

    public Task<long> GetReadSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationReadCursors
            .AsNoTracking()
            .Where(cursor =>
                cursor.ConversationId == conversationId &&
                cursor.UserId == userId)
            .Select(cursor =>
                cursor.LastReadSequence)
            .SingleOrDefaultAsync(ct);
    }

    public void AddReadCursor(
        ConversationReadCursor cursor)
    {
        db.ConversationReadCursors.Add(cursor);
    }

    // =====================================================
    // CONVERSATION USER STATE
    // =====================================================

    public Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationUserStates
            .AsNoTracking()
            .Where(state =>
                state.ConversationId == conversationId &&
                state.UserId == userId)
            .Select(state =>
                state.HiddenThroughSequence)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // UNREAD COUNT
    // =====================================================

    public Task<int> CountUnreadMessagesAsync(
        Guid actor,
        Guid conversationId,
        long lowerSequence,
        bool isPrivate,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .CountAsync(
                message =>
                    message.ConversationId == conversationId &&
                    message.Sequence > lowerSequence &&
                    message.SenderUserId != actor &&

                    // Delete For Me
                    !db.MessageUserStates.Any(state =>
                        state.MessageId == message.Id &&
                        state.UserId == actor) &&

                    // Private messages do not require
                    // group membership sequence checks.
                    (
                        isPrivate ||

                        db.ConversationMembers.Any(member =>
                            member.ConversationId ==
                                conversationId &&
                            member.UserId == actor &&
                            message.Sequence >=
                                member.JoinSequence &&
                            (
                                member.LeaveSequence == null ||
                                message.Sequence <=
                                    member.LeaveSequence
                            ))
                    ),
                ct);
    }

    // =====================================================
    // PRIVATE CONVERSATION RECIPIENT
    // =====================================================

    public async Task<Guid?> GetOtherPrivateParticipantAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct)
    {
        var pair = await db.PrivateConversationPairs
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
            return null;

        if (pair.UserId1 == actor)
            return pair.UserId2;

        if (pair.UserId2 == actor)
            return pair.UserId1;

        return null;
    }

    // =====================================================
    // OTHER ACTIVE GROUP MEMBERS
    // =====================================================

    public Task<List<Guid>> GetOtherActiveGroupMembersAsync(
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
            .Select(member =>
                member.UserId)
            .Distinct()
            .ToListAsync(ct);
    }
}
