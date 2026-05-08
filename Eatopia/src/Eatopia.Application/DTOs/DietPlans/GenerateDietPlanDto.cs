using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.DietPlans;

public class GenerateDietPlanDto
{
    [JsonPropertyName("durationDays")]
    [Range(1, 365)]
    public int DurationDays { get; set; } = 7;

    [JsonPropertyName("caloriesTargetPerDay")]
    [Range(1, 10000)]
    public decimal CaloriesTargetPerDay { get; set; } = 2000;

    [JsonPropertyName("avoidFoods")]
    public List<string>? DislikedFoods { get; set; }

    public List<string>? Allergies { get; set; }

    [MaxLength(200)]
    public string? Goal { get; set; }
}
