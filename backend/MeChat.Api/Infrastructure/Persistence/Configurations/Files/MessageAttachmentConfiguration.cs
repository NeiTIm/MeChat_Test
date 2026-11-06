using MeChat.Api.Features.Files.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Files;

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> e)
    {
        e.HasIndex(x => new { x.MessageId, x.DisplayOrder }).IsUnique();
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
        e.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId);
    }
}
