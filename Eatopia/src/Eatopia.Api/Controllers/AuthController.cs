using Eatopia.Application.DTOs.Auth;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        await _authService.RegisterAsync(dto);
        return Ok(new { success = true, requiresActivation = true, message = "Account created successfully. Check your email and activate your account before logging in." });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(new { data = result });
    }

    [HttpPost("activate")]
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

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return Ok(new { data = result });
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.LogoutAllDevicesAsync(userId);
        return Ok(new { success = true, message = "Logged out from all devices." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            data = new
            {
                id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                email = User.FindFirstValue(ClaimTypes.Email),
                name = User.FindFirstValue(ClaimTypes.Name),
                role = User.FindFirstValue(ClaimTypes.Role)
            }
        });
    }
}
