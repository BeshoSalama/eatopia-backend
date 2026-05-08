using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.AI;

public class AiFoodResultDto
{
    [JsonPropertyName("isFood")]
    public bool IsFood { get; set; } = true;

    [JsonPropertyName("foodName")]
    public string FoodName { get; set; } = null!;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("calories")]
    public decimal Calories { get; set; }

    [JsonPropertyName("protein")]
    public decimal Protein { get; set; }

    [JsonPropertyName("carbs")]
    public decimal Carbs { get; set; }

    [JsonPropertyName("fat")]
    public decimal Fat { get; set; }

    [JsonPropertyName("fiber")]
    public decimal Fiber { get; set; }

    [JsonPropertyName("sugar")]
    public decimal Sugar { get; set; }

    [JsonPropertyName("ingredients")]
    public List<string> Ingredients { get; set; } = new();

    [JsonPropertyName("instructions")]
    public List<string> Instructions { get; set; } = new();

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("modelError")]
    public string? ModelError { get; set; }
}
