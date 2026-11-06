
namespace MeChat.Api.Features.Messages;

// =====================================================
// CONVERSATION UNREAD RESPONSE
// =====================================================

public sealed record ConversationUnreadDto(
    Guid ConversationId,
    string Type,
    int Count,
    long LastReadSequence);

// =====================================================
// UNREAD SUMMARY RESPONSE
// =====================================================

public sealed record UnreadSummaryDto(
    int MainCount,
    int RequestCount,
    IReadOnlyList<ConversationUnreadDto> Conversations,
    int NotificationCount);
