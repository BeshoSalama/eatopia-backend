using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class MealLog : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid FoodId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;

    public string? MealImageUrl { get; set; }
    public DateTime? DetectedAt { get; set; }

    public decimal? QuantityGrams { get; set; }

    public decimal? CalculatedCalories { get; set; }
    public decimal? CalculatedProtein { get; set; }
    public decimal? CalculatedFat { get; set; }
    public decimal? CalculatedCarbs { get; set; }
}
