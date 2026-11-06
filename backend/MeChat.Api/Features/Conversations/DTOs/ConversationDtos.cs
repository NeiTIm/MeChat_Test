
using System.ComponentModel.DataAnnotations;
using MeChat.Api.Features.Files;

namespace MeChat.Api.Features.Conversations;

// =====================================================
// CREATE PRIVATE CONVERSATION REQUEST
// =====================================================

public sealed record CreatePrivateConversationRequest(
    [Required]
    Guid TargetUserId);

// =====================================================
// CONVERSATION RESPONSE
// =====================================================

public sealed record ConversationDto(
    Guid Id,
    string Type,
    string Status,
    Guid OtherUserId,
    long LastSequence,
    DateTime UpdatedAt);

// =====================================================
// SEND MESSAGE REQUEST
// =====================================================

public sealed record SendMessageRequest(
    [Required]
    Guid ClientMessageId,

    [MaxLength(10000)]
    string? Content,

    Guid? ReplyToMessageId = null,

    IReadOnlyList<Guid>? AttachmentIds = null);

// =====================================================
// MESSAGE RESPONSE
// =====================================================

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid ClientMessageId,
    long Sequence,
    string? Content,
    DateTime CreatedAt,
    bool Recalled,

    DateTime? EditedAt = null,

    bool IsForwarded = false,

    Guid? ReplyToMessageId = null,

    IReadOnlyList<AttachmentDto>? Attachments = null);

// =====================================================
// MESSAGE PAGINATION RESPONSE
// =====================================================

public sealed record MessagePageDto(
    IReadOnlyList<MessageDto> Items,
    long? NextCursor,
    bool HasMore);
