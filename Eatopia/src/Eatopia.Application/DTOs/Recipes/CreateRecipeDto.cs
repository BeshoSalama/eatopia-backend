using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Recipes;

public class CreateRecipeDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [MaxLength(2000)]
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [Range(0, 10000)]
    [JsonPropertyName("caloriesPerServing")]
    public decimal? CaloriesPerServing { get; set; }

    [Range(1, 100)]
    [JsonPropertyName("servings")]
    public int Servings { get; set; }

    [Required]
    [JsonPropertyName("ingredientsJson")]
    public string IngredientsJson { get; set; } = null!;

    [Required]
    [JsonPropertyName("stepsJson")]
    public string StepsJson { get; set; } = null!;
}
