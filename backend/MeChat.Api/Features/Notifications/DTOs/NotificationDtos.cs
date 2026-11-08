
namespace MeChat.Api.Features.Notifications;

// =====================================================
// NOTIFICATION RESPONSE
// =====================================================

public sealed record NotificationDto(
    Guid Id,
    string Type,
    Guid? ReferenceId,
    DateTime CreatedAt,
    DateTime? ReadAt,
    string? Text = null);

// =====================================================
// NOTIFICATION PAGE RESPONSE
// =====================================================

public sealed record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    DateTime? NextCursor,
    bool HasMore,
    Guid? NextCursorId = null);

// =====================================================
// NOTIFICATION UNREAD COUNT
// =====================================================

public sealed record NotificationCountDto(
    int Count);

public sealed record TrashedNotificationDto(
    Guid Id, string Type, Guid? ReferenceId, DateTime CreatedAt,
    DateTime? ReadAt, DateTime DeletedAt, string? Text = null);

public sealed record TrashedNotificationPageDto(
    IReadOnlyList<TrashedNotificationDto> Items, DateTime? NextCursor,
    bool HasMore, Guid? NextCursorId = null);

// =====================================================
// CONVERSATION SETTINGS
// =====================================================

public sealed record ConversationSettingsDto(
    DateTime? MutedUntil);
