using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Community;

public class ReportContentDto
{
    [Required]
    [MaxLength(1000)]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
