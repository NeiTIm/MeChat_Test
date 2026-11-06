namespace MeChat.Api.Features.Admin.Models;

public sealed class AccountSuspension
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AdminId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
