using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Community;

public class SharePostToProfileDto
{
    [MaxLength(1000)]
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}
