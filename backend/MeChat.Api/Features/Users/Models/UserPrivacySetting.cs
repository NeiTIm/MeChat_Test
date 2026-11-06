namespace MeChat.Api.Features.Users.Models;

public sealed class UserPrivacySetting
{
    public Guid UserId { get; set; }
    public bool ShowOnline { get; set; } = true;
    public bool ShowLastSeen { get; set; } = true;
    public bool AllowStrangerMessages { get; set; } = true;
    public bool AllowGroupInvites { get; set; } = true;
}
