using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Auth;

public class DeleteAccountRequestDto
{
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("confirmationText")]
    public string? ConfirmationText { get; set; }
}
