using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Groups;

[ApiController]
[Authorize]
[Route("api/groups/{id:guid}")]
public sealed class AdvancedGroupsController(AdvancedGroupService groups) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPatch]
    public async Task<IActionResult> Update(Guid id, UpdateGroupRequest input, CancellationToken ct) =>
        await groups.UpdateProfileAsync(Actor, id, input, ct) ? NoContent() : NotFound();

    [HttpPatch("members/{userId:guid}/role")]
    public async Task<IActionResult> Role(Guid id, Guid userId, ChangeGroupRoleRequest input, CancellationToken ct) =>
        await groups.ChangeRoleAsync(Actor, id, userId, input.Role, ct) ? NoContent() : NotFound();

    [HttpDelete("members/{userId:guid}")]
    public async Task<IActionResult> Remove(Guid id, Guid userId, CancellationToken ct) =>
        await groups.RemoveAsync(Actor, id, userId, ct) ? NoContent() : NotFound();

    [HttpPost("transfer-owner")]
    public async Task<IActionResult> Transfer(Guid id, TransferGroupOwnerRequest input, CancellationToken ct) =>
        await groups.TransferOwnerAsync(Actor, id, input.NewOwnerUserId, ct) ? NoContent() : NotFound();

    [HttpPost("dissolve")]
    public async Task<IActionResult> Dissolve(Guid id, DissolveGroupRequest input, CancellationToken ct) =>
        await groups.DissolveAsync(Actor, id, input.Confirm, ct) ? NoContent() : NotFound();

    [HttpGet("pins")]
    public async Task<IActionResult> Pins(Guid id, CancellationToken ct) =>
        await groups.PinsAsync(Actor, id, ct) is { } pins ? Ok(pins) : NotFound();

    [HttpGet("pins/page")]
    public async Task<IActionResult> PinPage(Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        await groups.PinPageAsync(Actor, id, beforeSequence, limit, ct) is { } pins ? Ok(pins) : NotFound();

    [HttpPost("pins")]
    public async Task<IActionResult> Pin(Guid id, PinGroupMessageRequest input, CancellationToken ct) =>
        await groups.PinAsync(Actor, id, input.MessageId, true, ct) ? NoContent() : NotFound();

    [HttpDelete("pins/{messageId:guid}")]
    public async Task<IActionResult> Unpin(Guid id, Guid messageId, CancellationToken ct) =>
        await groups.PinAsync(Actor, id, messageId, false, ct) ? NoContent() : NotFound();
}
