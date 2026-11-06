namespace MeChat.Api.Features.Conversations.Models;

public sealed class ConversationMember
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member";
    public long JoinSequence { get; set; }
    public long? LeaveSequence { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public string? LeftReason { get; set; }
}
