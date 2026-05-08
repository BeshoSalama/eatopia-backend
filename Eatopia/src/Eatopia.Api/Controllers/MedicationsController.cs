using Eatopia.Application.DTOs.Medication;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/medications")]
[ApiController]
[Authorize]
public class MedicationsController : ControllerBase
{
    private readonly MedicationService _medicationService;

    public MedicationsController(MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMedicationDto dto)
    {
        var medication = await _medicationService.CreateMedicationAsync(GetUserId(), dto);
        return Created("", new { data = medication, success = true, medication });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _medicationService.GetUserMedicationsAsync(GetUserId());
        return Ok(new { data = list, success = true, medications = list });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateMedicationDto dto)
    {
        var medication = await _medicationService.UpdateMedicationAsync(GetUserId(), id, dto);
        return Ok(new { data = medication, success = true, medication });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _medicationService.DeleteMedicationAsync(GetUserId(), id);
        return Ok(new { success = true, message = "Medication deleted." });
    }

    [HttpGet("today")]
    public async Task<IActionResult> TodaySchedules()
    {
        var schedules = await _medicationService.GetTodaySchedulesAsync(GetUserId());
        return Ok(new { data = schedules, success = true, schedules });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
