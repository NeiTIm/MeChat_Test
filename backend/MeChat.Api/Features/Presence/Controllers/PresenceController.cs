using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Presence;

[ApiController]
[Authorize]
[Route("api/users/{id:guid}/presence")]
public sealed class PresenceController(PresenceService presence) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await presence.GetAsync(actor, id, ct) is { } result ? Ok(result) : NotFound();
    }
}
