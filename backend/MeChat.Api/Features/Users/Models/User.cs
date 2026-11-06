namespace MeChat.Api.Features.Users.Models;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string Username { get; set; } = "";
    public string NormalizedUsername { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Bio { get; set; }
    public Guid? AvatarFileId { get; set; }
    public string SystemRole { get; set; } = "User";
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastUsernameChangedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
