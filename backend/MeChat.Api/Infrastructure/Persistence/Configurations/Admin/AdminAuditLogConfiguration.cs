using MeChat.Api.Features.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Admin;

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> e)
    {
        e.HasIndex(x => x.CreatedAt);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.AdminUserId);
    }
}
