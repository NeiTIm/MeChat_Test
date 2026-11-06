using MeChat.Api.Features.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Admin;

public sealed class ReportEvidenceConfiguration : IEntityTypeConfiguration<ReportEvidence>
{
    public void Configure(EntityTypeBuilder<ReportEvidence> e)
    {
        e.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId);
    }
}
