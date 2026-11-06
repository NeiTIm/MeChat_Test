
namespace MeChat.Api.Features.Moderation;

// =====================================================
// RETENTION MAINTENANCE CONFIGURATION
// =====================================================

public sealed class RetentionMaintenanceOptions
{
    // Enable the scheduled background job.
    public bool RunScheduled { get; set; }

    // Allow permanent deletion of eligible records.
    // Keep disabled by default.
    public bool EnablePurge { get; set; }

    // Authorized administrator executing the purge.
    public Guid OperatorUserId { get; set; }

    // Reference to an independently approved operation.
    public string? ApprovalReference { get; set; }

    // Maximum number of eligible records selected
    // from each dataset per maintenance run.
    public int BatchSize { get; set; } = 100;
}
