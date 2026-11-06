
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Messages.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Messages.Repositories;

public sealed class MessageActionRepository(
    AppDbContext db) : IMessageActionRepository
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
                await using var tx =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var result = await operation();

                if (!shouldCommit(result))
                {
                    await tx.RollbackAsync(ct);
                    db.ChangeTracker.Clear();

                    return result;
                }

                await tx.CommitAsync(ct);

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
            "Message action retry limit reached.");
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
        return db.Messages.SingleOrDefaultAsync(
            message => message.Id == messageId,
            ct);
    }

    public Task<Message?> GetUnrecalledMessageAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.RecalledAt == null,
            ct);
    }

    public Task<Message?> GetMessageByClientIdAsync(
        Guid senderUserId,
        Guid clientMessageId,
        CancellationToken ct)
    {
        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.SenderUserId == senderUserId &&
                message.ClientMessageId == clientMessageId,
            ct);
    }

    public Task<bool> IsMessageUnrecalledAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages.AnyAsync(
            message =>
                message.Id == messageId &&
                message.RecalledAt == null,
            ct);
    }

    public Task<bool> IsMessageRecalledAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.Messages.AnyAsync(
            message =>
                message.Id == messageId &&
                message.RecalledAt != null,
            ct);
    }

    // =====================================================
    // MESSAGE EDIT HISTORY
    // =====================================================

    public Task<int> GetEditVersionCountAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageEditHistories.CountAsync(
            history => history.MessageId == messageId,
            ct);
    }

    public Task<List<EditVersionDto>> GetEditVersionsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageEditHistories
            .AsNoTracking()
            .Where(history =>
                history.MessageId == messageId)
            .OrderBy(history => history.Version)
            .Select(history => new EditVersionDto(
                history.Version,
                history.PreviousContent,
                history.EditedAt))
            .ToListAsync(ct);
    }

    public void AddEditHistory(
        MessageEditHistory history)
    {
        db.MessageEditHistories.Add(history);
    }

    public void AddMessage(
        Message message)
    {
        db.Messages.Add(message);
    }

    // =====================================================
    // MESSAGE SEARCH
    // =====================================================

    public Task<List<Message>> SearchMessagesBatchAsync(
        Guid conversationId,
        string query,
        long? beforeSequence,
        int batchSize,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == conversationId &&
                message.RecalledAt == null &&
                message.Content != null &&
                message.Content.Contains(query) &&
                (
                    beforeSequence == null ||
                    message.Sequence < beforeSequence
                ))
            .OrderByDescending(message => message.Sequence)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    // =====================================================
    // MESSAGE USER STATES
    // =====================================================

    public Task<bool> HasMessageUserStateAsync(
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

    public void AddMessageUserState(
        MessageUserState state)
    {
        db.MessageUserStates.Add(state);
    }

    // =====================================================
    // MESSAGE REACTIONS
    // =====================================================

    public Task<List<ReactionCountDto>> GetReactionCountsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageReactions
            .AsNoTracking()
            .Where(reaction =>
                reaction.MessageId == messageId)
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new ReactionCountDto(
                group.Key,
                group.Count()))
            .ToListAsync(ct);
    }

    public Task<string?> GetMyReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct)
    {
        return db.MessageReactions
            .Where(reaction =>
                reaction.MessageId == messageId &&
                reaction.UserId == userId)
            .Select(reaction => reaction.Emoji)
            .SingleOrDefaultAsync(ct);
    }

    public Task<MessageReaction?> GetReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct)
    {
        return db.MessageReactions.SingleOrDefaultAsync(
            reaction =>
                reaction.MessageId == messageId &&
                reaction.UserId == userId,
            ct);
    }

    public void AddReaction(
        MessageReaction reaction)
    {
        db.MessageReactions.Add(reaction);
    }

    public void RemoveReaction(
        MessageReaction reaction)
    {
        db.MessageReactions.Remove(reaction);
    }

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    public Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations.SingleOrDefaultAsync(
            conversation =>
                conversation.Id == conversationId,
            ct);
    }

    public Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations
            .Where(conversation =>
                conversation.Id == conversationId)
            .Select(conversation => conversation.Type)
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

    public Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        return db.ConversationUserStates
            .Where(state =>
                state.ConversationId == conversationId &&
                state.UserId == userId)
            .Select(state =>
                state.HiddenThroughSequence)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GROUP RECIPIENTS
    // =====================================================

    public Task<List<Guid>> GetGroupRecipientsAsync(
        Guid conversationId,
        Guid? excludeUserId,
        CancellationToken ct)
    {
        return db.ConversationMembers
            .Where(member =>
                member.ConversationId == conversationId &&
                member.LeftAt == null &&
                (
                    excludeUserId == null ||
                    member.UserId != excludeUserId
                ))
            .Select(member => member.UserId)
            .ToListAsync(ct);
    }

    // =====================================================
    // PRIVATE RECIPIENTS
    // =====================================================

    public async Task<List<Guid>> GetPrivateRecipientsAsync(
        Guid conversationId,
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
            return [];

        return [pair.UserId1, pair.UserId2];
    }

    // =====================================================
    // MUTED CONVERSATION
    // =====================================================

    public Task<bool> IsConversationMutedAsync(
        Guid conversationId,
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.ConversationUserStates.AnyAsync(
            state =>
                state.ConversationId == conversationId &&
                state.UserId == userId &&
                state.MutedUntil > now,
            ct);
    }

    // =====================================================
    // MESSAGE ATTACHMENTS
    // =====================================================

    public Task<List<MessageAttachment>> GetMessageAttachmentsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.MessageId == messageId)
            .OrderBy(attachment =>
                attachment.DisplayOrder)
            .ToListAsync(ct);
    }

    public void AddMessageAttachment(
        MessageAttachment attachment)
    {
        db.MessageAttachments.Add(attachment);
    }

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    public void AddNotification(
        Notification notification)
    {
        db.Notifications.Add(notification);
    }
}
