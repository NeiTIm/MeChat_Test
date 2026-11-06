namespace MeChat.Api.Features.Messages.Models;

public sealed class MessageReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
