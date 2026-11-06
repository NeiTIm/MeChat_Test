using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Moderation;

[ApiController]
[Route("api/appeals")]
public sealed class AppealsController(AppealService appeals) : ControllerBase
{
    [HttpPost("authenticate")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Authenticate(AppealLoginRequest input, CancellationToken ct) =>
        await appeals.AuthenticateAsync(input, ct) is { } token ? Ok(token) : Unauthorized();

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateAppealRequest input, CancellationToken ct)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await appeals.CreateAsync(actor, input.Reason, ct) is { } appeal ? Ok(appeal) : NotFound();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await appeals.MineAsync(actor, ct));
    }
}
