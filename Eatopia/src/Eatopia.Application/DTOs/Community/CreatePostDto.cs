using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Community;

public class CreatePostDto
{
    [MaxLength(3000)]
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [MaxLength(2000)]
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}
