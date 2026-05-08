using Eatopia.Application.DTOs.DietPlans;
using Eatopia.Application.Exceptions;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/user-plans")]
[ApiController]
[Authorize]
public class UserPlansController : ControllerBase
{
    private readonly DietPlanService _dietPlanService;

    public UserPlansController(DietPlanService dietPlanService)
    {
        _dietPlanService = dietPlanService;
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign(AssignUserPlanDto dto)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var targetUserId = dto.UserId ?? currentUserId;

        // Only admin can assign to another user
        if (dto.UserId.HasValue && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var assignment = await _dietPlanService.AssignPlanToUserAsync(targetUserId, dto);

        return Ok(new { data = assignment });
    }

    [HttpGet("active")]
    public async Task<IActionResult> Active()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var active = await _dietPlanService.GetActivePlanAsync(userId);

        if (active == null)
            return Ok(new { data = (object?)null });

        return Ok(new { data = active });
    }
}
