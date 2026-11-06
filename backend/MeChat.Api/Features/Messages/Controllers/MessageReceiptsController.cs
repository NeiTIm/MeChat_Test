using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Messages;

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessageReceiptsController(MessageStatusService status) : ControllerBase
{
    [HttpGet("{id:guid}/receipts")]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct) =>
        await status.ReceiptAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, ct) is { } receipt ? Ok(receipt) : NotFound();
}
