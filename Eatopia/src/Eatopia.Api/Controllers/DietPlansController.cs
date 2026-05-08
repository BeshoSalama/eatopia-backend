using Eatopia.Application.DTOs.DietPlans;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/diet-plans")]
[ApiController]
[Authorize]
public class DietPlansController : ControllerBase
{
    private readonly DietPlanService _dietPlanService;

    public DietPlansController(DietPlanService dietPlanService)
    {
        _dietPlanService = dietPlanService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateDietPlanDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var plan = await _dietPlanService.CreatePlanAsync(userId, dto);

        return Created("", new { data = plan });
    }

    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(GenerateDietPlanDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var plan = await _dietPlanService.GeneratePlanAsync(userId, dto);

        // Auto-assign to current user for convenience (matches documentation)
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(Math.Max(dto.DurationDays, 1) - 1);

        var assignment = await _dietPlanService.AssignPlanToUserAsync(userId, new AssignUserPlanDto
        {
            PlanId = plan.Id,
            StartDate = start,
            EndDate = end
        });

        return Ok(new
        {
            data = new
            {
                planId = plan.Id,
                assignmentId = assignment.Id
            }
        });
    }
}
