
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Notifications.Repositories;

public sealed class NotificationRepository(
    AppDbContext db) : INotificationRepository
{
    // =====================================================
    // NOTIFICATION PAGINATION
    // =====================================================

    public Task<List<Notification>> GetNotificationPageAsync(
        Guid userId,
        DateTime? before,
        Guid? beforeId,
        int take,
        CancellationToken ct)
    {
        return db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == userId &&
                (
                    before == null ||
                    notification.CreatedAt < before ||
                    (
                        notification.CreatedAt == before &&
                        beforeId != null &&
                        notification.Id.CompareTo(
                            beforeId.Value) < 0
                    )
                ))
            .OrderByDescending(
                notification => notification.CreatedAt)
            .ThenByDescending(
                notification => notification.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    // =====================================================
    // UNREAD NOTIFICATIONS
    // =====================================================

    public Task<List<Notification>> GetUnreadNotificationsAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == userId &&
                notification.ReadAt == null)
            .ToListAsync(ct);
    }

    // =====================================================
    // GET NOTIFICATION FOR UPDATE
    // =====================================================

    public Task<Notification?> GetNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct)
    {
        // Tracking enabled for ReadAt updates.
        return db.Notifications.SingleOrDefaultAsync(
            notification =>
                notification.Id == notificationId &&
                notification.RecipientUserId == userId,
            ct);
    }

    // =====================================================
    // ADMIN WARNING TEXT
    // =====================================================

    public Task<string?> GetAdminWarningTextAsync(
        Guid auditLogId,
        Guid recipientUserId,
        CancellationToken ct)
    {
        // Only return the warning explicitly addressed
        // to this recipient.

        return db.AdminAuditLogs
            .AsNoTracking()
            .Where(audit =>
                audit.Id == auditLogId &&
                audit.Action == "WarnUser" &&
                audit.TargetType == "User" &&
                audit.TargetId == recipientUserId)
            .Select(audit => audit.Details)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // MARK ALL NOTIFICATIONS AS READ
    // =====================================================

    public Task<int> MarkAllReadAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.Notifications
            .Where(notification =>
                notification.RecipientUserId == userId &&
                notification.ReadAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAt,
                    now),
                ct);
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
            .Select(conversation =>
                conversation.Type)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // CONVERSATION MUTE SETTINGS
    // =====================================================

    public Task<DateTime?> GetMutedUntilAsync(
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
                state.MutedUntil)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GET CONVERSATION USER STATE
    // =====================================================

    public Task<ConversationUserState?>
        GetConversationUserStateAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct)
    {
        // Tracking enabled for mute changes.
        return db.ConversationUserStates
            .SingleOrDefaultAsync(
                state =>
                    state.ConversationId == conversationId &&
                    state.UserId == userId,
                ct);
    }

    // =====================================================
    // ADD CONVERSATION USER STATE
    // =====================================================

    public void AddConversationUserState(
        ConversationUserState state)
    {
        db.ConversationUserStates.Add(state);
    }
}
