using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Users;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(UserService users) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        await users.GetMeAsync(Actor, ct) is { } profile ? Ok(profile) : NotFound();

    [HttpPatch("me")]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct) =>
        await users.UpdateProfileAsync(Actor, request, ct) is { } profile ? Ok(profile) : BadRequest();

    [HttpPatch("me/username")]
    public async Task<IActionResult> Username(ChangeUsernameRequest request, CancellationToken ct) =>
        await users.ChangeUsernameAsync(Actor, request.NewUsername, ct) ? NoContent() : Conflict(new { code = "USERNAME_UNAVAILABLE_OR_COOLDOWN" });

    [HttpPost("me/email-change")]
    public async Task<IActionResult> EmailChange(ChangeEmailRequest request, CancellationToken ct) =>
        await users.RequestEmailChangeAsync(Actor, request.NewEmail, ct) ? Accepted() : Conflict(new { code = "EMAIL_UNAVAILABLE" });

    [HttpPost("me/confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailRequest request, CancellationToken ct) =>
        await users.ConfirmEmailChangeAsync(request.Token, Actor, ct) ? NoContent() : BadRequest(new { code = "INVALID_TOKEN" });

    [HttpGet("me/privacy")]
    public async Task<IActionResult> Privacy(CancellationToken ct) =>
        await users.GetPrivacyAsync(Actor, ct) is { } privacy ? Ok(privacy) : NotFound();

    [HttpPatch("me/privacy")]
    public async Task<IActionResult> UpdatePrivacy(PrivacyDto request, CancellationToken ct) =>
        await users.UpdatePrivacyAsync(Actor, request, ct) is { } privacy ? Ok(privacy) : NotFound();

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] string? cursor, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        Ok(await users.SearchAsync(query, cursor, limit, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Public(Guid id, CancellationToken ct) =>
        await users.GetPublicAsync(id, ct) is { } profile ? Ok(profile) : NotFound();
}
