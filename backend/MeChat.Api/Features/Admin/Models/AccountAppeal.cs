namespace MeChat.Api.Features.Admin.Models;

public sealed class AccountAppeal
{
    public Guid Id { get; set; }
    public Guid SuspensionId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
