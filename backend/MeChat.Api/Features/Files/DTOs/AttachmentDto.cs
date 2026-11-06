
namespace MeChat.Api.Features.Files;

// =====================================================
// MESSAGE ATTACHMENT RESPONSE
// =====================================================

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);
