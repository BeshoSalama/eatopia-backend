using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Chat;

public class CreateThreadDto
{
    [Required]
    [JsonPropertyName("otherUserId")]
    public Guid OtherUserId { get; set; }
}
