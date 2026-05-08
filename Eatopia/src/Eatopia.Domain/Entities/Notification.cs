using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = "info";
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
}
