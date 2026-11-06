namespace MeChat.Api.Features.Conversations.Models;

public sealed class ConversationUserState
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public long HiddenThroughSequence { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? MutedUntil { get; set; }
    public long RequestReadSequence { get; set; }
    public DateTime? RequestReadAt { get; set; }
}
