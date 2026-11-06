using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Conversations;

public sealed record NewMessageRequest(bool Confirm);

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController(PrivateChatService chats, MeChat.Api.Features.Messages.MessageActionService actions) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await chats.ListAsync(Actor, ct));

    [HttpPost("private")]
    public async Task<IActionResult> Open(CreatePrivateConversationRequest request, CancellationToken ct) =>
        await chats.OpenAsync(Actor, request.TargetUserId, ct) is { } conversation ? Ok(conversation) : NotFound();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        await chats.GetAsync(Actor, id, ct) is { } conversation ? Ok(conversation) : NotFound();

    [HttpPost("{id:guid}/message-request")]
    [EnableRateLimiting("messages")]
    public async Task<IActionResult> NewRequest(Guid id, NewMessageRequest request, CancellationToken ct) =>
        await chats.NewRequestAsync(Actor, id, request.Confirm, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/messages")]
    [EnableRateLimiting("messages")]
    public async Task<IActionResult> Send(Guid id, SendMessageRequest request, CancellationToken ct) =>
        await chats.SendAsync(Actor, id, request, ct) is { } message ? Ok(message) : NotFound();

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        await chats.HistoryAsync(Actor, id, beforeSequence, limit, ct) is { } page ? Ok(page) : NotFound();

    [HttpDelete("{id:guid}/for-me")]
    public async Task<IActionResult> HideForMe(Guid id, CancellationToken ct) =>
        await chats.HideForMeAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpGet("{id:guid}/messages/search")]
    public async Task<IActionResult> Search(Guid id, [FromQuery] string q, [FromQuery] long? beforeSequence, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        await actions.SearchAsync(Actor, id, q, beforeSequence, limit, ct) is { } page ? Ok(page) : NotFound();
}
