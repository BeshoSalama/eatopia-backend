using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class ChangePasswordDto
{
    [Required]
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = null!;
}
