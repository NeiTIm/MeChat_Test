
namespace MeChat.Api.Features.Messages;

// =====================================================
// DELIVERY ACKNOWLEDGEMENTS
// =====================================================

public sealed record DeliveryAcksRequest(
    IReadOnlyList<Guid> MessageIds);

// =====================================================
// READ CURSOR REQUEST
// =====================================================

public sealed record ReadCursorRequest(
    long LastReadSequence);

// =====================================================
// UNREAD RESPONSE
// =====================================================

public sealed record UnreadDto(
    int Count,
    long LastReadSequence);

// =====================================================
// MESSAGE RECEIPT RESPONSE
// =====================================================

public sealed record MessageReceiptDto(
    string Status,
    int DeliveredCount,
    int SeenCount);
