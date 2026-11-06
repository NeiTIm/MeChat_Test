
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Notifications.Repositories.Interfaces;

public interface INotificationRepository
{
    // =====================================================
    // NOTIFICATION QUERIES
    // =====================================================

    Task<List<Notification>> GetNotificationPageAsync(
        Guid userId,
        DateTime? before,
        Guid? beforeId,
        int take,
        CancellationToken ct);

    Task<List<Notification>> GetUnreadNotificationsAsync(
        Guid userId,
        CancellationToken ct);

    Task<Notification?> GetNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // ADMIN WARNING DETAILS
    // =====================================================

    Task<string?> GetAdminWarningTextAsync(
        Guid auditLogId,
        Guid recipientUserId,
        CancellationToken ct);

    // =====================================================
    // READ STATE
    // =====================================================

    Task<int> MarkAllReadAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct);

    // =====================================================
    // CONVERSATION SETTINGS
    // =====================================================

    Task<DateTime?> GetMutedUntilAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    Task<ConversationUserState?> GetConversationUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    void AddConversationUserState(
        ConversationUserState state);
}
