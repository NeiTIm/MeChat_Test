namespace MeChat.Api.Features.Conversations.Models;

public sealed class PrivateConversationPair
{
    public Guid ConversationId { get; set; }
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
}
