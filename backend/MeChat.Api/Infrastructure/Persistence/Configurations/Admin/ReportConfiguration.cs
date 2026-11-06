using MeChat.Api.Features.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Admin;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> e)
    {
        e.HasIndex(x => new { x.Status, x.CreatedAt });
        e.HasOne<User>().WithMany().HasForeignKey(x => x.ReporterUserId);
    }
}
