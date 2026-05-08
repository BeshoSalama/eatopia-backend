using Eatopia.Application.DTOs.Water;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/water")]
[ApiController]
[Authorize]
public class FrontendWaterController : ControllerBase
{
    private readonly WaterService _waterService;

    public FrontendWaterController(WaterService waterService)
    {
        _waterService = waterService;
    }

    [HttpGet("reminders")]
    public async Task<IActionResult> GetReminders([FromQuery] DateTime? date)
    {
        var reminders = await _waterService.GetRemindersAsync(GetUserId(), date);
        return Ok(new { success = true, intakes = reminders, data = reminders });
    }

    [HttpPut("reminders")]
    public async Task<IActionResult> SaveReminders([FromBody] UpsertWaterRemindersDto dto)
    {
        var reminders = await _waterService.UpsertRemindersAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Water reminders saved.", intakes = reminders, data = reminders });
    }

    [HttpPut("reminders/{id:guid}")]
    public async Task<IActionResult> ToggleReminder(Guid id, [FromBody] ToggleWaterReminderDto dto)
    {
        var reminder = await _waterService.ToggleReminderAsync(GetUserId(), id, dto.IsCompleted);
        return Ok(new { success = true, data = reminder, intake = reminder });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class ToggleWaterReminderDto
{
    [System.Text.Json.Serialization.JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }
}
