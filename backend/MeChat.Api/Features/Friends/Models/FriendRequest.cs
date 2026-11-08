namespace MeChat.Api.Features.Friends.Models;

public sealed class FriendRequest
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public Guid PairUserId1 { get; set; }
    public Guid PairUserId2 { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? SeenAt { get; set; }
}
