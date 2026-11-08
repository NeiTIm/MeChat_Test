using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Notifications.Repositories.Interfaces;

public interface INotificationRepository
{
    // Lấy một trang thông báo của user, phân trang theo cursor (before + beforeId).
    Task<List<Notification>> GetNotificationPageAsync(
        Guid userId,
        DateTime? before,
        Guid? beforeId,
        int take,
        CancellationToken ct);

    // Lấy toàn bộ thông báo chưa đọc của user.
    Task<List<Notification>> GetUnreadNotificationsAsync(
        Guid userId,
        CancellationToken ct);

    // Active snapshot only; conditional UPDATE protects read/delete races.
    Task<Notification?> GetNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct);

    // Lấy nội dung cảnh báo admin gửi đích danh cho người nhận.
    Task<string?> GetAdminWarningTextAsync(
        Guid auditLogId,
        Guid recipientUserId,
        CancellationToken ct);

    // Đánh dấu tất cả thông báo chưa đọc của user là đã đọc.
    Task<int> MarkAllReadAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    // Lưu thay đổi đang theo dõi xuống DB.
    Task<int> SaveChangesAsync(CancellationToken ct);

    Task<List<Notification>> GetTrashPageAsync(Guid actor, DateTime? before, Guid? beforeId, int take, CancellationToken ct);
    Task<Notification?> GetLifecycleNotificationAsync(Guid id, Guid actor, CancellationToken ct);
    Task<int> SetDeletedAsync(Guid id, Guid actor, DateTime? deletedAt, CancellationToken ct);
    Task<int> DeleteReadAsync(Guid actor, DateTime now, CancellationToken ct);
    Task<int> MarkReadAsync(Guid id, Guid actor, DateTime now, CancellationToken ct);

    // Lấy loại hội thoại (Group / Private).
    Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct);

    // Lấy mốc mute của user trong hội thoại.
    Task<DateTime?> GetMutedUntilAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    // Lấy trạng thái user trong hội thoại để cập nhật (bật tracking).
    Task<ConversationUserState?> GetConversationUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    // Thêm mới trạng thái user trong hội thoại.
    void AddConversationUserState(ConversationUserState state);
}
