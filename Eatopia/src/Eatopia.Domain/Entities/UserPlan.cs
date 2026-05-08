using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class UserPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public DietPlan Plan { get; set; } = null!;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
