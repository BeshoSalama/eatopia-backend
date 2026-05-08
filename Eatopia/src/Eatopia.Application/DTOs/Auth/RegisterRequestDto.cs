using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = null!;

    // Old backend shape: { "name": "..." }
    [MaxLength(200)]
    public string? Name { get; set; }

    // Frontend shape: { "fullName": "..." }
    [JsonPropertyName("fullName")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [JsonPropertyName("profileImage")]
    [MaxLength(1000)]
    public string? ProfileImage { get; set; }
}
