using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MeChat.Api.Features.Files;

namespace MeChat.Tests;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task Owned_synthetic_blob_delete_is_idempotent_after_a_retry()
    {
        var storage = new LocalFileStorage(new TestEnvironment());
        var key = Guid.NewGuid().ToString("N");
        await using var input = new MemoryStream([1, 2, 3]);
        await storage.SaveAsync(key, input, 3, CancellationToken.None);
        using (var read = storage.OpenRead(key)) Assert.Equal(3, read.Length);
        storage.Delete(key);
        storage.Delete(key);
        Assert.Throws<FileNotFoundException>(() => storage.OpenRead(key));
    }

    [Fact]
    public async Task Oversized_upload_removes_only_its_new_partial_blob_and_rejects_path_keys()
    {
        var storage = new LocalFileStorage(new TestEnvironment());
        var key = Guid.NewGuid().ToString("N");
        await using var input = new MemoryStream([1, 2]);
        await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(key, input, 1, CancellationToken.None));
        Assert.Throws<FileNotFoundException>(() => storage.OpenRead(key));
        Assert.Throws<InvalidOperationException>(() => storage.OpenRead("../outside"));
        Assert.Throws<InvalidOperationException>(() => storage.Delete("../outside"));
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "MeChat.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "private-uploads", $"storage-test-{Guid.NewGuid():N}");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
