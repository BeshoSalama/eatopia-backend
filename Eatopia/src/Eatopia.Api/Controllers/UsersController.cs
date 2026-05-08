using Eatopia.Application.DTOs.Users;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserPreferencesService _prefs;

    public UsersController(AuthService authService, UserPreferencesService prefs)
    {
        _authService = authService;
        _prefs = prefs;
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _authService.UpdateProfileAsync(userId, dto);

        return Ok(new { data = result });
    }

    // Allergies
    [HttpGet("me/allergies")]
    public async Task<IActionResult> GetAllergies()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _prefs.GetAllergiesAsync(userId);

        return Ok(new { data = result });
    }

    [HttpPost("me/allergies")]
    public async Task<IActionResult> AddAllergy(AddAllergyDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _prefs.AddAllergyAsync(userId, dto);

        return Ok(new { data = result });
    }

    [HttpDelete("me/allergies/{id:guid}")]
    public async Task<IActionResult> RemoveAllergy(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _prefs.RemoveAllergyAsync(userId, id);

        return Ok(new { message = "Deleted" });
    }

    // Disliked foods
    [HttpGet("me/disliked-foods")]
    public async Task<IActionResult> GetDislikedFoods()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _prefs.GetDislikedFoodsAsync(userId);

        return Ok(new { data = result });
    }

    [HttpPost("me/disliked-foods")]
    public async Task<IActionResult> AddDislikedFood(AddDislikedFoodDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _prefs.AddDislikedFoodAsync(userId, dto);

        return Ok(new { data = result });
    }

    [HttpDelete("me/disliked-foods/{id:guid}")]
    public async Task<IActionResult> RemoveDislikedFood(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _prefs.RemoveDislikedFoodAsync(userId, id);

        return Ok(new { message = "Deleted" });
    }
}
