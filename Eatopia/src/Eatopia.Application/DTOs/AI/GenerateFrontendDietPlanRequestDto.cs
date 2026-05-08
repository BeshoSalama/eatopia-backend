using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.AI;

public class GenerateFrontendDietPlanRequestDto
{
    [JsonPropertyName("generationId")]
    public string? GenerationId { get; set; }

    public int? Age { get; set; }

    [JsonPropertyName("weight")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("height")]
    public decimal? HeightCm { get; set; }

    public string? Goal { get; set; }

    [JsonPropertyName("activityLevel")]
    public string? ActivityLevel { get; set; }

    public List<string>? Allergies { get; set; }

    [JsonPropertyName("avoidFoods")]
    public List<string>? AvoidFoods { get; set; }

    [JsonPropertyName("durationDays")]
    public int DurationDays { get; set; } = 7;

    [JsonPropertyName("mealsPerDay")]
    public List<string>? MealsPerDay { get; set; }

    public FrontendDietPlanPreferencesDto? Preferences { get; set; }
}

public class FrontendDietPlanPreferencesDto
{
    public string? Goal { get; set; }
    public string? Language { get; set; }
}
