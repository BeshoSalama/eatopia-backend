using Eatopia.Api.Common;
using Eatopia.Application.DTOs.Food;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eatopia.Api.Controllers;

[Route("api/v1/food-items")]
[ApiController]
[Authorize]
public class FoodItemsController : ControllerBase
{
    private readonly FoodService _foodService;

    public FoodItemsController(FoodService foodService)
    {
        _foodService = foodService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? pageIndex = null)
    {
        var result = await _foodService.GetAllAsync(search, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);

        return Ok(new
        {
            data = result.Items,
            meta = PaginationHelper.ToMeta(result)
        });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var food = await _foodService.GetByIdAsync(id);
        return Ok(new { data = food });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateFoodItemDto dto)
    {
        var food = await _foodService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = food.Id }, new { data = food });
    }
}
