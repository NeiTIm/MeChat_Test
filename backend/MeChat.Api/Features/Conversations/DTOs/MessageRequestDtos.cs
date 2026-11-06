
namespace MeChat.Api.Features.Conversations;

// =====================================================
// MESSAGE REQUEST
// =====================================================

public sealed record MessageRequestDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid ReceiverUserId,
    string Status,
    long LastSequence,
    long RequestReadSequence,
    DateTime CreatedAt);

// =====================================================
// REQUEST READ CURSOR
// =====================================================

public sealed record RequestReadCursorDto(
    long LastReadSequence);
