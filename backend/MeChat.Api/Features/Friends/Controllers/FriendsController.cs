using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MeChat.Api.Configurations;

namespace MeChat.Api.Features.Friends;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FriendsController(FriendService friends) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("friends/requests/unread-count")]
    [BcrFeature(false)]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) =>
        Ok(new FriendRequestUnreadCountDto(await friends.UnseenCountAsync(Actor, ct)));

    [HttpPost("friends/requests/mark-seen")]
    [BcrFeature(false)]
    [BcrInputValidation("INVALID_REQUEST_IDS")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> MarkSeen(FriendRequestSeenInput input, CancellationToken ct)
    {
        if (!FriendRequestSeenInput.TryNormalize(input.RequestIds, out var ids))
            return BcrResponses.Invalid("INVALID_REQUEST_IDS");
        await friends.MarkSeenAsync(Actor, ids, ct);
        return NoContent();
    }

    [HttpGet("friends")]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await friends.FriendsAsync(Actor, ct));

    [HttpGet("friends/requests")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Requests(CancellationToken ct) => Ok(await friends.RequestsAsync(Actor, ct));

    [HttpPost("friends/requests")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Send(FriendRequestCreate input, CancellationToken ct) =>
        (await friends.SendAsync(Actor, input.TargetUserId, ct)) switch
        {
            FriendActionResult.Created => StatusCode(201),
            FriendActionResult.Accepted => Ok(new { status = "Accepted" }),
            FriendActionResult.Conflict => Conflict(new { code = "REQUEST_OR_FRIENDSHIP_EXISTS" }),
            _ => NotFound()
        };

    [HttpPost("friends/requests/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) =>
        await friends.RespondAsync(Actor, id, true, ct) == FriendActionResult.Accepted ? NoContent() : NotFound();

    [HttpPost("friends/requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct) =>
        await friends.RespondAsync(Actor, id, false, ct) == FriendActionResult.Created ? NoContent() : NotFound();

    [HttpPost("friends/requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        await friends.CancelAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpDelete("friends/{targetUserId:guid}")]
    public async Task<IActionResult> Unfriend(Guid targetUserId, CancellationToken ct) =>
        await friends.UnfriendAsync(Actor, targetUserId, ct) ? NoContent() : NotFound();

    [HttpPost("blocks")]
    public async Task<IActionResult> Block(BlockCreate input, CancellationToken ct) =>
        await friends.BlockAsync(Actor, input.TargetUserId, ct) ? NoContent() : NotFound();

    [HttpGet("blocks")]
    public async Task<IActionResult> Blocks(CancellationToken ct) => Ok(await friends.BlocksAsync(Actor, ct));

    [HttpDelete("blocks/{targetUserId:guid}")]
    public async Task<IActionResult> Unblock(Guid targetUserId, CancellationToken ct) =>
        await friends.UnblockAsync(Actor, targetUserId, ct) ? NoContent() : NotFound();
}
