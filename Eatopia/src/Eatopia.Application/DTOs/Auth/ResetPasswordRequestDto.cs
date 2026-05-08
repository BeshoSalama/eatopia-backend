using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be exactly 6 digits.")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = null!;

    [Required]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = null!;
}
