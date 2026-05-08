using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.SocialAuth;

public class SocialLoginRequestDto
{
    [Required]
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = null!;

    [Required]
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = null!;

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }
}
