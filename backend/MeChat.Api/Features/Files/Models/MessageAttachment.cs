namespace MeChat.Api.Features.Files.Models;

public sealed class MessageAttachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid StoredFileId { get; set; }
    public int DisplayOrder { get; set; }
}
