using System.ComponentModel.DataAnnotations;

namespace MeChat.Api.Features.Users;

public sealed record PrivateProfileDto(Guid Id, string Email, string Username, string DisplayName, string? Bio, Guid? AvatarFileId, string SystemRole, DateTime CreatedAt, DateTime? LastUsernameChangedAt);
public sealed record PublicProfileDto(Guid Id, string Username, string DisplayName, string? Bio, Guid? AvatarFileId);
public sealed record UpdateProfileRequest([Required, MaxLength(100)] string DisplayName, [MaxLength(500)] string? Bio);
public sealed record ChangeUsernameRequest([Required, MinLength(3), MaxLength(50)] string NewUsername);
public sealed record ChangeEmailRequest([Required, EmailAddress] string NewEmail);
public sealed record ConfirmEmailRequest([Required] string Token);
public sealed record PrivacyDto(bool ShowOnline, bool ShowLastSeen, bool AllowStrangerMessages, bool AllowGroupInvites);
public sealed record SearchPageDto(IReadOnlyList<PublicProfileDto> Items, string? NextCursor, bool HasMore);
