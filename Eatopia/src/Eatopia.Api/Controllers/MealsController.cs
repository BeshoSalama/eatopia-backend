using Eatopia.Api.Common;
using Eatopia.Application.DTOs.Meals;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/meals")]
[ApiController]
[Authorize]
public class MealsController : ControllerBase
{
    private readonly MealService _mealService;

    public MealsController(MealService mealService)
    {
        _mealService = mealService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMealDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var meal = await _mealService.CreateMealAsync(userId, dto);

        return CreatedAtAction(nameof(History), new { }, new { data = meal });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? pageIndex = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mealService.GetHistoryAsync(userId, from, to, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);

        return Ok(new
        {
            data = result.Items,
            meta = PaginationHelper.ToMeta(result)
        });
    }

    [HttpPost("analyze-and-save")]
    public async Task<IActionResult> AnalyzeAndSave(AnalyzeMealDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var meal = await _mealService.AnalyzeAndSaveAsync(userId, dto);

        return Ok(new { data = meal });
    }
}
