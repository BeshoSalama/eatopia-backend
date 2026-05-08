using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Water;

public class UpdateWaterGoalDto
{
    [Range(1, 10000)]
    [JsonPropertyName("dailyTargetMl")]
    public int DailyTargetMl { get; set; }

    [Range(1, 1440)]
    [JsonPropertyName("remindEveryMinutes")]
    public int RemindEveryMinutes { get; set; }
}
