using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Notifications.Repositories;

public sealed class NotificationRepository(
    AppDbContext db) : INotificationRepository
{
    public Task<List<Notification>> GetNotificationPageAsync(
        Guid userId,
        DateTime? before,
        Guid? beforeId,
        int take,
        CancellationToken ct)
    {
        // Phân trang theo cursor: ưu tiên CreatedAt, cùng thời điểm thì so Id.
        return db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == userId && notification.DeletedAt == null &&
                (
                    before == null ||
                    notification.CreatedAt < before ||
                    (
                        notification.CreatedAt == before &&
                        beforeId != null &&
                        notification.Id.CompareTo(beforeId.Value) < 0
                    )
                ))
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<List<Notification>> GetUnreadNotificationsAsync(
        Guid userId,
        CancellationToken ct)
    {
        // Các thông báo chưa có ReadAt.
        return db.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == userId &&
                notification.DeletedAt == null &&
                notification.ReadAt == null)
            .ToListAsync(ct);
    }

    public Task<Notification?> GetNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct)
    {
        // Snapshot only; the conditional UPDATE guards read/delete races at write time.
        return db.Notifications.AsNoTracking().SingleOrDefaultAsync(
            notification =>
                notification.Id == notificationId &&
                notification.RecipientUserId == userId && notification.DeletedAt == null,
            ct);
    }

    public Task<string?> GetAdminWarningTextAsync(
        Guid auditLogId,
        Guid recipientUserId,
        CancellationToken ct)
    {
        // Chỉ trả về cảnh báo được gửi đích danh cho người nhận này.
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

    public Task<int> MarkAllReadAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        // Cập nhật hàng loạt, không load entity lên.
        return db.Notifications
            .Where(notification =>
                notification.RecipientUserId == userId &&
                notification.DeletedAt == null &&
                notification.ReadAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAt,
                    now),
                ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        // Lưu các thay đổi đang được tracking.
        return db.SaveChangesAsync(ct);
    }

    public Task<List<Notification>> GetTrashPageAsync(Guid actor, DateTime? before, Guid? beforeId, int take, CancellationToken ct) =>
        db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == actor && n.DeletedAt != null &&
                (before == null || n.CreatedAt < before ||
                    (n.CreatedAt == before && beforeId != null && n.Id.CompareTo(beforeId.Value) < 0)))
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(take).ToListAsync(ct);

    public Task<Notification?> GetLifecycleNotificationAsync(Guid id, Guid actor, CancellationToken ct) =>
        db.Notifications.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id && n.RecipientUserId == actor, ct);

    public Task<int> SetDeletedAsync(Guid id, Guid actor, DateTime? deletedAt, CancellationToken ct)
    {
        var rows = db.Notifications.Where(n => n.Id == id && n.RecipientUserId == actor);
        rows = deletedAt == null ? rows.Where(n => n.DeletedAt != null) : rows.Where(n => n.DeletedAt == null);
        return rows.ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, deletedAt), ct);
    }

    public Task<int> DeleteReadAsync(Guid actor, DateTime now, CancellationToken ct) =>
        db.Notifications.Where(n => n.RecipientUserId == actor && n.DeletedAt == null && n.ReadAt != null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, (DateTime?)now), ct);

    public Task<int> MarkReadAsync(Guid id, Guid actor, DateTime now, CancellationToken ct) =>
        db.Notifications.Where(n => n.Id == id && n.RecipientUserId == actor && n.DeletedAt == null && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, (DateTime?)now), ct);

    public Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        // Chỉ lấy cột Type.
        return db.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.Id == conversationId)
            .Select(conversation => conversation.Type)
            .SingleOrDefaultAsync(ct);
    }

    public Task<DateTime?> GetMutedUntilAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        // Chỉ lấy cột MutedUntil.
        return db.ConversationUserStates
            .AsNoTracking()
            .Where(state =>
                state.ConversationId == conversationId &&
                state.UserId == userId)
            .Select(state => state.MutedUntil)
            .SingleOrDefaultAsync(ct);
    }

    public Task<ConversationUserState?> GetConversationUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        // Bật tracking để cập nhật mute.
        return db.ConversationUserStates
            .SingleOrDefaultAsync(
                state =>
                    state.ConversationId == conversationId &&
                    state.UserId == userId,
                ct);
    }

    public void AddConversationUserState(ConversationUserState state)
    {
        // Thêm mới vào change tracker, chờ SaveChanges.
        db.ConversationUserStates.Add(state);
    }
}
