
namespace MeChat.Api.Features.Files;

public sealed class FileRetentionOptions
{
    // Enable the hourly background job.
    public bool RunScheduled { get; set; }

    // Explicit approval required for physical deletion.
    public bool EnableDeletion { get; set; }

    public string? ApprovalReference { get; set; }

    // Retention periods.
    public int PendingHours { get; set; } = 24;

    public int RecalledDays { get; set; } = 30;

    public int AvatarDays { get; set; } = 30;

    // Maximum files processed per batch.
    public int BatchSize { get; set; } = 100;
}
