using Eatopia.Domain.Auth;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eatopia.Tests.Integration;

public class RecipesApiTests
{
    [Fact]
    public async Task Admin_can_create_list_and_delete_recipe_through_api()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        var admin = await factory.AddUserAsync("admin@test.com", UserRoles.Admin);
        var token = factory.CreateToken(admin);
        using var client = factory.CreateClient();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/recipes")
        {
            Content = JsonContent.Create(new
            {
                title = "Integration Lentil Bowl",
                description = "Balanced lentils with rice and salad.",
                imageUrl = "https://example.com/lentils.jpg",
                caloriesPerServing = 420,
                servings = 2,
                ingredientsJson = "[\"1 cup lentils\",\"1/2 cup rice\",\"salad\"]",
                stepsJson = "[\"Cook lentils\",\"Serve with rice and salad\"]"
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createJson = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var recipeId = createJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var listResponse = await client.GetAsync("/api/recipes?page=1&pageSize=10");

        listResponse.EnsureSuccessStatusCode();
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Integration Lentil Bowl", listBody);

        var anonymousDelete = await client.DeleteAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousDelete.StatusCode);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/recipes/{recipeId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await client.SendAsync(deleteRequest);

        deleteResponse.EnsureSuccessStatusCode();
        await factory.WithDbAsync(async db =>
        {
            Assert.False(await db.Recipes.AnyAsync(x => x.Id == recipeId));
        });
    }

    [Fact]
    public async Task Regular_user_cannot_create_update_or_delete_recipes()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        var regularUser = await factory.AddUserAsync("member@test.com", UserRoles.User);
        var admin = await factory.AddUserAsync("admin@test.com", UserRoles.Admin);
        using var client = factory.CreateClient();

        var recipeId = await factory.WithDbAsync(async db =>
        {
            var recipe = new Eatopia.Domain.Entities.Recipe
            {
                Id = Guid.NewGuid(),
                AuthorId = admin.Id,
                Title = "Protected Recipe",
                Description = "Only elevated roles can manage this.",
                IngredientsJson = "[\"ingredient\"]",
                StepsJson = "[\"step\"]",
                CreatedAt = DateTime.UtcNow
            };

            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
            return recipe.Id;
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(regularUser));
        var createResponse = await client.PostAsJsonAsync("/api/recipes", new
        {
            title = "User Created Recipe",
            ingredientsJson = "[\"x\"]",
            stepsJson = "[\"y\"]"
        });
        var updateResponse = await client.PutAsJsonAsync($"/api/recipes/{recipeId}", new
        {
            title = "User Updated Recipe",
            ingredientsJson = "[\"x\"]",
            stepsJson = "[\"y\"]"
        });
        var deleteResponse = await client.DeleteAsync($"/api/recipes/{recipeId}");

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        await factory.WithDbAsync(async db =>
        {
            var recipeStillExists = await db.Recipes.AnyAsync(x => x.Id == recipeId);
            Assert.True(recipeStillExists);
        });
    }
}
