namespace MeChat.Api.Features.Groups.Models;

public sealed class GroupInviteLink
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid CreatedByMembershipId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
