using Eatopia.Application.DTOs.Users;

namespace Eatopia.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public UserResponseDto User { get; set; } = null!;
}
