using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Medication;

public class CreateMedicationDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [MaxLength(200)]
    [JsonPropertyName("dosageText")]
    public string? DosageText { get; set; }

    [MaxLength(20)]
    [JsonPropertyName("beforeAfterMeal")]
    public string? BeforeAfterMeal { get; set; }

    [Range(1, 24)]
    [JsonPropertyName("timesPerDay")]
    public int TimesPerDay { get; set; }

    [Required]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [Required]
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [MinLength(1)]
    [JsonPropertyName("timesOfDay")]
    public List<TimeSpan> TimesOfDay { get; set; } = new();
}
