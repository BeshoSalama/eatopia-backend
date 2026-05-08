using Eatopia.Application.DTOs.Water;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/water")]
[ApiController]
[Authorize]
public class WaterController : ControllerBase
{
    private readonly WaterService _waterService;

    public WaterController(WaterService waterService)
    {
        _waterService = waterService;
    }

    [HttpGet("goal")]
    public async Task<IActionResult> GetGoal()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var goal = await _waterService.GetGoalAsync(userId);

        return Ok(new { data = goal });
    }

    [HttpPut("goal")]
    public async Task<IActionResult> UpdateGoal(UpdateWaterGoalDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var goal = await _waterService.UpdateGoalAsync(userId, dto);

        return Ok(new { data = goal });
    }

    [HttpPost("logs")]
    public async Task<IActionResult> AddLog(AddWaterLogDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var log = await _waterService.AddLogAsync(userId, dto);

        return Created("", new { data = log });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] DateTime date)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _waterService.GetLogsByDateAsync(userId, date);

        return Ok(new { data = result });
    }
}
