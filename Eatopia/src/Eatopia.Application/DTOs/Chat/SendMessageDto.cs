using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Chat;

public class SendMessageDto
{
    [MaxLength(2000)]
    [JsonPropertyName("messageText")]
    public string? MessageText { get; set; }

    [MaxLength(30)]
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "text";

    // Portable /uploads/... URL for voice notes, images, short videos or small files.
    [JsonPropertyName("mediaContent")]
    public string? MediaContent { get; set; }

    [MaxLength(255)]
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
}
