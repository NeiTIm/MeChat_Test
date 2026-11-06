namespace MeChat.Api.Features.Conversations.Models;

public sealed class PrivateChatAccess
{
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public string Status { get; set; } = "None";
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
