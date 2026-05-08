using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Medication;

public class MarkDoseTakenDto
{
    [JsonPropertyName("isTaken")]
    public bool IsTaken { get; set; }

    [JsonPropertyName("takenAt")]
    public DateTime? TakenAt { get; set; }
}
