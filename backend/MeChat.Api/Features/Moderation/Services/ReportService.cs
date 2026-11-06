
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation;

public sealed class ReportService(
    IReportRepository repository,
    GroupService groups,
    MessageActionService messages,
    TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // CREATE REPORT
    // =====================================================

    public Task<ReportDto?> CreateAsync(
        Guid actor,
        CreateReportRequest input,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE REASON
        // =================================================

        var reason = input.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 1000 ||
            input.TargetId == Guid.Empty)
        {
            return Task.FromResult<ReportDto?>(null);
        }

        // =================================================
        // SERIALIZABLE TRANSACTION
        // =================================================

        return repository.ExecuteAtomicAsync<ReportDto>(
            async () =>
            {
                Message? message = null;

                // =============================================
                // REPORT MESSAGE
                // =============================================

                if (input.TargetType == "Message")
                {
                    message =
                        await repository.GetUnrecalledMessageAsync(
                            input.TargetId,
                            ct);

                    if (message is null)
                    {
                        return null;
                    }

                    // Only users who can currently view the
                    // message may report it.
                    var canView =
                        await messages.CanViewAttachmentAsync(
                            actor,
                            message.Id,
                            ct);

                    if (!canView)
                    {
                        return null;
                    }
                }

                // =============================================
                // REPORT GROUP
                // =============================================

                else if (input.TargetType == "Group")
                {
                    var canRead =
                        await groups.CanReadAsync(
                            actor,
                            input.TargetId,
                            ct);

                    if (!canRead)
                    {
                        return null;
                    }
                }

                // =============================================
                // REPORT USER
                // =============================================

                else if (input.TargetType == "User")
                {
                    // User cannot report themselves.
                    if (actor == input.TargetId)
                    {
                        return null;
                    }

                    var targetExists =
                        await repository.IsVerifiedUserAsync(
                            input.TargetId,
                            ct);

                    if (!targetExists)
                    {
                        return null;
                    }
                }

                // =============================================
                // INVALID TARGET TYPE
                // =============================================

                else
                {
                    return null;
                }

                // =============================================
                // CREATE REPORT
                // =============================================

                var now = Now;

                var report = new Report
                {
                    Id = Guid.NewGuid(),
                    ReporterUserId = actor,
                    TargetType = input.TargetType,
                    TargetId = input.TargetId,
                    Reason = reason,
                    Status = "Pending",
                    CreatedAt = now
                };

                repository.AddReport(report);

                // =============================================
                // CAPTURE MESSAGE EVIDENCE
                // =============================================

                if (message is not null)
                {
                    // -----------------------------------------
                    // CONTENT SNAPSHOT
                    // -----------------------------------------

                    // Preserve a copy of the message content
                    // at the time the report was created.

                    repository.AddEvidence(
                        new ReportEvidence
                        {
                            Id = Guid.NewGuid(),
                            ReportId = report.Id,
                            SourceMessageId = message.Id,
                            EvidenceContent = message.Content,
                            CapturedAt = now
                        });

                    // -----------------------------------------
                    // ATTACHMENT REFERENCES
                    // -----------------------------------------

                    var attachmentIds =
                        await repository
                            .GetMessageAttachmentFileIdsAsync(
                                message.Id,
                                ct);

                    var attachmentEvidence =
                        new List<ReportEvidence>();

                    foreach (var fileId in attachmentIds)
                    {
                        attachmentEvidence.Add(
                            new ReportEvidence
                            {
                                Id = Guid.NewGuid(),
                                ReportId = report.Id,
                                SourceMessageId = message.Id,
                                StoredFileId = fileId,
                                CapturedAt = now
                            });
                    }

                    repository.AddEvidenceRange(
                        attachmentEvidence);
                }

                // =============================================
                // SAVE REPORT + EVIDENCE ATOMICALLY
                // =============================================

                await repository.SaveChangesAsync(ct);

                // =============================================
                // MAP RESPONSE
                // =============================================

                return Map(report);
            },
            ct);
    }

    // =====================================================
    // GET MY REPORTS
    // =====================================================

    public Task<List<ReportDto>> MineAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetUserReportsAsync(
            actor,
            ct);
    }

    // =====================================================
    // MAP REPORT ENTITY TO DTO
    // =====================================================

    private static ReportDto Map(
        Report report)
    {
        return new ReportDto(
            report.Id,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Status,
            report.CreatedAt,
            report.ReviewedAt);
    }
}
