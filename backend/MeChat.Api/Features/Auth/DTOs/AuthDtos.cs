using System.ComponentModel.DataAnnotations;

namespace MeChat.Api.Features.Auth;

public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(3), MaxLength(50)] string Username, [Required, MinLength(12)] string Password);
public sealed record TokenRequest([Required] string Token);
public sealed record LoginRequest([Required] string Identifier, [Required] string Password, string? DeviceName);
public sealed record AccessResponse(string AccessToken, DateTime ExpiresAt, Guid UserId, string Username);
public sealed record SessionResponse(Guid Id, string DeviceName, DateTime CreatedAt, DateTime LastActivityAt, bool IsCurrent);
public sealed record GenericResponse(string Message);
public sealed record EmailRequest([Required, EmailAddress] string Email);
public sealed record ResetPasswordRequest([Required] string Token, [Required, MinLength(12)] string NewPassword);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(12)] string NewPassword);
public sealed record ReactivateRequest([Required] string Identifier, [Required] string Password);
public sealed record DeactivateRequest([Required] string Password);
