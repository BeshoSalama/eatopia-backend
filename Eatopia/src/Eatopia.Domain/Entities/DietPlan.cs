using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class DietPlan : BaseEntity
{
    public string Title { get; set; } = null!;

    public decimal? CaloriesTargetPerDay { get; set; }
    public int DurationDays { get; set; }

    public Guid CreatedBy { get; set; }
    public User Creator { get; set; } = null!;

    public ICollection<DietPlanItem> Items { get; set; } = new List<DietPlanItem>();
}
