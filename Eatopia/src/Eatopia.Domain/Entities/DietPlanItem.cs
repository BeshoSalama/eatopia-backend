using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class DietPlanItem : BaseEntity
{
    public Guid PlanId { get; set; }
    public DietPlan Plan { get; set; } = null!;

    public int DayOfWeek { get; set; } // 1..7
    public string MealType { get; set; } = null!; // Breakfast/Lunch/Dinner/Snack
    public string Title { get; set; } = null!;

    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public decimal? CaloriesEstimated { get; set; }
}
