using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(AuthService auth, IHostEnvironment environment) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (!await auth.RegisterAsync(request, ct)) return Conflict(new { code = "REGISTRATION_UNAVAILABLE" });
        return StatusCode(201, new GenericResponse("Check the development mail pickup for verification."));
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(TokenRequest request, CancellationToken ct) =>
        await auth.VerifyEmailAsync(request.Token, ct) ? NoContent() : BadRequest(new { code = "INVALID_TOKEN" });

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(EmailRequest request, CancellationToken ct)
    {
        await auth.ResendVerificationAsync(request.Email, ct);
        return Ok(new GenericResponse("If the address needs verification, a message was prepared."));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(EmailRequest request, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(request.Email, ct);
        return Ok(new GenericResponse("If the account exists, password reset instructions were prepared."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct) =>
        await auth.ResetPasswordAsync(request, ct) ? NoContent() : BadRequest(new { code = "INVALID_TOKEN" });

    [HttpPost("reactivate")]
    public async Task<IActionResult> Reactivate(ReactivateRequest request, CancellationToken ct) =>
        await auth.ReactivateAsync(request, ct) ? NoContent() : Unauthorized(new { code = "INVALID_CREDENTIALS_OR_ACCOUNT_STATE" });

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        if (result is null) return Unauthorized(new { code = "INVALID_CREDENTIALS_OR_ACCOUNT_STATE" });
        SetRefreshCookies(result.Value.Refresh);
        return Ok(result.Value.Access);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!ValidCsrf() || !Request.Cookies.TryGetValue("webchat_refresh", out var raw) || string.IsNullOrEmpty(raw))
            return Unauthorized(new { code = "INVALID_REFRESH" });
        var result = await auth.RefreshAsync(raw, ct);
        if (result is null) { ClearRefreshCookies(); return Unauthorized(new { code = "INVALID_REFRESH" }); }
        SetRefreshCookies(result.Value.Refresh);
        return Ok(result.Value.Access);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!ValidCsrf()) return Forbid();
        var (userId, sessionId) = Actor();
        await auth.LogoutAsync(userId, sessionId, ct);
        ClearRefreshCookies();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-others")]
    public async Task<IActionResult> LogoutOthers(CancellationToken ct)
    {
        var (userId, sessionId) = Actor();
        await auth.LogoutOthersAsync(userId, sessionId, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var (userId, sessionId) = Actor();
        return await auth.ChangePasswordAsync(userId, sessionId, request, ct)
            ? NoContent() : BadRequest(new { code = "INVALID_CURRENT_PASSWORD" });
    }

    [Authorize]
    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate(DeactivateRequest request, CancellationToken ct)
    {
        var (userId, _) = Actor();
        return await auth.DeactivateAsync(userId, request.Password, ct)
            ? NoContent() : BadRequest(new { code = "INVALID_CURRENT_PASSWORD" });
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken ct)
    {
        var (userId, sessionId) = Actor();
        return Ok(await auth.SessionsAsync(userId, sessionId, ct));
    }

    [Authorize]
    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        var (userId, _) = Actor();
        return await auth.RevokeSessionAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    private (Guid UserId, Guid SessionId) Actor() =>
        (Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Guid.Parse(User.FindFirstValue("sid")!));

    private bool ValidCsrf() => Request.Cookies.TryGetValue("webchat_csrf", out var cookie)
        && Request.Headers.TryGetValue("X-CSRF-Token", out var header)
        && !string.IsNullOrEmpty(cookie) && cookie == header.ToString();

    private void SetRefreshCookies(string refresh)
    {
        var secure = !environment.IsDevelopment();
        Response.Cookies.Append("webchat_refresh", refresh, new CookieOptions { HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(30), Path = "/api/auth" });
        Response.Cookies.Append("webchat_csrf", Convert.ToHexString(RandomNumberGenerator.GetBytes(24)), new CookieOptions { HttpOnly = false, Secure = secure, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(30), Path = "/" });
    }

    private void ClearRefreshCookies()
    {
        Response.Cookies.Delete("webchat_refresh", new CookieOptions { Path = "/api/auth" });
        Response.Cookies.Delete("webchat_csrf", new CookieOptions { Path = "/" });
    }
}
