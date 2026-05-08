using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Eatopia.Tests.Integration;

public class DietPlansApiTests
{
    [Fact]
    public async Task Generate_diet_plan_requires_auth_and_creates_assigned_plan_for_user()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var anonymousResponse = await client.PostAsJsonAsync("/api/v1/diet-plans/generate", new
        {
            durationDays = 2,
            caloriesTargetPerDay = 1800,
            goal = "Weight Loss"
        });
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var user = await factory.AddUserAsync("planner@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(user));
        var response = await client.PostAsJsonAsync("/api/v1/diet-plans/generate", new
        {
            durationDays = 2,
            caloriesTargetPerDay = 1800,
            goal = "Weight Loss"
        });

        response.EnsureSuccessStatusCode();
        await factory.WithDbAsync(async db =>
        {
            var plan = await db.DietPlans.SingleAsync(x => x.CreatedBy == user.Id);
            var assignment = await db.UserPlans.SingleAsync(x => x.UserId == user.Id && x.PlanId == plan.Id);
            var items = await db.DietPlanItems.Where(x => x.PlanId == plan.Id).ToListAsync();

            Assert.NotNull(assignment.StartDate);
            Assert.NotNull(assignment.EndDate);
            Assert.Equal(2, (assignment.EndDate!.Value - assignment.StartDate!.Value).Days + 1);
            Assert.Equal(8, items.Count);
            Assert.Equal(new[] { 1, 2 }, items.Select(x => x.DayOfWeek).Distinct().OrderBy(x => x).ToArray());
        });
    }
}
