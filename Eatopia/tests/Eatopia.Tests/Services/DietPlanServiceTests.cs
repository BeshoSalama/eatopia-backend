using Eatopia.Application.DTOs.DietPlans;
using Eatopia.Application.Exceptions;
using Eatopia.Infrastructure.Services;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Services;

public class DietPlanServiceTests
{
    [Fact]
    public async Task GeneratePlanAsync_UsesRequestedDurationAndVariesMealTitles()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync("planner@eatopia.local");
        var service = new DietPlanService(database.Context);

        var plan = await service.GeneratePlanAsync(user.Id, new GenerateDietPlanDto
        {
            DurationDays = 3,
            CaloriesTargetPerDay = 1800,
            Goal = "Weight Loss"
        });

        var items = await database.Context.DietPlanItems
            .Where(x => x.PlanId == plan.Id)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.MealType)
            .ToListAsync();

        Assert.Equal(12, items.Count);
        Assert.Equal(new[] { 1, 2, 3 }, items.Select(x => x.DayOfWeek).Distinct().ToArray());
        Assert.True(items.Where(x => x.MealType == "Breakfast").Select(x => x.Title).Distinct().Count() > 1);

        foreach (var dayGroup in items.GroupBy(x => x.DayOfWeek))
            Assert.Equal(1800, dayGroup.Sum(x => x.CaloriesEstimated));
    }

    [Fact]
    public async Task AssignPlanToUserAsync_EndDateBeforeStartDate_ThrowsValidationError()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync("planner@eatopia.local");
        var service = new DietPlanService(database.Context);
        var plan = await service.GeneratePlanAsync(user.Id, new GenerateDietPlanDto
        {
            DurationDays = 1,
            CaloriesTargetPerDay = 1800
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.AssignPlanToUserAsync(user.Id, new AssignUserPlanDto
            {
                PlanId = plan.Id,
                StartDate = new DateTime(2026, 5, 10),
                EndDate = new DateTime(2026, 5, 9)
            }));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("VALIDATION_ERROR", exception.Code);
    }
}
