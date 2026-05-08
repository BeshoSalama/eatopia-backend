using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Community;

public class SharePostToMessageDto
{
    [Required]
    [JsonPropertyName("targetUserId")]
    public Guid TargetUserId { get; set; }

    [MaxLength(1000)]
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
