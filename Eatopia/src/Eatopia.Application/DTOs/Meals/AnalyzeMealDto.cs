using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Meals;

public class AnalyzeMealDto
{
    [Required]
    [MaxLength(1000)]
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = null!;

    [Range(1, 10000)]
    [JsonPropertyName("quantityGrams")]
    public decimal QuantityGrams { get; set; }
}
