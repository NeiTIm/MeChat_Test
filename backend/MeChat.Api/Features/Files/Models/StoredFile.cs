namespace MeChat.Api.Features.Files.Models;

public sealed class StoredFile
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string Purpose { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
