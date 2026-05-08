using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Chat;

public class UpdateMessageDto
{
    [Required]
    [MaxLength(2000)]
    [JsonPropertyName("messageText")]
    public string MessageText { get; set; } = null!;
}
