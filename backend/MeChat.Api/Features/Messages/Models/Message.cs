namespace MeChat.Api.Features.Messages.Models;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid ClientMessageId { get; set; }
    public long Sequence { get; set; }
    public string Type { get; set; } = "Text";
    public string? Content { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public bool IsForwarded { get; set; }
    public string Status { get; set; } = "Sent";
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? RecalledAt { get; set; }
}
