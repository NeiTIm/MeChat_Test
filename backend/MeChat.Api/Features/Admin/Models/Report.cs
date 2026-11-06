namespace MeChat.Api.Features.Admin.Models;

public sealed class Report
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
