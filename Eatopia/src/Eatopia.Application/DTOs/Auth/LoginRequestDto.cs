using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Auth;

public class LoginRequestDto
{
    // Old backend shape: { "email": "..." }
    [EmailAddress]
    public string? Email { get; set; }

    // Frontend shape: { "usernameOrEmail": "..." }
    [JsonPropertyName("usernameOrEmail")]
    [MaxLength(256)]
    public string? UsernameOrEmail { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = null!;
}
