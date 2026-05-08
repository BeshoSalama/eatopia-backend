using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class AddDislikedFoodDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("foodName")]
    public string FoodName { get; set; } = null!;
}
