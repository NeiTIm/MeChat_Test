namespace MeChat.Api.Features.Groups.Models;

public sealed class GroupInvitation
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid InviterUserId { get; set; }
    public Guid InviteeUserId { get; set; }
    public Guid? InviteLinkId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
