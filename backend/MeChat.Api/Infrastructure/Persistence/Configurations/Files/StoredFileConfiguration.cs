using MeChat.Api.Features.Files.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Files;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> e)
    {
        e.HasIndex(x => x.StorageKey).IsUnique();
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId);
    }
}
