using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Auth;

public class ActivateAccountRequestDto
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [Required]
    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}
