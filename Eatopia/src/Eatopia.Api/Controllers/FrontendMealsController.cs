using Eatopia.Api.Common;
using Eatopia.Application.DTOs.Meals;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/meals")]
[ApiController]
[Authorize]
public class FrontendMealsController : ControllerBase
{
    private readonly MealService _mealService;

    public FrontendMealsController(MealService mealService)
    {
        _mealService = mealService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? pageIndex = null)
    {
        var result = await _mealService.GetHistoryAsync(GetUserId(), from, to, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);
        return Ok(new
        {
            success = true,
            meals = result.Items,
            data = result.Items,
            meta = PaginationHelper.ToMeta(result)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FrontendMealDto dto)
    {
        var meal = await _mealService.CreateFrontendMealAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Meal saved.", meal, data = meal });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FrontendMealDto dto)
    {
        var meal = await _mealService.UpdateFrontendMealAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Meal updated.", meal, data = meal });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mealService.DeleteFrontendMealAsync(GetUserId(), id);
        return Ok(new { success = true, message = "Meal deleted." });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
