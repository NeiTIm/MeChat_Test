using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeChat.Api.Features.Friends;

namespace MeChat.Api.Features.Conversations;

[ApiController]
[Authorize]
[Route("api/message-requests")]
public sealed class MessageRequestsController(MessageRequestService requests, FriendService friends) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Inbox(CancellationToken ct) => Ok(await requests.InboxAsync(Actor, ct));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) => Ok(new { count = await requests.UnreadCountAsync(Actor, ct) });

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) => await requests.RespondAsync(Actor, id, true, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct) => await requests.RespondAsync(Actor, id, false, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct)
    {
        var request = (await requests.InboxAsync(Actor, ct)).SingleOrDefault(r => r.Id == id);
        return request is not null && await friends.BlockAsync(Actor, request.SenderUserId, ct) ? NoContent() : NotFound();
    }

    [HttpPut("{id:guid}/read-cursor")]
    public async Task<IActionResult> MarkRead(Guid id, RequestReadCursorDto input, CancellationToken ct) =>
        await requests.MarkReadAsync(Actor, id, input.LastReadSequence, ct) ? NoContent() : NotFound();
}
