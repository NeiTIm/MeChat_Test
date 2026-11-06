using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeChat.Api.Features.Files;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController(FileService files) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(26L * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string purpose, CancellationToken ct) =>
        await files.UploadAsync(Actor, file, purpose, ct) is { } result ? Ok(result) : BadRequest();

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct) =>
        await files.ReadAsync(Actor, id, ct) is { } result ? File(result.Content, result.ContentType, enableRangeProcessing: true) : NotFound();

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) =>
        await files.ReadAsync(Actor, id, ct) is { } result ? File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true) : NotFound();
}
