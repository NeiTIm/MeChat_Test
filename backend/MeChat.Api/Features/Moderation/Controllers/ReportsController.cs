using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Moderation;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create(CreateReportRequest input, CancellationToken ct) =>
        await reports.CreateAsync(Actor, input, ct) is { } report ? Ok(report) : NotFound();

    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await reports.MineAsync(Actor, ct));
}
