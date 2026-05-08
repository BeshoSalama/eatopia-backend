using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Meals;

public class CreateMealDto
{
    [Required]
    [JsonPropertyName("foodId")]
    public Guid FoodId { get; set; }

    [MaxLength(1000)]
    [JsonPropertyName("mealImageUrl")]
    public string? MealImageUrl { get; set; }

    [Range(1, 10000)]
    [JsonPropertyName("quantityGrams")]
    public decimal QuantityGrams { get; set; }
}
