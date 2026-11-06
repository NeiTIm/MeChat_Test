
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Conversations.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Conversations.Repositories;

public sealed class MessageRequestRepository(
    AppDbContext db) : IMessageRequestRepository
{
    // =====================================================
    // TRANSACTION / UNIT OF WORK
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
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
                when (attempt < 2 && IsRetryable(error))
            {
                // Discard entities tracked by a failed attempt.
                // Every retry starts with a clean change tracker.
                db.ChangeTracker.Clear();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
        }
    }

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // MESSAGE REQUEST INBOX
    // =====================================================

    public Task<List<MessageRequestDto>> GetPendingInboxAsync(
        Guid receiverUserId,
        CancellationToken ct)
    {
        return (
            from request in db.MessageRequests.AsNoTracking()

            join conversation in db.Conversations.AsNoTracking()
                on request.ConversationId equals conversation.Id

            where request.ReceiverUserId == receiverUserId
               && request.Status == "Pending"

            orderby conversation.UpdatedAt descending,
                request.Id

            select new MessageRequestDto(
                request.Id,
                request.ConversationId,
                request.SenderUserId,
                request.ReceiverUserId,
                request.Status,
                conversation.LastSequence,

                db.ConversationUserStates
                    .Where(state =>
                        state.ConversationId ==
                            request.ConversationId &&
                        state.UserId == receiverUserId)
                    .Select(state => state.RequestReadSequence)
                    .SingleOrDefault(),

                request.CreatedAt
            )
        ).ToListAsync(ct);
    }

    // =====================================================
    // GET PENDING REQUEST FOR UPDATE
    // =====================================================

    public Task<MessageRequest?> GetPendingRequestForUpdateAsync(
        Guid requestId,
        Guid receiverUserId,
        CancellationToken ct)
    {
        // SQL Server update locks ensure that concurrent
        // mutations of this request are serialized.
        //
        // Parameters are supplied by FromSqlInterpolated.
        // This is not string-concatenated SQL.

        return db.MessageRequests
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [MessageRequests] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {requestId}
                  AND [ReceiverUserId] = {receiverUserId}
                  AND [Status] = {"Pending"}
                """)
            .SingleOrDefaultAsync(ct);
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

    public void AddPrivateChatAccess(
        PrivateChatAccess access)
    {
        db.PrivateChatAccess.Add(access);
    }

    // =====================================================
    // CONVERSATION LAST SEQUENCE
    // =====================================================

    public Task<long?> GetLastSequenceAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Conversations
            .Where(conversation =>
                conversation.Id == conversationId)
            .Select(conversation =>
                (long?)conversation.LastSequence)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // CONVERSATION USER STATE
    // =====================================================

    public Task<ConversationUserState?>
        GetConversationUserStateAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct)
    {
        return db.ConversationUserStates
            .SingleOrDefaultAsync(
                state =>
                    state.ConversationId == conversationId &&
                    state.UserId == userId,
                ct);
    }

    public void AddConversationUserState(
        ConversationUserState state)
    {
        db.ConversationUserStates.Add(state);
    }

    // =====================================================
    // RETRYABLE DATABASE ERRORS
    // =====================================================

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
}
