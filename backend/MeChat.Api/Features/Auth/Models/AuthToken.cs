namespace MeChat.Api.Features.Auth.Models;

public sealed class AuthToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string? PendingEmail { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
