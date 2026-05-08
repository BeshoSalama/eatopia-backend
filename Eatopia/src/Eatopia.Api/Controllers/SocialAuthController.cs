using Eatopia.Application.DTOs.SocialAuth;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Eatopia.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class SocialAuthController : ControllerBase
{
    private readonly ExternalAuthService _externalAuthService;

    public SocialAuthController(ExternalAuthService externalAuthService)
    {
        _externalAuthService = externalAuthService;
    }

    [HttpPost("social-login")]
    public async Task<IActionResult> SocialLogin(SocialLoginRequestDto dto)
    {
        var result = await _externalAuthService.LoginAsync(dto);
        return Ok(new
        {
            success = true,
            message = "Logged in successfully.",
            token = result.Token,
            refreshToken = result.RefreshToken,
            refreshTokenExpiresAt = result.RefreshTokenExpiresAt,
            user = result.User
        });
    }
}
