namespace MeChat.Api.Configurations;

// Deployment switches; schema compatibility filters must never depend on these flags.
public sealed class BcrFeatureOptions
{
    public const string SectionName = "BcrFeatures";
    public bool NotificationLifecycleEnabled { get; set; }
    public bool FriendRequestSeenEnabled { get; set; }
}
