using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class UpdateProfileDto
{
    [JsonPropertyName("name")]
    [MaxLength(200)]
    public string? Name { get; set; }

    [JsonPropertyName("fullName")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [JsonPropertyName("username")]
    [MaxLength(100)]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }

    [JsonPropertyName("age")]
    [Range(1, 120)]
    public int? Age { get; set; }

    [JsonPropertyName("weight")]
    [Range(1, 1000)]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("height")]
    [Range(1, 300)]
    public decimal? HeightCm { get; set; }

    [JsonPropertyName("goal")]
    [MaxLength(200)]
    public string? Goal { get; set; }

    [JsonPropertyName("gender")]
    [MaxLength(20)]
    public string? Gender { get; set; }

    [JsonPropertyName("activityLevel")]
    [MaxLength(50)]
    public string? ActivityLevel { get; set; }

    [JsonPropertyName("location")]
    [MaxLength(100)]
    public string? Location { get; set; }

    [JsonPropertyName("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; set; }
}
