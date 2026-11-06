using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MeChat.Api.Features.Conversations;

namespace MeChat.Api.Features.Groups;

[ApiController]
[Authorize]
[Route("api")]
public sealed class GroupsController(GroupService groups) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpDelete("groups/{id:guid}/for-me")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct) =>
        await groups.HideForMeAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpGet("groups")]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await groups.ListAsync(Actor, ct));

    [HttpPost("groups")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create(CreateGroupRequest input, CancellationToken ct) =>
        await groups.CreateAsync(Actor, input, ct) is { } group ? Ok(group) : NotFound();

    [HttpGet("groups/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        await groups.GetAsync(Actor, id, ct) is { } group ? Ok(group) : NotFound();

    [HttpGet("groups/{id:guid}/members")]
    public async Task<IActionResult> Members(Guid id, CancellationToken ct) =>
        await groups.MembersAsync(Actor, id, ct) is { } members ? Ok(members) : NotFound();

    [HttpPost("groups/{id:guid}/invitations")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Invite(Guid id, InviteGroupRequest input, CancellationToken ct) =>
        await groups.InviteAsync(Actor, id, input.InviteeUserId, ct) ? NoContent() : NotFound();

    [HttpGet("group-invitations")]
    public async Task<IActionResult> Invitations(CancellationToken ct) => Ok(await groups.InvitationsAsync(Actor, ct));

    [HttpPost("group-invitations/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) =>
        await groups.RespondInvitationAsync(Actor, id, true, ct) ? NoContent() : NotFound();

    [HttpPost("group-invitations/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct) =>
        await groups.RespondInvitationAsync(Actor, id, false, ct) ? NoContent() : NotFound();

    [HttpPost("groups/{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) =>
        await groups.LeaveAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpGet("groups/{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        await groups.HistoryAsync(Actor, id, beforeSequence, limit, ct) is { } page ? Ok(page) : NotFound();

    [HttpPost("groups/{id:guid}/messages")]
    [EnableRateLimiting("messages")]
    public async Task<IActionResult> Send(Guid id, SendMessageRequest input, CancellationToken ct) =>
        await groups.SendAsync(Actor, id, input, ct) is { } message ? Ok(message) : NotFound();
}
