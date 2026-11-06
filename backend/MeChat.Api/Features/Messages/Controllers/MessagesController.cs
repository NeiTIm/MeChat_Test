using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Messages;

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController(MessageActionService actions) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        await actions.GetAsync(Actor, id, ct) is { } message ? Ok(message) : NotFound();

    [HttpGet("{id:guid}/context")]
    public async Task<IActionResult> Context(Guid id, CancellationToken ct) =>
        await actions.ContextAsync(Actor, id, ct) is { } context ? Ok(context) : NotFound();

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, EditMessageRequest input, CancellationToken ct) =>
        await actions.EditAsync(Actor, id, input, ct) is { } message ? Ok(message) : NotFound();

    [HttpGet("{id:guid}/edit-history")]
    public async Task<IActionResult> EditHistory(Guid id, CancellationToken ct) =>
        await actions.EditHistoryAsync(Actor, id, ct) is { } versions ? Ok(new { versions }) : NotFound();

    [HttpPost("{id:guid}/recall")]
    public async Task<IActionResult> Recall(Guid id, CancellationToken ct) => await actions.RecallAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}/for-me")]
    public async Task<IActionResult> DeleteForMe(Guid id, CancellationToken ct) => await actions.DeleteForMeAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpPut("{id:guid}/reaction")]
    public async Task<IActionResult> React(Guid id, ReactionRequest input, CancellationToken ct) =>
        await actions.SetReactionAsync(Actor, id, input.Emoji, ct) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}/reaction")]
    public async Task<IActionResult> RemoveReaction(Guid id, CancellationToken ct) =>
        await actions.RemoveReactionAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/forward")]
    [EnableRateLimiting("messages")]
    public async Task<IActionResult> Forward(Guid id, ForwardMessageRequest input, CancellationToken ct) =>
        await actions.ForwardAsync(Actor, id, input, ct) is { } message ? Ok(message) : NotFound();
}
