using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class AddAllergyDto
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("allergyName")]
    public string AllergyName { get; set; } = null!;
}
