using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Messages;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}")]
public sealed class ConversationStatusController(MessageStatusService statuses) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("delivery-acks")]
    public async Task<IActionResult> Acknowledge(Guid conversationId, DeliveryAcksRequest input, CancellationToken ct) =>
        await statuses.AcknowledgeAsync(Actor, conversationId, input, ct) ? NoContent() : NotFound();

    [HttpPut("read-cursor")]
    public async Task<IActionResult> MarkRead(Guid conversationId, ReadCursorRequest input, CancellationToken ct) =>
        await statuses.MarkReadAsync(Actor, conversationId, input.LastReadSequence, ct) ? NoContent() : NotFound();

    [HttpGet("unread")]
    public async Task<IActionResult> Unread(Guid conversationId, CancellationToken ct) =>
        await statuses.UnreadAsync(Actor, conversationId, ct) is { } unread ? Ok(unread) : NotFound();
}
