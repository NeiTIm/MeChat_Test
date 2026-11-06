using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Groups;

[ApiController]
[Authorize]
[Route("api")]
public sealed class GroupInviteLinksController(GroupInviteLinkService links) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("groups/{id:guid}/invite-links")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create(Guid id, CreateGroupLinkRequest input, CancellationToken ct) =>
        await links.CreateAsync(Actor, id, input.ExpiresAt, ct) is { } link ? Ok(link) : NotFound();

    [HttpGet("groups/{id:guid}/invite-links")]
    public async Task<IActionResult> List(Guid id, CancellationToken ct) =>
        await links.ListAsync(Actor, id, ct) is { } list ? Ok(list) : NotFound();

    [HttpDelete("groups/{id:guid}/invite-links/{linkId:guid}")]
    public async Task<IActionResult> Revoke(Guid id, Guid linkId, CancellationToken ct) =>
        await links.RevokeAsync(Actor, id, linkId, ct) ? NoContent() : NotFound();

    [HttpPost("group-invite-links/resolve")]
    public async Task<IActionResult> Resolve(GroupLinkTokenRequest input, CancellationToken ct) =>
        await links.ResolveAsync(Actor, input.Token, ct) is { } preview ? Ok(preview) : NotFound();

    [HttpPost("group-invite-links/join")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Join(JoinGroupLinkRequest input, CancellationToken ct) =>
        await links.JoinAsync(Actor, input.Token, input.Confirm, ct) is { } id ? Ok(new { groupId = id }) : NotFound();
}
