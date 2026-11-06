
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Notifications;

public sealed class NotificationService(
    INotificationRepository repository,
    PrivateChatService privateChats,
    GroupService groups,
    MessageActionService actions,
    TimeProvider clock,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // LIST NOTIFICATIONS
    // =====================================================

    public async Task<NotificationPageDto> ListAsync(
        Guid actor,
        DateTime? before,
        int limit,
        CancellationToken ct,
        Guid? beforeId = null)
    {
        // =================================================
        // PAGINATION
        // =================================================

        var count = Math.Clamp(limit, 1, 50);

        const int batchSize = 200;

        var rows =
            await repository.GetNotificationPageAsync(
                actor,
                before,
                beforeId,
                batchSize,
                ct);

        // =================================================
        // FILTER VISIBLE NOTIFICATIONS
        // =================================================

        var visible = new List<NotificationDto>();

        foreach (var row in rows)
        {
            var canView = await VisibleAsync(
                actor,
                row,
                ct);

            if (canView)
            {
                string? text = null;

                // =============================================
                // ADMIN WARNING
                // =============================================

                if (row.Type == "AdminWarning" &&
                    row.ReferenceId is { } auditLogId)
                {
                    // Only retrieve the warning addressed
                    // to this authenticated recipient.
                    text =
                        await repository.GetAdminWarningTextAsync(
                            auditLogId,
                            actor,
                            ct);
                }

                // =============================================
                // MAP NOTIFICATION DTO
                // =============================================

                visible.Add(
                    new NotificationDto(
                        row.Id,
                        row.Type,
                        row.ReferenceId,
                        row.CreatedAt,
                        row.ReadAt,
                        text));
            }

            // Keep one extra visible item to determine
            // whether another page exists.
            if (visible.Count > count)
                break;
        }

        // =================================================
        // NEXT PAGE
        // =================================================

        var hasMore =
            visible.Count > count ||
            rows.Count == batchSize;

        var cursor = visible.Count > count
            ? visible[count - 1]
            : null;

        var lastScannedRow = rows.LastOrDefault();

        var nextCursor = hasMore
            ? cursor?.CreatedAt ?? lastScannedRow?.CreatedAt
            : null;

        var nextCursorId = hasMore
            ? cursor?.Id ?? lastScannedRow?.Id
            : null;

        // =================================================
        // RESPONSE
        // =================================================

        return new NotificationPageDto(
            visible.Take(count).ToList(),
            nextCursor,
            hasMore,
            nextCursorId);
    }

    // =====================================================
    // UNREAD NOTIFICATION COUNT
    // =====================================================

    public async Task<NotificationCountDto> UnreadAsync(
        Guid actor,
        CancellationToken ct)
    {
        var rows =
            await repository.GetUnreadNotificationsAsync(
                actor,
                ct);

        var count = 0;

        foreach (var row in rows)
        {
            // Only visible notifications contribute
            // to the unread count.

            if (await VisibleAsync(
                    actor,
                    row,
                    ct))
            {
                count++;
            }
        }

        return new NotificationCountDto(count);
    }

    // =====================================================
    // MARK ONE NOTIFICATION AS READ
    // =====================================================

    public async Task<bool> MarkReadAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        // =================================================
        // GET NOTIFICATION
        // =================================================

        var notification =
            await repository.GetNotificationAsync(
                id,
                actor,
                ct);

        if (notification is null)
            return false;

        // =================================================
        // VISIBILITY CHECK
        // =================================================

        var canView =
            await VisibleAsync(
                actor,
                notification,
                ct);

        if (!canView)
            return false;

        // =================================================
        // UPDATE READ STATE
        // =================================================

        notification.ReadAt ??= Now;

        await repository.SaveChangesAsync(ct);

        // =================================================
        // REALTIME
        // =================================================

        await NotifyReadAsync(actor, ct);

        return true;
    }

    // =====================================================
    // MARK ALL NOTIFICATIONS AS READ
    // =====================================================

    public async Task MarkAllReadAsync(
        Guid actor,
        CancellationToken ct)
    {
        // Preserve the original behavior:
        // all unread notifications owned by actor are
        // marked as read, including ones currently hidden
        // by the visibility filter.

        await repository.MarkAllReadAsync(
            actor,
            Now,
            ct);

        // =================================================
        // REALTIME
        // =================================================

        await NotifyReadAsync(actor, ct);
    }

    // =====================================================
    // GET CONVERSATION SETTINGS
    // =====================================================

    public async Task<ConversationSettingsDto?> SettingsAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        // =================================================
        // AUTHORIZATION
        // =================================================

        if (!await CanSetAsync(
                actor,
                conversationId,
                ct))
        {
            return null;
        }

        // =================================================
        // MUTE SETTINGS
        // =================================================

        var mutedUntil =
            await repository.GetMutedUntilAsync(
                conversationId,
                actor,
                ct);

        return new ConversationSettingsDto(
            mutedUntil);
    }

    // =====================================================
    // SET CONVERSATION MUTE
    // =====================================================

    public async Task<bool> SetMuteAsync(
        Guid actor,
        Guid conversationId,
        DateTime? mutedUntil,
        CancellationToken ct)
    {
        var now = Now;

        // =================================================
        // VALIDATE MUTE EXPIRATION
        // =================================================

        if (mutedUntil is not null &&
            (
                mutedUntil <= now ||
                mutedUntil > now.AddYears(1)
            ))
        {
            return false;
        }

        // =================================================
        // AUTHORIZATION
        // =================================================

        if (!await CanSetAsync(
                actor,
                conversationId,
                ct))
        {
            return false;
        }

        // =================================================
        // GET OR CREATE USER STATE
        // =================================================

        var state =
            await repository.GetConversationUserStateAsync(
                conversationId,
                actor,
                ct);

        if (state is null)
        {
            state = new ConversationUserState
            {
                ConversationId = conversationId,
                UserId = actor
            };

            repository.AddConversationUserState(state);
        }

        // =================================================
        // UPDATE MUTE
        // =================================================

        state.MutedUntil = mutedUntil;

        await repository.SaveChangesAsync(ct);

        return true;
    }

    // =====================================================
    // CAN CHANGE CONVERSATION SETTINGS
    // =====================================================

    private async Task<bool> CanSetAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        // =================================================
        // GROUP CONVERSATION
        // =================================================

        if (type == "Group")
        {
            return await groups.CanSendAsync(
                actor,
                conversationId,
                ct);
        }

        // =================================================
        // PRIVATE CONVERSATION
        // =================================================

        if (type == "Private")
        {
            return await privateChats.CanReadAsync(
                actor,
                conversationId,
                ct);
        }

        return false;
    }

    // =====================================================
    // NOTIFICATION VISIBILITY
    // =====================================================

    private async Task<bool> VisibleAsync(
        Guid actor,
        Notification notification,
        CancellationToken ct)
    {
        // =================================================
        // NOTIFICATIONS WITHOUT REFERENCE
        // =================================================

        if (notification.ReferenceId is not { } referenceId)
        {
            return true;
        }

        // =================================================
        // MESSAGE-RELATED NOTIFICATIONS
        // =================================================

        if (notification.Type is
            "Message" or
            "GroupMessage" or
            "MessageRequest")
        {
            // Reuse MessageActionService authorization.
            // Recalled, deleted-for-me or inaccessible
            // messages must not expose their notifications.

            return await actions.CanViewAttachmentAsync(
                actor,
                referenceId,
                ct);
        }

        // =================================================
        // OTHER NOTIFICATION TYPES
        // =================================================

        return true;
    }

    // =====================================================
    // REALTIME NOTIFICATION READ EVENT
    // =====================================================

    private Task NotifyReadAsync(
        Guid actor,
        CancellationToken ct)
    {
        return events.SendAsync(
            [actor],
            "NotificationRead",
            new
            {
                eventId = Guid.NewGuid()
            },
            ct);
    }
}
