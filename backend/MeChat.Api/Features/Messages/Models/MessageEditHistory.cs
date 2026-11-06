namespace MeChat.Api.Features.Messages.Models;

public sealed class MessageEditHistory
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public int Version { get; set; }
    public string PreviousContent { get; set; } = "";
    public DateTime EditedAt { get; set; }
}
