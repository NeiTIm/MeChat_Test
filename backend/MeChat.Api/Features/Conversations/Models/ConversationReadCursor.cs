namespace MeChat.Api.Features.Conversations.Models;

public sealed class ConversationReadCursor
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public long LastReadSequence { get; set; }
    public DateTime UpdatedAt { get; set; }
}
