using System.Text.Json;
using Eatopia.Application.DTOs.Recipes;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Services;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Services;

public class RecipeServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesTitleAndCleansIngredientAndStepLists()
    {
        await using var database = await TestDatabase.CreateAsync();
        var author = await database.AddUserAsync("chef@eatopia.local");
        var service = new RecipeService(database.Context);

        var recipe = await service.CreateAsync(author.Id, new CreateRecipeDto
        {
            Title = "  Lentil    Soup  ",
            Description = "  Warm soup  ",
            ImageUrl = "  https://example.com/soup.jpg  ",
            CaloriesPerServing = 320,
            Servings = 2,
            IngredientsJson = JsonSerializer.Serialize(new[] { " lentils ", "Lentils", " lemon " }),
            StepsJson = JsonSerializer.Serialize(new[] { " Boil ", "Boil", " Serve " })
        });

        Assert.Equal("Lentil Soup", recipe.Title);
        Assert.Equal("Warm soup", recipe.Description);
        Assert.Equal("https://example.com/soup.jpg", recipe.ImageUrl);
        Assert.Equal(new[] { "lentils", "lemon" }, JsonSerializer.Deserialize<string[]>(recipe.IngredientsJson));
        Assert.Equal(new[] { "Boil", "Serve" }, JsonSerializer.Deserialize<string[]>(recipe.StepsJson));
    }

    [Fact]
    public async Task CreateAsync_DuplicateTitleIgnoringCaseAndSpacing_ThrowsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var author = await database.AddUserAsync("chef@eatopia.local");
        var service = new RecipeService(database.Context);
        var dto = ValidRecipe("Ful Medames");

        await service.CreateAsync(author.Id, dto);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateAsync(author.Id, ValidRecipe("  ful   medames ")));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecipeAndSavedRecipeRows()
    {
        await using var database = await TestDatabase.CreateAsync();
        var author = await database.AddUserAsync("chef@eatopia.local");
        var user = await database.AddUserAsync("user@eatopia.local");
        var service = new RecipeService(database.Context);
        var recipe = await service.CreateAsync(author.Id, ValidRecipe("Koshari Bowl"));

        database.Context.RecipeSaved.Add(new RecipeSaved
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RecipeId = recipe.Id,
            CreatedAt = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();

        await service.DeleteAsync(recipe.Id);

        Assert.False(await database.Context.Recipes.AnyAsync(x => x.Id == recipe.Id));
        Assert.False(await database.Context.RecipeSaved.AnyAsync(x => x.RecipeId == recipe.Id));
    }

    private static CreateRecipeDto ValidRecipe(string title) => new()
    {
        Title = title,
        Description = "A tested recipe.",
        CaloriesPerServing = 300,
        Servings = 2,
        IngredientsJson = JsonSerializer.Serialize(new[] { "ingredient" }),
        StepsJson = JsonSerializer.Serialize(new[] { "cook" })
    };
}
