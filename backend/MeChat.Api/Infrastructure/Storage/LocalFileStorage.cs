namespace MeChat.Api.Features.Files;

public interface IFileStorage
{
    Task SaveAsync(string key, Stream source, long maximumBytes, CancellationToken ct);
    Stream OpenRead(string key);
    void Delete(string key);
}

public sealed class LocalFileStorage(IHostEnvironment environment) : IFileStorage
{
    private readonly string root = Path.Combine(environment.ContentRootPath, "private-uploads", "files");

    private string PathFor(string key)
    {
        if (key.Length != 32 || !key.All(Uri.IsHexDigit)) throw new InvalidOperationException("Invalid storage key.");
        return Path.Combine(root, key);
    }

    public async Task SaveAsync(string key, Stream source, long maximumBytes, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        var path = PathFor(key);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        var buffer = new byte[81920];
        long total = 0;
        try
        {
            int count;
            while ((count = await source.ReadAsync(buffer, ct)) > 0)
            {
                total += count;
                if (total > maximumBytes) throw new InvalidDataException("File is too large.");
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            if (total == 0) throw new InvalidDataException("Empty file.");
        }
        catch { output.Close(); File.Delete(path); throw; }
    }

    public Stream OpenRead(string key) => new FileStream(PathFor(key), FileMode.Open, FileAccess.Read, FileShare.Read);
    public void Delete(string key) { var path = PathFor(key); if (File.Exists(path)) File.Delete(path); }
}
