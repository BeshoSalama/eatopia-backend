using Eatopia.Application.DTOs.Auth;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Eatopia.Api.Controllers;

[Route("api")]
[ApiController]
public class FrontendAuthController : ControllerBase
{
    private readonly AuthService _authService;

    public FrontendAuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(RegisterRequestDto dto)
    {
        await _authService.RegisterAsync(dto);
        return Ok(new
        {
            success = true,
            requiresActivation = true,
            message = "Account created successfully. Check your email and activate your account before logging in."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(new { success = true, message = "Logged in successfully.", token = result.Token, refreshToken = result.RefreshToken, refreshTokenExpiresAt = result.RefreshTokenExpiresAt, user = result.User });
    }

    [HttpPost("activate-account")]
    public async Task<IActionResult> ActivateAccount(ActivateAccountRequestDto dto)
    {
        await _authService.ActivateAccountAsync(dto);
        return Ok(new { success = true, message = "Account activated successfully. You can login now." });
    }

    [HttpPost("resend-activation")]
    public async Task<IActionResult> ResendActivationEmail(ResendActivationEmailRequestDto dto)
    {
        await _authService.ResendActivationEmailAsync(dto);
        return Ok(new { success = true, message = "If this email needs activation, a new activation link has been sent." });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return Ok(new { success = true, token = result.Token, refreshToken = result.RefreshToken, refreshTokenExpiresAt = result.RefreshTokenExpiresAt, user = result.User });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
    {
        await _authService.RequestPasswordResetAsync(dto);
        return Ok(new { success = true, message = "If this email exists, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new { success = true, message = "Password reset successfully. You can login now." });
    }
}
