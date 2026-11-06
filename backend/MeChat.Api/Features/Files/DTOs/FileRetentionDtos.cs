
namespace MeChat.Api.Features.Files;

// =====================================================
// FILE FACTS FOR RETENTION POLICY
// =====================================================

public sealed record FileRetentionFacts(
    string Status,
    DateTime CreatedAt,
    bool HasAvatar,
    bool HasEvidence,
    int MessageReferences,
    bool HasLiveMessage,
    DateTime? LastRecall);

// =====================================================
// CLEANUP CANDIDATE
// =====================================================

public sealed record FileCleanupCandidate(
    Guid Id,
    string Reason,
    long SizeBytes);

// =====================================================
// CLEANUP PREVIEW
// =====================================================

public sealed record FileCleanupPreview(
    IReadOnlyList<FileCleanupCandidate> Items,
    bool HasMore,
    bool DeletionEnabled);
