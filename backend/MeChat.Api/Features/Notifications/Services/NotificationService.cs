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

    // Lấy một trang thông báo, lọc bỏ những cái actor không còn xem được.
    public async Task<NotificationPageDto> ListAsync(
        Guid actor,
        DateTime? before,
        int limit,
        CancellationToken ct,
        Guid? beforeId = null)
    {
        var count = Math.Clamp(limit, 1, 50);

        const int batchSize = 200;

        var rows = await repository.GetNotificationPageAsync(
            actor,
            before,
            beforeId,
            batchSize,
            ct);

        var visible = new List<NotificationDto>();

        foreach (var row in rows)
        {
            var canView = await VisibleAsync(actor, row, ct);
            if (!canView)
                continue;

            string? text = null;

            // Cảnh báo admin: chỉ lấy nội dung gửi cho chính người nhận này.
            if (row.Type == "AdminWarning" && row.ReferenceId is { } auditLogId)
            {
                text = await repository.GetAdminWarningTextAsync(auditLogId, actor, ct);
            }

            visible.Add(new NotificationDto(
                row.Id,
                row.Type,
                row.ReferenceId,
                row.CreatedAt,
                row.ReadAt,
                text));

            // Giữ dư một phần tử để biết còn trang kế tiếp hay không.
            if (visible.Count > count)
                break;
        }

        var hasMore = visible.Count > count || rows.Count == batchSize;

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

        return new NotificationPageDto(
            visible.Take(count).ToList(),
            nextCursor,
            hasMore,
            nextCursorId);
    }

    // Đếm thông báo chưa đọc, chỉ tính những cái còn hiển thị.
    public async Task<NotificationCountDto> UnreadAsync(Guid actor, CancellationToken ct)
    {
        var rows = await repository.GetUnreadNotificationsAsync(actor, ct);

        var count = 0;

        foreach (var row in rows)
        {
            if (await VisibleAsync(actor, row, ct))
                count++;
        }

        return new NotificationCountDto(count);
    }

    // Đánh dấu một thông báo là đã đọc, sau khi kiểm tra quyền xem.
    public async Task<bool> MarkReadAsync(Guid actor, Guid id, CancellationToken ct)
    {
        var notification = await repository.GetNotificationAsync(id, actor, ct);
        if (notification is null)
            return false;

        var canView = await VisibleAsync(actor, notification, ct);
        if (!canView)
            return false;

        var changed = await repository.MarkReadAsync(id, actor, Now, ct);
        if (changed == 0)
        {
            // A delete may have committed since the initial SELECT. Never mark Trash read.
            var current = await repository.GetNotificationAsync(id, actor, ct);
            if (current is null || current.ReadAt is null || !await VisibleAsync(actor, current, ct)) return false;
        }
        await NotifyReadAsync(actor, ct);

        return true;
    }

    // Đánh dấu tất cả là đã đọc, kể cả cái đang bị bộ lọc hiển thị ẩn đi.
    public async Task MarkAllReadAsync(Guid actor, CancellationToken ct)
    {
        await repository.MarkAllReadAsync(actor, Now, ct);

        await NotifyReadAsync(actor, ct);
    }

    public async Task<TrashedNotificationPageDto> TrashAsync(Guid actor, DateTime? before, Guid? beforeId, int limit, CancellationToken ct)
    {
        var count = Math.Clamp(limit, 1, 50);
        var rows = await repository.GetTrashPageAsync(actor, before, beforeId, 200, ct);
        var visible = new List<TrashedNotificationDto>();
        foreach (var row in rows)
        {
            if (!await VisibleAsync(actor, row, ct)) continue;
            var text = row.Type == "AdminWarning" && row.ReferenceId is { } auditId
                ? await repository.GetAdminWarningTextAsync(auditId, actor, ct) : null;
            visible.Add(new(row.Id, row.Type, row.ReferenceId, row.CreatedAt, row.ReadAt, row.DeletedAt!.Value, text));
            if (visible.Count > count) break;
        }
        var hasMore = visible.Count > count || rows.Count == 200;
        var last = visible.Count > count ? visible[count - 1] : null;
        return new(visible.Take(count).ToList(), hasMore ? last?.CreatedAt ?? rows.LastOrDefault()?.CreatedAt : null,
            hasMore, hasMore ? last?.Id ?? rows.LastOrDefault()?.Id : null);
    }

    public async Task<bool> SetDeletedAsync(Guid actor, Guid id, bool restore, CancellationToken ct)
    {
        var row = await repository.GetLifecycleNotificationAsync(id, actor, ct);
        if (row is null || !await VisibleAsync(actor, row, ct)) return false;
        // ExecuteUpdate is a single autocommitted statement; publish only after it succeeds.
        var changed = await repository.SetDeletedAsync(id, actor, restore ? null : Now, ct);
        if (changed > 0)
        {
            if (restore)
                await events.SendAsync([actor], "NotificationRestored", new { eventId = Guid.NewGuid(), notificationId = id }, ct);
            else
                await events.SendAsync([actor], "NotificationDeleted", new { eventId = Guid.NewGuid(), scope = "Single", notificationId = (Guid?)id }, ct);
        }
        return true;
    }

    public async Task DeleteReadAsync(Guid actor, CancellationToken ct)
    {
        if (await repository.DeleteReadAsync(actor, Now, ct) > 0)
            await events.SendAsync([actor], "NotificationDeleted", new { eventId = Guid.NewGuid(), scope = "ReadBatch", notificationId = (Guid?)null }, ct);
    }

    // Lấy cài đặt mute của actor trong hội thoại.
    public async Task<ConversationSettingsDto?> SettingsAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        if (!await CanSetAsync(actor, conversationId, ct))
            return null;

        var mutedUntil = await repository.GetMutedUntilAsync(conversationId, actor, ct);

        return new ConversationSettingsDto(mutedUntil);
    }

    // Đặt hoặc huỷ mute; từ chối mốc trong quá khứ hoặc quá một năm.
    public async Task<bool> SetMuteAsync(
        Guid actor,
        Guid conversationId,
        DateTime? mutedUntil,
        CancellationToken ct)
    {
        var now = Now;

        if (mutedUntil is not null &&
            (mutedUntil <= now || mutedUntil > now.AddYears(1)))
        {
            return false;
        }

        if (!await CanSetAsync(actor, conversationId, ct))
            return false;

        var state = await repository.GetConversationUserStateAsync(conversationId, actor, ct);

        if (state is null)
        {
            state = new ConversationUserState
            {
                ConversationId = conversationId,
                UserId = actor
            };

            repository.AddConversationUserState(state);
        }

        state.MutedUntil = mutedUntil;

        await repository.SaveChangesAsync(ct);

        return true;
    }

    // Kiểm tra actor có quyền đổi cài đặt hội thoại hay không.
    private async Task<bool> CanSetAsync(Guid actor, Guid conversationId, CancellationToken ct)
    {
        var type = await repository.GetConversationTypeAsync(conversationId, ct);

        return type switch
        {
            "Group" => await groups.CanSendAsync(actor, conversationId, ct),
            "Private" => await privateChats.CanReadAsync(actor, conversationId, ct),
            _ => false
        };
    }

    // Kiểm tra thông báo còn hiển thị được với actor hay không.
    private async Task<bool> VisibleAsync(Guid actor, Notification notification, CancellationToken ct)
    {
        // Không có tham chiếu thì luôn hiển thị.
        if (notification.ReferenceId is not { } referenceId)
            return true;

        // Thông báo tin nhắn: dùng lại quyền xem của MessageActionService,
        // tránh lộ thông báo của tin đã thu hồi / đã xoá phía tôi / không truy cập được.
        if (notification.Type is "Message" or "GroupMessage" or "MessageRequest")
        {
            return await actions.CanViewAttachmentAsync(actor, referenceId, ct);
        }

        return true;
    }

    // Bắn sự kiện realtime báo cho client biết trạng thái đọc đã thay đổi.
    private Task NotifyReadAsync(Guid actor, CancellationToken ct)
    {
        return events.SendAsync(
            [actor],
            "NotificationRead",
            new { eventId = Guid.NewGuid() },
            ct);
    }
}
