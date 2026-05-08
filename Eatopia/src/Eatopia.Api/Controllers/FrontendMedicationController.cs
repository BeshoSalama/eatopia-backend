using Eatopia.Application.DTOs.Medication;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/medications")]
[ApiController]
[Authorize]
public class FrontendMedicationController : ControllerBase
{
    private readonly MedicationService _medicationService;

    public FrontendMedicationController(MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _medicationService.GetUserMedicationsAsync(GetUserId());
        return Ok(new { success = true, medications = list, data = list });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicationDto dto)
    {
        var medication = await _medicationService.CreateMedicationAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Medication saved.", medication, data = medication });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMedicationDto dto)
    {
        var medication = await _medicationService.UpdateMedicationAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Medication updated.", medication, data = medication });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _medicationService.DeleteMedicationAsync(GetUserId(), id);
        return Ok(new { success = true, message = "Medication deleted." });
    }

    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> MarkDose(Guid scheduleId, [FromBody] MarkDoseTakenDto dto)
    {
        var result = await _medicationService.MarkDoseTakenAsync(GetUserId(), scheduleId, dto);
        return Ok(new { success = true, data = result });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
