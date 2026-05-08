using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.AI;

public class FrontendDietPlanResponseDto
{
    [JsonPropertyName("weeklyPlan")]
    public List<FrontendDietPlanDayDto> WeeklyPlan { get; set; } = new();

    [JsonPropertyName("targetMacros")]
    public FrontendDietPlanMacrosDto? TargetMacros { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("weightForecast")]
    public List<FrontendWeightForecastWeekDto> WeightForecast { get; set; } = new();
}

public class FrontendDietPlanDayDto
{
    public int Day { get; set; }
    public FrontendDietPlanMealsDto Meals { get; set; } = new();
}

public class FrontendDietPlanMealsDto
{
    public FrontendDietMealDto Breakfast { get; set; } = new() { Title = "Breakfast" };
    public FrontendDietMealDto Lunch { get; set; } = new() { Title = "Lunch" };
    public FrontendDietMealDto Dinner { get; set; } = new() { Title = "Dinner" };
    public FrontendDietMealDto Snacks { get; set; } = new() { Title = "Snacks" };
}

public class FrontendDietMealDto
{
    public string Title { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string? RecipeName { get; set; }
    public string? RecipeSearch { get; set; }
    public int? Calories { get; set; }
    public int? Protein { get; set; }
    public int? Carbs { get; set; }
    public int? Fat { get; set; }
    public int? QuantityGrams { get; set; }
}

public class FrontendDietPlanMacrosDto
{
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fat { get; set; }
}

public class FrontendWeightForecastWeekDto
{
    public int Week { get; set; }
    public decimal ExpectedWeightKg { get; set; }
    public decimal WeeklyChangeKg { get; set; }
    public decimal TotalChangeKg { get; set; }
    public decimal ExpectedLossKg { get; set; }
    public string Direction { get; set; } = "stable";
}
