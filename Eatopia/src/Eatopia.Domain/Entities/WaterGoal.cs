using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class WaterGoal : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int DailyTargetMl { get; set; }
    public int RemindEveryMinutes { get; set; }
}
