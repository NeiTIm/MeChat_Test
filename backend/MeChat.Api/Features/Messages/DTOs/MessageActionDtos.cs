
namespace MeChat.Api.Features.Messages;

// =====================================================
// EDIT MESSAGE
// =====================================================

public sealed record EditMessageRequest(
    string Content);

// =====================================================
// MESSAGE REACTION
// =====================================================

public sealed record ReactionRequest(
    string Emoji);

// =====================================================
// FORWARD MESSAGE
// =====================================================

public sealed record ForwardMessageRequest(
    Guid DestinationConversationId,
    Guid ClientMessageId);

// =====================================================
// MESSAGE EDIT HISTORY
// =====================================================

public sealed record EditVersionDto(
    int Version,
    string Content,
    DateTime EditedAt);

// =====================================================
// REPLY PREVIEW
// =====================================================

public sealed record ReplyPreviewDto(
    bool Available,
    string? Content);

// =====================================================
// REACTION COUNT
// =====================================================

public sealed record ReactionCountDto(
    string Emoji,
    int Count);

// =====================================================
// MESSAGE CONTEXT
// =====================================================

public sealed record MessageContextDto(
    ReplyPreviewDto? Reply,
    IReadOnlyList<ReactionCountDto> Reactions,
    string? MyReaction);
