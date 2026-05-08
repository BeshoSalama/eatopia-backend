using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class ContentReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public string ContentType { get; set; } = null!;
    public Guid ContentId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public User? ReportedUser { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
}
