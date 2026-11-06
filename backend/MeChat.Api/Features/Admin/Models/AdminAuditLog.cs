namespace MeChat.Api.Features.Admin.Models;

public sealed class AdminAuditLog
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string Action { get; set; } = "";
    public string TargetType { get; set; } = "";
    public Guid? TargetId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Details { get; set; }
    public bool LegalHold { get; set; }
}
