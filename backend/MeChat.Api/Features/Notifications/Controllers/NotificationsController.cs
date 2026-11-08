using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeChat.Api.Configurations;

namespace MeChat.Api.Features.Notifications;

[ApiController]
[Authorize]
[Route("api")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    // Id của user đang đăng nhập, lấy từ claim NameIdentifier.
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpDelete("notifications/{id:guid}")]
    [BcrFeature(true)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await notifications.SetDeletedAsync(Actor, id, false, ct) ? NoContent() : BcrResponses.Unavailable();

    [HttpDelete("notifications/read")]
    [BcrFeature(true)]
    public async Task<IActionResult> DeleteRead(CancellationToken ct)
    {
        await notifications.DeleteReadAsync(Actor, ct);
        return NoContent();
    }

    [HttpGet("notifications/trash")]
    [BcrFeature(true)]
    [BcrInputValidation("INVALID_NOTIFICATION_CURSOR")]
    public async Task<IActionResult> Trash([FromQuery] DateTime? before, [FromQuery] Guid? beforeId,
        [FromQuery] int limit = 30, CancellationToken ct = default)
    {
        if (before.HasValue != beforeId.HasValue || beforeId == Guid.Empty ||
            Request.Query.ContainsKey("before") != before.HasValue || Request.Query.ContainsKey("beforeId") != beforeId.HasValue)
            return BcrResponses.Invalid("INVALID_NOTIFICATION_CURSOR");
        return Ok(await notifications.TrashAsync(Actor, before, beforeId, limit, ct));
    }

    [HttpPost("notifications/{id:guid}/restore")]
    [BcrFeature(true)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct) =>
        await notifications.SetDeletedAsync(Actor, id, true, ct) ? NoContent() : BcrResponses.Unavailable();

    // Lấy danh sách thông báo, phân trang theo cursor (before + beforeId).
    [HttpGet("notifications")]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? before,
        [FromQuery] int limit = 30,
        [FromQuery] Guid? beforeId = null,
        CancellationToken ct = default)
    {
        var result = await notifications.ListAsync(Actor, before, limit, ct, beforeId);
        return Ok(result);
    }

    // Đếm số thông báo chưa đọc của user hiện tại.
    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> Unread(CancellationToken ct)
    {
        var count = await notifications.UnreadAsync(Actor, ct);
        return Ok(count);
    }

    // Đánh dấu một thông báo là đã đọc; 404 nếu không tìm thấy.
    [HttpPatch("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        var marked = await notifications.MarkReadAsync(Actor, id, ct);
        return marked ? NoContent() : NotFound();
    }

    // Đánh dấu tất cả thông báo của user hiện tại là đã đọc.
    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(Actor, ct);
        return NoContent();
    }

    // Lấy cài đặt của hội thoại (mute); 404 nếu không có quyền.
    [HttpGet("conversations/{id:guid}/settings")]
    public async Task<IActionResult> Settings(Guid id, CancellationToken ct)
    {
        var settings = await notifications.SettingsAsync(Actor, id, ct);
        return settings is null ? NotFound() : Ok(settings);
    }

    // Đặt hoặc huỷ mute cho hội thoại; 404 nếu không có quyền hoặc dữ liệu không hợp lệ.
    [HttpPatch("conversations/{id:guid}/settings")]
    public async Task<IActionResult> Mute(Guid id, ConversationSettingsDto input, CancellationToken ct)
    {
        var updated = await notifications.SetMuteAsync(Actor, id, input.MutedUntil, ct);
        return updated ? NoContent() : NotFound();
    }
}
