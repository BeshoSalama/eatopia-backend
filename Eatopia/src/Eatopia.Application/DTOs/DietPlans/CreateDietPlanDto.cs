using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.DietPlans;

public class CreateDietPlanDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [Range(1, 10000)]
    [JsonPropertyName("caloriesTargetPerDay")]
    public decimal? CaloriesTargetPerDay { get; set; }

    [Range(1, 365)]
    [JsonPropertyName("durationDays")]
    public int DurationDays { get; set; }
}
