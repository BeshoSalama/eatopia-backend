using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class WaterReminder : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ReminderDate { get; set; }
    public TimeSpan TimeOfDay { get; set; }
    public int AmountMl { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
}
