using Eatopia.Application.DTOs.Medication;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/medication-schedules")]
[ApiController]
[Authorize]
public class MedicationSchedulesController : ControllerBase
{
    private readonly MedicationService _medicationService;

    public MedicationSchedulesController(MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    [HttpPatch("{id:guid}/taken")]
    public async Task<IActionResult> MarkTaken(Guid id, MarkDoseTakenDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _medicationService.MarkDoseTakenAsync(userId, id, dto);

        return Ok(new { data = result });
    }
}
