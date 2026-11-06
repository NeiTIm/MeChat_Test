using System.Text.Json;

namespace MeChat.Api.Features.Auth;

public interface IAuthMailSender
{
    Task SendTokenAsync(string email, string token, string purpose, CancellationToken cancellationToken);
}

public sealed class DevMailSink(IHostEnvironment environment) : IAuthMailSender
{
    public async Task SendTokenAsync(string email, string token, string purpose, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Configure an email provider outside Development.");
        var directory = Path.Combine(environment.ContentRootPath, "private-uploads", "dev-mail");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { email, token, purpose }), cancellationToken);
    }
}
