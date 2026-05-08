using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class WaterLog : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int AmountMl { get; set; }
    public DateTime LoggedAt { get; set; }
}
