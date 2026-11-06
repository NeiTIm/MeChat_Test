namespace MeChat.Api.Features.Messages.Models;

public sealed class MessageDelivery
{
    public Guid MessageId { get; set; }
    public Guid RecipientUserId { get; set; }
    public DateTime DeliveredAt { get; set; }
}
