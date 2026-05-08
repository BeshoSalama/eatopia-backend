using Eatopia.Api.Common;
using Eatopia.Application.DTOs.Recipes;
using Eatopia.Domain.Auth;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/recipes")]
[Route("api/recipes")]
[ApiController]
public class RecipesController : ControllerBase
{
    private readonly RecipeService _recipeService;

    public RecipesController(RecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? pageIndex = null)
    {
        var result = await _recipeService.GetAllAsync(search, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);

        return Ok(new
        {
            data = result.Items,
            meta = PaginationHelper.ToMeta(result)
        });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var recipe = await _recipeService.GetByIdAsync(id);
        return Ok(new { data = recipe });
    }

    [Authorize(Roles = UserRoles.Elevated)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRecipeDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var recipe = await _recipeService.CreateAsync(userId, dto);

        return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, new { data = recipe });
    }

    [Authorize(Roles = UserRoles.Elevated)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateRecipeDto dto)
    {
        var recipe = await _recipeService.UpdateAsync(id, dto);
        return Ok(new { success = true, message = "Recipe updated.", data = recipe, recipe });
    }

    [Authorize(Roles = UserRoles.Elevated)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _recipeService.DeleteAsync(id);
        return Ok(new { success = true, message = "Recipe deleted." });
    }

    [Authorize]
    [HttpPost("{id:guid}/save")]
    public async Task<IActionResult> Save(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _recipeService.SaveRecipeAsync(userId, id);

        return Ok(new { message = "Saved" });
    }

    [Authorize]
    [HttpDelete("{id:guid}/save")]
    public async Task<IActionResult> Unsave(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _recipeService.RemoveSavedRecipeAsync(userId, id);

        return Ok(new { message = "Removed" });
    }

    [Authorize]
    [HttpGet("saved")]
    public async Task<IActionResult> Saved()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var recipes = await _recipeService.GetSavedRecipesAsync(userId);

        return Ok(new { data = recipes });
    }
}
