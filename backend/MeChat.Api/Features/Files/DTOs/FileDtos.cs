
namespace MeChat.Api.Features.Files;

// =====================================================
// STORED FILE RESPONSE
// =====================================================

public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status);

// =====================================================
// FILE READ RESULT
// =====================================================

public sealed record FileRead(
    Stream Content,
    string ContentType,
    string FileName);
