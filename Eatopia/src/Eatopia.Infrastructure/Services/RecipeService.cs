using Eatopia.Application.Common;
using Eatopia.Application.DTOs.Recipes;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class RecipeService
{
    private readonly EatopiaDbContext _context;

    public RecipeService(EatopiaDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Recipe>> GetAllAsync(string? search, int pageIndex, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _context.Recipes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.Title.Contains(search));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResultAsync(pageIndex, pageSize);
    }

    public async Task<Recipe> GetByIdAsync(Guid id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe == null)
            throw new ApiException("Recipe not found", 404, "NOT_FOUND");

        return recipe;
    }

    public async Task<Recipe> CreateAsync(Guid userId, CreateRecipeDto dto)
    {
        ValidateRecipe(dto);
        var title = NormalizeTitle(dto.Title);
        await EnsureUniqueRecipeTitleAsync(title);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = CleanOptional(dto.Description, 1000),
            ImageUrl = CleanOptional(dto.ImageUrl, 2000),
            CaloriesPerServing = dto.CaloriesPerServing,
            Servings = dto.Servings,
            IngredientsJson = CleanRequiredJsonList(dto.IngredientsJson, "Ingredients"),
            StepsJson = CleanRequiredJsonList(dto.StepsJson, "Steps"),
            AuthorId = userId
        };

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return recipe;
    }

    public async Task<Recipe> UpdateAsync(Guid id, CreateRecipeDto dto)
    {
        ValidateRecipe(dto);
        var title = NormalizeTitle(dto.Title);

        var recipe = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id);
        if (recipe == null)
            throw new ApiException("Recipe not found", 404, "NOT_FOUND");

        await EnsureUniqueRecipeTitleAsync(title, id);

        recipe.Title = title;
        recipe.Description = CleanOptional(dto.Description, 1000);
        recipe.ImageUrl = CleanOptional(dto.ImageUrl, 2000);
        recipe.CaloriesPerServing = dto.CaloriesPerServing;
        recipe.Servings = dto.Servings;
        recipe.IngredientsJson = CleanRequiredJsonList(dto.IngredientsJson, "Ingredients");
        recipe.StepsJson = CleanRequiredJsonList(dto.StepsJson, "Steps");

        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task DeleteAsync(Guid id)
    {
        var recipe = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id);
        if (recipe == null)
            throw new ApiException("Recipe not found", 404, "NOT_FOUND");

        var savedRows = await _context.RecipeSaved
            .Where(x => x.RecipeId == id)
            .ToListAsync();

        _context.RecipeSaved.RemoveRange(savedRows);
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
    }

    public async Task SaveRecipeAsync(Guid userId, Guid recipeId)
    {
        var exists = await _context.RecipeSaved
            .AnyAsync(x => x.UserId == userId && x.RecipeId == recipeId);

        if (exists)
            throw new ApiException("Recipe already saved", 409, "ALREADY_EXISTS");

        var save = new RecipeSaved
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipeId = recipeId
        };

        _context.RecipeSaved.Add(save);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveSavedRecipeAsync(Guid userId, Guid recipeId)
    {
        var saved = await _context.RecipeSaved
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RecipeId == recipeId);

        if (saved == null)
            throw new ApiException("Not saved", 404, "NOT_FOUND");

        _context.RecipeSaved.Remove(saved);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Recipe>> GetSavedRecipesAsync(Guid userId)
    {
        return await _context.RecipeSaved
            .Where(x => x.UserId == userId)
            .Include(x => x.Recipe)
            .Select(x => x.Recipe)
            .ToListAsync();
    }

    private static void ValidateRecipe(CreateRecipeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ApiException("Recipe title is required", 400, "VALIDATION_ERROR");
        if (string.IsNullOrWhiteSpace(dto.IngredientsJson))
            throw new ApiException("Ingredients are required", 400, "VALIDATION_ERROR");
        if (string.IsNullOrWhiteSpace(dto.StepsJson))
            throw new ApiException("Steps are required", 400, "VALIDATION_ERROR");
        if (dto.Servings < 1)
            throw new ApiException("Servings must be at least 1", 400, "VALIDATION_ERROR");
    }

    private async Task EnsureUniqueRecipeTitleAsync(string title, Guid? currentRecipeId = null)
    {
        var normalizedTitle = NormalizeComparableTitle(title);
        var duplicateExists = await _context.Recipes
            .AsNoTracking()
            .AnyAsync(x =>
                (!currentRecipeId.HasValue || x.Id != currentRecipeId.Value) &&
                x.Title.ToLower().Trim() == normalizedTitle);

        if (duplicateExists)
            throw new ApiException("A recipe with this title already exists.", 409, "ALREADY_EXISTS");
    }

    private static string NormalizeTitle(string value)
    {
        return string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeComparableTitle(string value)
    {
        return NormalizeTitle(value).ToLowerInvariant();
    }

    private static string CleanRequiredJsonList(string value, string fieldName)
    {
        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<string>>(value) ?? [];
            var cleanItems = items
                .Select(item => item?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanItems.Count == 0)
                throw new ApiException($"{fieldName} must include at least one item.", 400, "VALIDATION_ERROR");

            return System.Text.Json.JsonSerializer.Serialize(cleanItems);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ApiException($"{fieldName} must be a valid JSON list.", 400, "VALIDATION_ERROR");
        }
    }

    private static string? CleanOptional(string? value, int maxLength)
    {
        var clean = value?.Trim();
        if (string.IsNullOrWhiteSpace(clean)) return null;
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }
}
