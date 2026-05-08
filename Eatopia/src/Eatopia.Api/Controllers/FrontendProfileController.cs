using Eatopia.Application.DTOs.Auth;
using Eatopia.Application.DTOs.Users;
using Eatopia.Application.Exceptions;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api")]
[ApiController]
[Authorize]
public class FrontendProfileController : ControllerBase
{
    private readonly AuthService _authService;

    public FrontendProfileController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _authService.GetProfileAsync(userId);

        return Ok(new
        {
            success = true,
            user
        });
    }

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _authService.UpdateProfileAsync(userId, dto);

        return Ok(new
        {
            success = true,
            message = "Profile updated successfully.",
            user
        });
    }

    [HttpGet("profile/privacy-settings")]
    public async Task<IActionResult> GetPrivacySettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _authService.GetPrivacySettingsAsync(userId);
        return Ok(new { success = true, settings, data = settings });
    }

    [HttpPut("profile/privacy-settings")]
    public async Task<IActionResult> UpdatePrivacySettings([FromBody] PrivacySettingsDto dto)
    {
        var userId = GetCurrentUserId();
        var settings = await _authService.UpdatePrivacySettingsAsync(userId, dto);
        return Ok(new { success = true, message = "Privacy settings updated.", settings, data = settings });
    }

    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        await _authService.ChangePasswordAsync(userId, dto);

        return Ok(new
        {
            success = true,
            message = "Password updated successfully."
        });
    }

    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto dto)
    {
        var userId = GetCurrentUserId();
        await _authService.DeleteAccountAsync(userId, dto);

        return Ok(new
        {
            success = true,
            message = "Account deleted permanently."
        });
    }

    [HttpPost("logout-all-devices")]
    public async Task<IActionResult> LogoutAllDevices()
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAllDevicesAsync(userId);
        return Ok(new { success = true, message = "Logged out from all devices." });
    }

    private Guid GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var userId))
            throw new ApiException("Invalid token.", 401, "INVALID_TOKEN");

        return userId;
    }
}
