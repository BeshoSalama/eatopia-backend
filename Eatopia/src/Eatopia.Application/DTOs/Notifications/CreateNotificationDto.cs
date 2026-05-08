using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Notifications;

public class CreateNotificationDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [Required]
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [MaxLength(50)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "info";

    [JsonPropertyName("scheduledFor")]
    public DateTime? ScheduledFor { get; set; }

    [JsonPropertyName("sendEmail")]
    public bool SendEmail { get; set; } = true;
}
