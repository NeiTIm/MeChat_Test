using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Notifications;

[ApiController]
[Authorize]
[Route("api")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("notifications")]
    public async Task<IActionResult> List([FromQuery] DateTime? before, [FromQuery] int limit = 30, CancellationToken ct = default, [FromQuery] Guid? beforeId = null) =>
        Ok(await notifications.ListAsync(Actor, before, limit, ct, beforeId));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> Unread(CancellationToken ct) => Ok(await notifications.UnreadAsync(Actor, ct));

    [HttpPatch("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) =>
        await notifications.MarkReadAsync(Actor, id, ct) ? NoContent() : NotFound();

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { await notifications.MarkAllReadAsync(Actor, ct); return NoContent(); }

    [HttpGet("conversations/{id:guid}/settings")]
    public async Task<IActionResult> Settings(Guid id, CancellationToken ct) =>
        await notifications.SettingsAsync(Actor, id, ct) is { } value ? Ok(value) : NotFound();

    [HttpPatch("conversations/{id:guid}/settings")]
    public async Task<IActionResult> Mute(Guid id, ConversationSettingsDto input, CancellationToken ct) =>
        await notifications.SetMuteAsync(Actor, id, input.MutedUntil, ct) ? NoContent() : NotFound();
}
