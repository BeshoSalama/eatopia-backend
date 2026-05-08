using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Food;

public class CreateFoodItemDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [Range(0, 10000)]
    [JsonPropertyName("caloriesPer100g")]
    public decimal CaloriesPer100g { get; set; }

    [Range(0, 1000)]
    [JsonPropertyName("proteinPer100g")]
    public decimal ProteinPer100g { get; set; }

    [Range(0, 1000)]
    [JsonPropertyName("fatPer100g")]
    public decimal FatPer100g { get; set; }

    [Range(0, 1000)]
    [JsonPropertyName("carbsPer100g")]
    public decimal CarbsPer100g { get; set; }

    [MaxLength(100)]
    [JsonPropertyName("servingSize")]
    public string? ServingSize { get; set; }
}
