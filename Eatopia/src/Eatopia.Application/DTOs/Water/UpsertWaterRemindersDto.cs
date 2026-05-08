using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Water;

public class UpsertWaterRemindersDto
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [Required]
    [MinLength(1)]
    [JsonPropertyName("intakes")]
    public List<WaterReminderItemDto> Intakes { get; set; } = new();
}

public class WaterReminderItemDto
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [Required]
    [JsonPropertyName("timeOfDay")]
    public TimeSpan TimeOfDay { get; set; }

    [Range(1, 10000)]
    [JsonPropertyName("amountMl")]
    public int AmountMl { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }
}
