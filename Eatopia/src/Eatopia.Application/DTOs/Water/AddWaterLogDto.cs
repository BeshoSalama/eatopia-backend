using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Water;

public class AddWaterLogDto
{
    [Range(1, 10000)]
    [JsonPropertyName("amountMl")]
    public int AmountMl { get; set; }

    [JsonPropertyName("loggedAt")]
    public DateTime LoggedAt { get; set; }
}
