using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeChat.Api.Features.Messages;

[ApiController]
[Authorize]
[Route("api/unread")]
public sealed class UnreadController(UnreadSummaryService summary) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await summary.GetAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct));
}
